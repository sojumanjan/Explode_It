using UnityEngine;

namespace ExplodeIt.Core
{
    // 목표값을 출렁이며 따라가는 값. 그림 트윈에서 상태가 바뀔 때 툭 끊기지 않고 탄성 있게 넘어가게 한다.
    // 트윈 객체를 만들지 않으므로 적 수십 마리가 매 프레임 써도 할당이 없다.
    public struct SpringValue
    {
        private const float MaxStep = 1f / 120f;

        public float Value;
        public float Velocity;

        public SpringValue(float value)
        {
            Value = value;
            Velocity = 0f;
        }

        // frequency: 초당 진동 수, damping: 1이면 출렁임 없이 멈추고 낮을수록 더 출렁인다.
        public void Step(float target, float frequency, float damping, float deltaTime)
        {
            float omega = 2f * Mathf.PI * frequency;
            // 반 고정 오일러. 히트스톱 직후처럼 프레임이 길어도 튀지 않게 한 프레임 길이를 제한한다.
            deltaTime = Mathf.Min(deltaTime, 1f / 30f);
            // 한 번에 크게 밟으면 프레임이 낮을 때(하드웨어 가속을 끈 웹 등) 출렁임이 커지다 발산한다. 잘게 나눠 밟는다.
            int steps = Mathf.CeilToInt(deltaTime / MaxStep);
            float step = deltaTime / Mathf.Max(steps, 1);
            for (int i = 0; i < steps; i++)
            {
                Velocity += (-omega * omega * (Value - target) - 2f * damping * omega * Velocity) * step;
                Value += Velocity * step;
            }
        }

        public void Snap(float value)
        {
            Value = value;
            Velocity = 0f;
        }
    }
}
