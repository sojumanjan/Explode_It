using System;
using System.Collections.Generic;
using ExplodeIt.Stage;
using Unity.Services.Analytics;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 게임 씬에서 판 하나의 플레이 기록을 모아 보낸다. 다른 시스템은 건드리지 않고 게임 이벤트만 듣는다.
    // 판 시작·끝, 보스 시작·끝, 피격, 튜토리얼 과제, 엔딩. 자주 일어나는 일(던지기, 처치 하나하나)은 판 끝에 합계로만 남긴다.
    // 모든 이벤트에 판 번호(run_id)를 붙여 한 판 안의 기록끼리 묶어 볼 수 있게 한다.
    public class RunAnalytics : MonoBehaviour
    {
        // 처치 수·시간·정확도는 결과 창과 같은 기록을 읽는다.
        [SerializeField] private StageStats _stats;

        private readonly List<string> _ids = new List<string>(8);
        private bool _isRunning;
        private bool _isStoryMode;
        private string _runId = "";
        private string _killer = "";
        private int _waveReached;
        private int _bossReached;
        private int _hitsTaken;
        private int _abilityUses;
        private int _abilityKills;
        private int _dashCount;

        // 지금 싸우는 보스. 이름이 비어 있으면 보스전이 아니다.
        private string _boss = "";
        private float _bossStartTime;
        private int _bossHits;

        private void OnEnable()
        {
            GameEvents.StageStarted += OnStageStarted;
            GameEvents.StageCleared += OnStageCleared;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.PlayerKillerKnown += OnKillerKnown;
            GameEvents.PlayerHit += OnPlayerHit;
            GameEvents.WaveStarted += OnWaveStarted;
            GameEvents.BossIntroStarted += OnBossIntroStarted;
            GameEvents.BossAppeared += OnBossAppeared;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.AbilityUsed += OnAbilityUsed;
            GameEvents.EnemyKilledBy += OnEnemyKilledBy;
            GameEvents.PlayerDodged += OnDodged;
            GameEvents.TutorialStepCompleted += OnTutorialStep;
            GameEvents.StoryEndingFinished += OnStoryEnding;
            GameEvents.CreditsFinished += OnCreditsFinished;
        }

        private void OnDisable()
        {
            GameEvents.StageStarted -= OnStageStarted;
            GameEvents.StageCleared -= OnStageCleared;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.PlayerKillerKnown -= OnKillerKnown;
            GameEvents.PlayerHit -= OnPlayerHit;
            GameEvents.WaveStarted -= OnWaveStarted;
            GameEvents.BossIntroStarted -= OnBossIntroStarted;
            GameEvents.BossAppeared -= OnBossAppeared;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.AbilityUsed -= OnAbilityUsed;
            GameEvents.EnemyKilledBy -= OnEnemyKilledBy;
            GameEvents.PlayerDodged -= OnDodged;
            GameEvents.TutorialStepCompleted -= OnTutorialStep;
            GameEvents.StoryEndingFinished -= OnStoryEnding;
            GameEvents.CreditsFinished -= OnCreditsFinished;
            // 판 도중 다시 하기·나가기로 씬이 내려가면 그만둔 판으로 남긴다.
            EndRun("abandoned");
        }

        private void OnStageStarted(bool isStoryMode)
        {
            _isRunning = true;
            _isStoryMode = isStoryMode;
            _runId = Guid.NewGuid().ToString("N").Substring(0, 8);
            _killer = "";
            _waveReached = 0;
            _bossReached = 0;
            _hitsTaken = 0;
            _abilityUses = 0;
            _abilityKills = 0;
            _dashCount = 0;
            _boss = "";

            SaveData save = SaveSystem.Data;
            save.RunCount++;
            var e = new CustomEvent("run_started")
            {
                { "run_id", _runId },
                { "mode", ModeName() },
                { "is_first_run", !save.FirstRunDone },
                { "weapon", RunSetup.WeaponId },
                { "ability", RunSetup.AbilityId },
                { "artifact_1", ArtifactAt(0) },
                { "artifact_2", ArtifactAt(1) },
                { "upgrade_count", save.EquippedUpgradeCount },
                { "curse_count", RunCurses.Count },
                { "curses", Join(RunSetup.CurseIds) }
            };
            PlayAnalytics.Send(e, "run_started");
        }

        private void OnStageCleared()
        {
            EndRun("clear");
        }

        private void OnPlayerDied()
        {
            EndBoss("died");
            EndRun("death");
        }

        private void OnKillerKnown(IKillerProfile killer)
        {
            _killer = NameOf(killer);
        }

        private void OnPlayerHit(HitInfo hit, int remainingHits)
        {
            if (!_isRunning)
            {
                return;
            }

            _hitsTaken++;
            if (_boss.Length > 0)
            {
                _bossHits++;
            }

            var e = new CustomEvent("player_hit")
            {
                { "run_id", _runId },
                { "source", NameOf(hit.Killer) },
                { "source_kind", KindOf(hit) },
                { "wave", _waveReached },
                { "run_sec", Mathf.RoundToInt(_stats.Elapsed) },
                { "hp_left", remainingHits }
            };
            PlayAnalytics.Send(e, "player_hit");
        }

        private void OnWaveStarted(int wave)
        {
            _waveReached = Mathf.Max(_waveReached, wave);
        }

        private void OnBossIntroStarted(int bossNumber)
        {
            _bossReached = Mathf.Max(_bossReached, bossNumber);
        }

        // 보스전 시작(번호)이 먼저 오고 바로 이어서 보스 이름이 온다.
        private void OnBossAppeared(string boss, int lap)
        {
            if (!_isRunning)
            {
                return;
            }

            _boss = boss;
            _bossStartTime = _stats.Elapsed;
            _bossHits = 0;
            var e = new CustomEvent("boss_started")
            {
                { "run_id", _runId },
                { "boss", boss },
                { "boss_number", _bossReached },
                { "lap", lap },
                { "run_sec", Mathf.RoundToInt(_stats.Elapsed) }
            };
            PlayAnalytics.Send(e, "boss_started");
        }

        private void OnBossDefeated(int bossNumber)
        {
            EndBoss("defeated");
        }

        private void EndBoss(string result)
        {
            if (!_isRunning || _boss.Length == 0)
            {
                return;
            }

            var e = new CustomEvent("boss_ended")
            {
                { "run_id", _runId },
                { "boss", _boss },
                { "result", result },
                { "fight_sec", Mathf.RoundToInt(_stats.Elapsed - _bossStartTime) },
                { "hits_taken", _bossHits }
            };
            PlayAnalytics.Send(e, "boss_ended");
            _boss = "";
        }

        private void OnAbilityUsed()
        {
            _abilityUses++;
        }

        private void OnEnemyKilledBy(DamageSource source, EnemyKind kind)
        {
            if (source == DamageSource.BlackHole || source == DamageSource.ScatterBomb || source == DamageSource.InstantBomb)
            {
                _abilityKills++;
            }
        }

        private void OnDodged()
        {
            _dashCount++;
        }

        // 튜토리얼은 판 시작 전에 하므로 판 번호 없이, 게임 씬이 열린 뒤 걸린 시간을 남긴다.
        private void OnTutorialStep(string step)
        {
            var e = new CustomEvent("tutorial_step")
            {
                { "step_name", step },
                { "elapsed_sec", Mathf.RoundToInt(Time.timeSinceLevelLoad) }
            };
            PlayAnalytics.Send(e, "tutorial_step");
        }

        private void OnStoryEnding()
        {
            SendEnding("story", false, 0f);
        }

        private void OnCreditsFinished(bool isSkipped, float fastForwardRatio)
        {
            SendEnding("true", isSkipped, fastForwardRatio);
        }

        private void SendEnding(string ending, bool isSkipped, float fastForwardRatio)
        {
            var e = new CustomEvent("ending_seen")
            {
                { "run_id", _runId },
                { "ending", ending },
                { "credits_skipped", isSkipped },
                { "fast_forward_ratio", Mathf.Round(fastForwardRatio * 100f) / 100f }
            };
            PlayAnalytics.Send(e, "ending_seen");
        }

        // 판 하나의 요약. 누적 정보(몇 번째 판, 총 플레이 시간 등)를 함께 붙여 한 줄만 봐도 이 사람이 어디까지 왔는지 보이게 한다.
        private void EndRun(string result)
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            SaveData save = SaveSystem.Data;
            save.TotalPlaySeconds += _stats.Elapsed;
            var e = new CustomEvent("run_ended")
            {
                { "run_id", _runId },
                { "mode", ModeName() },
                { "result", result },
                { "survival_sec", Mathf.RoundToInt(_stats.Elapsed) },
                // 스토리를 중간부터 시작한 판은 건너뛴 웨이브 몫을 빼고 이번 판에 실제로 잡은 수만 보낸다. 진행 위치는 wave_reached로 본다.
                { "kills", _stats.Kills - _stats.StartKills },
                { "wave_reached", _waveReached },
                { "boss_reached", _bossReached },
                { "killer", result == "death" ? _killer : "" },
                { "hits_taken", _hitsTaken },
                { "accuracy", Mathf.RoundToInt(_stats.Accuracy * 100f) },
                { "ability_uses", _abilityUses },
                { "ability_kills", _abilityKills },
                { "dash_count", _dashCount },
                { "weapon", RunSetup.WeaponId },
                { "curse_count", RunCurses.Count },
                { "run_index", save.RunCount },
                { "total_play_sec", Mathf.RoundToInt(save.TotalPlaySeconds) },
                { "story_cleared", save.StoryCleared },
                { "unlocked_count", save.UnlockedCount }
            };
            PlayAnalytics.Send(e, "run_ended");
            PlayAnalytics.Flush();
            SaveSystem.Save();
        }

        private string ModeName()
        {
            if (_isStoryMode)
            {
                return "story";
            }

            return RunCurses.Has(CurseKind.King) ? "true_ending" : "endless";
        }

        private static string KindOf(HitInfo hit)
        {
            if (hit.Source == DamageSource.Curse)
            {
                return "curse";
            }

            return hit.IsFromEnemy && hit.Attacker == EnemyKind.Boss ? "boss" : "enemy";
        }

        // 데이터 에셋 이름(영문)을 쓴다. 표시 이름(한글)은 바뀔 수 있어 기록끼리 비교하기 어렵다.
        private static string NameOf(IKillerProfile profile)
        {
            if (profile is UnityEngine.Object asset && asset != null)
            {
                return asset.name;
            }

            return profile != null ? profile.KillerName : "";
        }

        private static string ArtifactAt(int index)
        {
            int found = 0;
            IReadOnlyList<string> ids = RunSetup.ArtifactIds;
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.IsNullOrEmpty(ids[i]))
                {
                    continue;
                }

                if (found == index)
                {
                    return ids[i];
                }

                found++;
            }

            return "";
        }

        private string Join(IReadOnlyList<string> ids)
        {
            _ids.Clear();
            for (int i = 0; i < ids.Count; i++)
            {
                if (!string.IsNullOrEmpty(ids[i]))
                {
                    _ids.Add(ids[i]);
                }
            }

            return string.Join(",", _ids);
        }
    }
}
