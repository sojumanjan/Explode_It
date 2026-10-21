using System;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "기습"의 표시를 그린다. 자리 고르기와 스폰은 스테이지 진행이 하고, 나오기 전에 알림을 보내면 그 자리에 원을 깐다.
    // 원은 적이 나오는 순간까지 차오른다. 폭발이 아니라 "여기서 나온다"는 표시다.
    public class AmbushCurse : CurseBehaviour<AmbushCurseData>
    {
        [SerializeField] private CurseCircle _circlePrefab;
        [SerializeField, Min(1)] private int _prewarm = 6;
        [SerializeField, Min(1)] private int _maxCircles = 24;

        private ComponentPool<CurseCircle> _pool;
        private Action<CurseCircle> _onDone;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            _pool = new ComponentPool<CurseCircle>(_circlePrefab, transform, _prewarm, _maxCircles);
            _pool.Prewarm(_prewarm);
            _onDone = Release;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GameEvents.SpawnWarned += OnSpawnWarned;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameEvents.SpawnWarned -= OnSpawnWarned;
        }

        private void OnSpawnWarned(Vector2 position, float radius, float duration)
        {
            CurseCircle circle = _pool.Get(position);
            circle.Show(position, radius, Data.RangeColor, Data.FillColor);
            circle.Countdown(duration, true, 0f, _onDone);
            AudioManager.Play(Data.WarnSound);
        }

        private void Release(CurseCircle circle)
        {
            _pool.Release(circle);
        }
    }
}
