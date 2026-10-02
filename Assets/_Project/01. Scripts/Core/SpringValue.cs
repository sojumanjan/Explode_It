using UnityEngine;

namespace ExplodeIt.Core
{
    // 목표값을 출렁이며 따라가는 값. 그림 트윈에서 상태가 바뀔 때 툭 끊기지 않고 탄성 있게 넘어가게 한다.
    // 트윈 객체를 만들지 않으므로 적 수십 마리가 매 프레임 써도 할당이 없다.
    public struct SpringValue
    {
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
            // 반 고정 오일러. 히트스톱 직후처럼 프레임이 길어도 튀지 않게 한 스텝 길이를 제한한다.
            deltaTime = Mathf.Min(deltaTime, 1f / 30f);
            Velocity += (-omega * omega * (Value - target) - 2f * damping * omega * Velocity) * deltaTime;
            Value += Velocity * deltaTime;
        }

        public void Snap(float value)
        {
            Value = value;
            Velocity = 0f;
        }
    }
}
