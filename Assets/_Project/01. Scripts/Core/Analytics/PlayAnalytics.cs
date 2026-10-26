using System;
using System.Text;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

namespace ExplodeIt.Core
{
    // 플레이 기록을 Unity Analytics로 보내는 창구. 기록기들은 이벤트를 만들어 여기로 넘기기만 한다.
    // 플레이어가 동의했을 때만 서비스를 켜고 수집을 허락한다. 동의를 거두면 그 자리에서 수집을 멈춘다.
    // 씬마다 놓인 AnalyticsBootstrap이 설정을 넘겨 시작한다(처음 넘긴 설정을 계속 쓴다).
    public static class PlayAnalytics
    {
        private static AnalyticsData _data;
        private static bool _isInitializing;
        private static bool _isReady;
        private static readonly StringBuilder _log = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _data = null;
            _isInitializing = false;
            _isReady = false;
        }

        // 에디터에서 보내지 않도록 했으면 콘솔에만 찍는다.
        private static bool CanSend => _data != null && _data.IsEnabled && SaveSystem.Data.AnalyticsEnabled
            && (!Application.isEditor || _data.SendFromEditor);

        // 기록에 붙는 익명 사용자 번호. 설문 답과 기록을 같은 사람끼리 이어 보려고 설문 링크에 붙인다.
        // 수집을 거절했거나 서비스가 아직 안 켜졌으면 빈 문자열(기록이 없으니 이을 것도 없다).
        public static string UserId => _isReady && CanSend ? AnalyticsService.Instance.GetAnalyticsUserID() : "";

        public static void Configure(AnalyticsData data)
        {
            if (_data != null || data == null)
            {
                return;
            }

            _data = data;
            Apply();
        }

        // 동의 창·설정에서 고른 답을 저장하고 바로 반영한다.
        public static void SetConsent(bool isGranted)
        {
            SaveData save = SaveSystem.Data;
            save.AnalyticsAsked = true;
            save.AnalyticsEnabled = isGranted;
            SaveSystem.Save();
            Apply();
        }

        // 저장을 초기화하는 등 동의 값이 밖에서 바뀌었을 때 다시 맞춘다.
        public static void Apply()
        {
            if (_data == null)
            {
                return;
            }

            if (!CanSend)
            {
                if (_isReady)
                {
                    EndUserConsent.SetConsentState(new ConsentState { AnalyticsIntent = ConsentStatus.Denied });
                }

                return;
            }

            if (_isReady)
            {
                EndUserConsent.SetConsentState(new ConsentState { AnalyticsIntent = ConsentStatus.Granted });
                return;
            }

            StartServices();
        }

        public static void Send(CustomEvent analyticsEvent, string name)
        {
            if (_data == null)
            {
                return;
            }

            if (_data.LogEvents && Debug.isDebugBuild)
            {
                _log.Clear();
                _log.Append("[PlayAnalytics] ").Append(name);
                foreach (object item in analyticsEvent)
                {
                    _log.Append(' ').Append(item);
                }

                Debug.Log(_log.ToString());
            }

            if (_isReady && CanSend)
            {
                AnalyticsService.Instance.RecordEvent(analyticsEvent);
            }
        }

        // 모아 둔 이벤트를 바로 보낸다. 웹은 창을 닫으면 모아 둔 것이 사라지므로 판이 끝날 때 부른다.
        public static void Flush()
        {
            if (_isReady && CanSend)
            {
                AnalyticsService.Instance.Flush();
            }
        }

        // 서비스는 처음 동의했을 때 한 번만 켠다. 웹에서는 켜는 데 시간이 걸려, 그 사이 이벤트는 버린다(판 시작 전에 끝난다).
        private static async void StartServices()
        {
            if (_isInitializing)
            {
                return;
            }

            _isInitializing = true;
            try
            {
                await UnityServices.InitializeAsync();
                _isReady = true;
                Apply();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PlayAnalytics] 서비스를 켜지 못했습니다: {exception.Message}");
            }
            finally
            {
                _isInitializing = false;
            }
        }
    }
}
