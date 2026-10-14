using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Bombs
{
    // 최대 사거리를 플레이어 둘레에 원으로 그린다. 플레이어의 자식으로 두면 따라다닌다.
    [RequireComponent(typeof(LineRenderer))]
    public class ThrowRangeIndicator : MonoBehaviour
    {
        [SerializeField, Min(8)] private int _segments = 64;

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.loop = true;
        }

        // 보스를 쓰러뜨린 뒤 처치 대화·엔딩 연출 동안은 사거리 원이 몰입을 깨서 숨긴다. 다음 웨이브나 새 판이 시작되면 다시 보인다.
        private void OnEnable()
        {
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.WaveStarted += OnWaveStarted;
            GameEvents.StageStarted += OnStageStarted;
            GameEvents.PlayerControlLockChanged += OnControlLockChanged;
        }

        private void OnDisable()
        {
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.WaveStarted -= OnWaveStarted;
            GameEvents.StageStarted -= OnStageStarted;
            GameEvents.PlayerControlLockChanged -= OnControlLockChanged;
        }

        private void OnBossDefeated(int bossNumber)
        {
            _line.enabled = false;
        }

        private void OnWaveStarted(int wave)
        {
            _line.enabled = true;
        }

        private void OnStageStarted(bool isStoryMode)
        {
            _line.enabled = true;
        }

        // 오프닝·보스 대화처럼 조작이 막힌 연출 동안도 숨긴다. 쓰러진 보스 대화는 조작이 끝까지 막혀 있어 다시 보이지 않는다.
        private void OnControlLockChanged(bool isLocked)
        {
            _line.enabled = !isLocked;
        }

        // 사거리가 바뀔 때만 호출한다. 매 프레임 다시 그리지 않는다.
        public void SetRadius(float radius)
        {
            _line.positionCount = _segments;
            float step = Mathf.PI * 2f / _segments;
            for (int i = 0; i < _segments; i++)
            {
                float angle = step * i;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }
    }
}
