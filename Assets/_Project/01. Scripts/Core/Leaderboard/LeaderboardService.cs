// 무한 모드 순위표 창구: 익명 로그인, 닉네임 맞추기, 기록 올리기, 페이지별 순위와 내 순위 불러오기
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 닉네임은 처음 켤 때 정해 기기에 저장하고, 서버 이름은 기록을 올리거나 순위를 볼 때 뒤에서 맞춘다(오프라인이어도 닉네임 정하기가 막히지 않게).
    // UI와 기록기는 이 창구만 부른다. 실패(오프라인, 시간 초과, 서버 오류)는 예외 대신 빈 결과로 돌려줘 게임 흐름을 막지 않는다.
    // 씬마다 놓인 LeaderboardBootstrap이 설정을 넘겨 시작한다(처음 넘긴 설정을 계속 쓴다).
    public static class LeaderboardService
    {
        private const string LogPrefix = "[Leaderboard] ";

        // SubmitAsync 결과. 1 이상은 순위, 0은 올릴 것이 없어 건너뜀, 이 값은 연결 실패(오프라인, 시간 초과, 서버 오류).
        public const int NetworkFailed = -1;

        private static LeaderboardData _data;
        // 이번 실행에서 서버에 맞춘 닉네임. 저장된 닉네임과 다르면 다시 맞춘다.
        private static string _syncedName;
        private static readonly List<LeaderboardRow> _rows = new List<LeaderboardRow>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _data = null;
            _syncedName = null;
            _rows.Clear();
        }

        public static bool IsAvailable => _data != null && _data.IsEnabled;
        public static bool HasName => SaveSystem.Data.LeaderboardName.Length > 0;
        public static int PageSize => _data != null ? _data.PageSize : 10;
        public static int NameMinLength => _data != null ? _data.NameMinLength : 2;
        public static int NameMaxLength => _data != null ? _data.NameMaxLength : 12;

        // 에디터에서는 설정을 켰을 때만 올린다(개발 중 기록이 섞이지 않게).
        public static bool IsBlockedInEditor => Application.isEditor && _data != null && !_data.SubmitFromEditor;

        public static void Configure(LeaderboardData data)
        {
            if (_data == null && data != null)
            {
                _data = data;
            }
        }

        // 입력한 이름을 서버 규칙(띄어쓰기 없음)과 게임 규칙(길이, 글자, 금지어)에 맞춘다. 쓸 수 없으면 null.
        // 띄어쓰기는 밑줄로 바꿔 받아 준다. 글자는 한글 완성형, 영문, 숫자, 밑줄만 허용한다.
        public static string CleanName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var builder = new StringBuilder();
            string trimmed = raw.Trim();
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (char.IsWhiteSpace(c))
                {
                    c = '_';
                }

                bool isAllowed = (c >= '가' && c <= '힣') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                    || (c >= '0' && c <= '9') || c == '_';
                if (!isAllowed)
                {
                    return null;
                }

                builder.Append(c);
            }

            string name = builder.ToString();
            if (name.Length < NameMinLength || name.Length > NameMaxLength || HasBannedWord(name))
            {
                return null;
            }

            return name;
        }

        private static bool HasBannedWord(string name)
        {
            if (_data == null || _data.BannedWords == null)
            {
                return false;
            }

            string plain = name.Replace("_", "").ToLowerInvariant();
            string[] banned = _data.BannedWords;
            for (int i = 0; i < banned.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(banned[i]) && plain.Contains(banned[i].Replace(" ", "").ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        // 닉네임을 기기에 저장한다. 서버에는 다음 요청 때 맞춘다.
        public static void SaveName(string cleanName)
        {
            SaveSystem.Data.LeaderboardName = cleanName;
            SaveSystem.Save();
        }

        // 서비스를 켜고 익명으로 로그인한 뒤 서버 이름을 저장된 닉네임에 맞춘다. 처음이면 이 기기에 새 익명 계정이 생긴다.
        private static async Task<bool> EnsureReadyAsync()
        {
            if (!IsAvailable)
            {
                return false;
            }

            try
            {
                if (!await Within(UgsServices.InitializeAsync()))
                {
                    return false;
                }

                IAuthenticationService auth = AuthenticationService.Instance;
                if (!auth.IsSignedIn && !await Within(auth.SignInAnonymouslyAsync()))
                {
                    return false;
                }

                string name = SaveSystem.Data.LeaderboardName;
                if (name.Length > 0 && name != _syncedName)
                {
                    if (!await Within(auth.UpdatePlayerNameAsync(name)))
                    {
                        return false;
                    }

                    _syncedName = name;
                }

                return auth.IsSignedIn;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + "서버에 연결하지 못했습니다: " + exception.Message);
                return false;
            }
        }

        // 무한 모드 최고 기록을 올린다. 닉네임이 없거나, 이미 올린 기록보다 낮거나, 말이 안 되는 기록이면 올리지 않는다(0).
        // 올리면 순위(1부터)를, 연결에 실패하면 NetworkFailed를 돌려준다.
        public static async Task<int> SubmitAsync(int kills, float seconds, string curseIds)
        {
            SaveData save = SaveSystem.Data;
            if (!IsAvailable || !HasName || IsBlockedInEditor || kills <= 0 || kills <= save.LeaderboardSubmittedKills)
            {
                return 0;
            }

            if (seconds <= 0f || kills / seconds > _data.MaxKillsPerSecond)
            {
                Debug.LogWarning(LogPrefix + $"말이 안 되는 기록이라 올리지 않습니다: {kills}마리 / {seconds:0.0}초");
                return 0;
            }

            if (!await EnsureReadyAsync())
            {
                return NetworkFailed;
            }

            try
            {
                var options = new AddPlayerScoreOptions
                {
                    Metadata = new Dictionary<string, object>
                    {
                        { LeaderboardMeta.CursesKey, curseIds ?? "" },
                        { LeaderboardMeta.SecondsKey, Mathf.Round(seconds) }
                    }
                };
                Task<LeaderboardEntry> request = LeaderboardsService.Instance.AddPlayerScoreAsync(_data.LeaderboardId, kills, options);
                if (!await Within(request))
                {
                    return NetworkFailed;
                }

                save.LeaderboardSubmittedKills = kills;
                SaveSystem.Save();
                return request.Result.Rank + 1;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + "기록을 올리지 못했습니다: " + exception.Message);
                return NetworkFailed;
            }
        }

        // 한 페이지(0부터)의 순위를 받아 온다. 전체 인원은 total로 돌려준다(-1이면 실패).
        // 돌려준 목록은 다음 호출 때 다시 채워지므로 바로 그린다.
        public static async Task<IReadOnlyList<LeaderboardRow>> GetPageAsync(int page, Action<int> setTotal)
        {
            _rows.Clear();
            if (!await EnsureReadyAsync())
            {
                setTotal(-1);
                return _rows;
            }

            try
            {
                var options = new GetScoresOptions { Offset = page * PageSize, Limit = PageSize, IncludeMetadata = true };
                Task<LeaderboardScoresPage> request = LeaderboardsService.Instance.GetScoresAsync(_data.LeaderboardId, options);
                if (!await Within(request))
                {
                    setTotal(-1);
                    return _rows;
                }

                List<LeaderboardEntry> results = request.Result.Results;
                for (int i = 0; i < results.Count; i++)
                {
                    _rows.Add(ToRow(results[i]));
                }

                setTotal(request.Result.Total);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + "순위를 불러오지 못했습니다: " + exception.Message);
                setTotal(-1);
            }

            return _rows;
        }

        // 내 순위(1부터). 아직 기록이 없거나 실패하면 0.
        public static async Task<int> GetMyRankAsync()
        {
            if (!HasName || !await EnsureReadyAsync())
            {
                return 0;
            }

            try
            {
                Task<LeaderboardEntry> request = LeaderboardsService.Instance.GetPlayerScoreAsync(_data.LeaderboardId);
                if (!await Within(request))
                {
                    return 0;
                }

                return request.Result.Rank + 1;
            }
            catch (Exception)
            {
                // 기록이 없으면 서버가 "찾을 수 없음"으로 답한다. 실패와 구분할 필요가 없어 둘 다 없음으로 본다.
                return 0;
            }
        }

        private static LeaderboardRow ToRow(LeaderboardEntry entry)
        {
            string curses = "";
            if (!string.IsNullOrEmpty(entry.Metadata))
            {
                try
                {
                    curses = JsonUtility.FromJson<LeaderboardMeta>(entry.Metadata).curses ?? "";
                }
                catch (ArgumentException)
                {
                    // 곁들인 정보가 깨졌으면 저주만 비우고 나머지는 보여 준다.
                }
            }

            bool isMine = AuthenticationService.Instance.IsSignedIn && entry.PlayerId == AuthenticationService.Instance.PlayerId;
            return new LeaderboardRow(entry.Rank + 1, StripTag(entry.PlayerName), (int)entry.Score, curses, isMine);
        }

        // 서버 이름 "이름#1234"에서 같은 이름 구분 번호를 뗀다.
        private static string StripTag(string serverName)
        {
            if (string.IsNullOrEmpty(serverName))
            {
                return "";
            }

            int hash = serverName.LastIndexOf('#');
            return hash < 0 ? serverName : serverName.Substring(0, hash);
        }

        // 웹 빌드는 Task.Delay(스레드 타이머)가 동작하지 않아, 프레임마다 실제 시간을 보며 기다린다. 제한 시간을 넘기면 false.
        private static async Task<bool> Within(Task request)
        {
            float timeout = _data != null ? _data.RequestTimeout : 8f;
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!request.IsCompleted)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Debug.LogWarning(LogPrefix + $"요청이 {timeout:0}초 안에 끝나지 않았습니다");
                    return false;
                }

                await Awaitable.NextFrameAsync();
            }

            // 실패한 요청은 여기서 예외를 다시 던져 호출한 곳의 catch로 보낸다.
            await request;
            return true;
        }
    }
}
