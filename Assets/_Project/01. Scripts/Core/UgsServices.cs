// Unity Gaming Services를 한 번만 켜는 공용 창구(플레이 기록과 순위표가 같이 쓴다)
using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;

namespace ExplodeIt.Core
{
    // 서비스를 켜는 것과 수집 허락은 별개다. 순위표가 먼저 서비스를 켜도 플레이 기록은 동의 값에 따라 PlayAnalytics가 막는다.
    // 웹에서는 켜는 데 시간이 걸리므로, 여러 곳에서 동시에 불러도 같은 작업 하나를 기다리게 한다.
    public static class UgsServices
    {
        private static Task _initialization;

        // 서비스가 켜진 순간 한 번 알린다. 이미 켜진 뒤에 구독하면 오지 않으니 IsReady를 먼저 본다.
        public static event Action Ready;

        public static bool IsReady => UnityServices.State == ServicesInitializationState.Initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _initialization = null;
            Ready = null;
        }

        // 실패했던 작업은 다음 호출에서 다시 시도한다(웹에서 잠깐 연결이 끊긴 경우).
        public static Task InitializeAsync()
        {
            if (_initialization == null || _initialization.IsFaulted || _initialization.IsCanceled)
            {
                _initialization = Initialize();
            }

            return _initialization;
        }

        private static async Task Initialize()
        {
            if (!IsReady)
            {
                await UnityServices.InitializeAsync();
            }

            Ready?.Invoke();
        }
    }
}
