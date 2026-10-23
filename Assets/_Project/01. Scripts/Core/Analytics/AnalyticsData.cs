using UnityEngine;

namespace ExplodeIt.Core
{
    // 플레이 기록(Unity Analytics) 설정. 무엇을 보내는지는 기록기 코드에, 보낼지 말지는 여기와 플레이어 동의에 달렸다.
    [CreateAssetMenu(fileName = "AnalyticsData", menuName = "Explode It/Analytics Data")]
    public class AnalyticsData : ScriptableObject
    {
        [Tooltip("플레이 기록 기능 전체 켜기. 끄면 동의 여부와 상관없이 아무것도 보내지 않는다")]
        [SerializeField] private bool _isEnabled = true;

        [Tooltip("에디터에서 플레이할 때도 실제로 보낼지. 끄면 에디터에서는 콘솔에만 찍는다(개발 중 기록이 섞이지 않게)")]
        [SerializeField] private bool _sendFromEditor;

        [Tooltip("보내는 이벤트를 콘솔에 찍을지. 에디터와 개발 빌드에서만 찍는다")]
        [SerializeField] private bool _logEvents = true;

        public bool IsEnabled => _isEnabled;
        public bool SendFromEditor => _sendFromEditor;
        public bool LogEvents => _logEvents;
    }
}
