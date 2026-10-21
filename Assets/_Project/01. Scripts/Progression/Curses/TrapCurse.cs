using System;
using System.Collections.Generic;
using ExplodeIt.Core;
using ExplodeIt.Player;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "덫"을 게임 씬에서 돌린다. 주기마다 플레이어 둘레 빈자리에 덫을 깔고, 밟는 순간 덫이 닫히며 플레이어를 친다.
    // 대시 중에는 뛰어넘는다. 플레이어 폭발(폭탄·스킬)이나 운석에 휩쓸린 덫은 부서져 사라지므로 미리 치워 둘 수 있다.
    // 깔린 직후 정해진 시간 동안은 반투명하고 밟아도 발동하지 않는다. 밟지 않은 덫은 수명이 다하면 사라진다.
    public class TrapCurse : CurseBehaviour<TrapCurseData>
    {
        private class Trap
        {
            public CurseCircle Circle;
            public float Age;
        }

        [SerializeField] private CurseCircle _circlePrefab;
        [Tooltip("덫에 맞는 레이어 (플레이어)")]
        [SerializeField] private LayerMask _hitMask;
        [Tooltip("덫이 깔리지 않을 구조물 레이어")]
        [SerializeField] private LayerMask _obstacleMask;
        // 대시 중인지 보고 덫을 뛰어넘게 한다.
        [SerializeField] private PlayerDodge _dodge;

        private readonly List<Trap> _traps = new List<Trap>(16);
        // 덫 정보도 다시 쓴다. 전투 중 할당을 줄인다.
        private readonly Stack<Trap> _spare = new Stack<Trap>(16);
        private ComponentPool<CurseCircle> _pool;
        private Action<CurseCircle> _onVanished;
        private Action<Vector2, float> _onBlast;
        private float _nextTime = float.PositiveInfinity;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            // 닫히며 사라지는 중인 덫까지 넉넉히 둔다.
            int capacity = Data.MaxTraps + Data.Count;
            _pool = new ComponentPool<CurseCircle>(_circlePrefab, transform, capacity, capacity * 2);
            _pool.Prewarm(capacity);
            _onVanished = Release;
            _onBlast = BreakWithin;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GameEvents.PlayerBlastExploded += _onBlast;
            MeteorRainCurse.Impacted += _onBlast;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameEvents.PlayerBlastExploded -= _onBlast;
            MeteorRainCurse.Impacted -= _onBlast;
        }

        protected override void OnRunStarted()
        {
            FeedbackPlayer.Prepare(Data.TriggerFeedback);
            FeedbackPlayer.Prepare(Data.BreakFeedback);
            _nextTime = Time.time + Data.FirstDelay;
        }

        private void Update()
        {
            TickTraps(RunCurses.DeltaTime);
            if (!IsRunning)
            {
                _nextTime += Time.deltaTime;
                return;
            }

            if (Time.time < _nextTime)
            {
                return;
            }

            _nextTime = Time.time + Data.Interval;
            Vector2 player = PlayerPosition;
            for (int i = 0; i < Data.Count; i++)
            {
                if (TryPickSpot(player, Data.MinDistance, Data.MaxDistance, Data.TriggerRadius, _obstacleMask, out Vector2 spot))
                {
                    Place(spot);
                }
            }

            AudioManager.Play(Data.PlaceSound);
        }

        private void Place(Vector2 spot)
        {
            // 가장 오래된 덫부터 치운다.
            if (_traps.Count >= Data.MaxTraps)
            {
                Remove(0);
            }

            Trap trap = _spare.Count > 0 ? _spare.Pop() : new Trap();
            trap.Circle = _pool.Get(spot);
            trap.Age = 0f;
            trap.Circle.Show(spot, Data.TriggerRadius, Data.IdleColor, Data.IdleColor);
            trap.Circle.SetSprite(Data.TrapSprite, Color.white);
            trap.Circle.SetOpacity(0f);
            _traps.Add(trap);
        }

        private void TickTraps(float deltaTime)
        {
            if (_traps.Count == 0)
            {
                return;
            }

            Vector2 player = PlayerPosition;
            float sqrTrigger = Data.TriggerRadius * Data.TriggerRadius;
            bool canStep = IsRunning && (_dodge == null || !_dodge.IsDodging);
            for (int i = _traps.Count - 1; i >= 0; i--)
            {
                Trap trap = _traps[i];
                trap.Age += deltaTime;
                if (trap.Age >= Data.Lifetime)
                {
                    Remove(i);
                    continue;
                }

                // 발동 전에는 반투명하게 보여 "아직 안전하다"는 걸 알리고, 발동하는 순간 진해진다.
                if (trap.Age < Data.ArmDelay)
                {
                    float appear = Data.AppearTime > 0f ? Mathf.Clamp01(trap.Age / Data.AppearTime) : 1f;
                    trap.Circle.SetOpacity(appear * Data.UnarmedAlpha);
                    continue;
                }

                trap.Circle.SetOpacity(1f);

                if (canStep && (player - trap.Circle.Position).sqrMagnitude <= sqrTrigger)
                {
                    Spring(i);
                }
            }
        }

        // 밟힌 덫: 바로 플레이어를 치고, 탁 닫히며 사라진다. 피격 후 무적이면 맞지 않고 덫만 닫힌다.
        private void Spring(int index)
        {
            CurseCircle circle = _traps[index].Circle;
            CurseHits.Circle(circle.Position, Data.TriggerRadius, _hitMask, Data);
            FeedbackPlayer.Play(Data.TriggerFeedback, circle.Position);
            AudioManager.Play(Data.TriggerSound);
            Detach(index);
            circle.HideRange();
            circle.PlaySpriteSnap(Data.SnapVanishTime);
            circle.Countdown(Data.SnapVanishTime, false, 0f, _onVanished);
        }

        // 폭발 범위 안에 중심이 든 덫을 부순다(폭탄과 같은 규칙).
        private void BreakWithin(Vector2 center, float radius)
        {
            float sqrRadius = radius * radius;
            for (int i = _traps.Count - 1; i >= 0; i--)
            {
                Vector2 position = _traps[i].Circle.Position;
                if ((position - center).sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                FeedbackPlayer.Play(Data.BreakFeedback, position);
                AudioManager.Play(Data.BreakSound);
                Remove(i);
            }
        }

        private void Remove(int index)
        {
            CurseCircle circle = _traps[index].Circle;
            Detach(index);
            _pool.Release(circle);
        }

        // 목록에서만 뺀다. 그림은 부른 쪽이 정리한다.
        private void Detach(int index)
        {
            Trap trap = _traps[index];
            _traps.RemoveAt(index);
            trap.Circle = null;
            _spare.Push(trap);
        }

        private void Release(CurseCircle circle)
        {
            _pool.Release(circle);
        }
    }
}
