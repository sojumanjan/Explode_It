using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Player
{
    // 구르기 회피. 한 방 사망 게임에서 "읽었는데 몸이 못 따라가는" 순간을 구제하는 수단이라
    // 무적은 짧게, 쿨타임은 남발하지 못할 만큼 둔다.
    [CreateAssetMenu(fileName = "DodgeData", menuName = "Explode It/Dodge Data")]
    public class DodgeData : ScriptableObject
    {
        [Tooltip("구르는 거리 (유닛). 구르는 시간 동안 이 거리를 일정한 속도로 이동한다. 벽에 막히면 그 앞에서 멈춘다")]
        [SerializeField, Min(0f)] private float _distance = 2f;

        [Tooltip("구르는 시간 (초). 이 동안은 일반 이동 입력이 먹지 않는다")]
        [SerializeField, Min(0.01f)] private float _duration = 0.25f;

        [Tooltip("무적 시간 (초). 구르기를 시작한 순간부터 센다")]
        [SerializeField, Min(0f)] private float _invulnerableDuration = 0.25f;

        [Tooltip("쿨타임 (초). 구르기를 시작한 순간부터 다음 구르기까지")]
        [SerializeField, Min(0f)] private float _cooldown = 1.5f;

        [Header("이어 구르기 (아티팩트 가죽 신발)")]
        [Tooltip("구르기가 끝난 뒤 이 시간(초) 안에 다시 누르면, 이 시간이 다 지나는 순간 쿨타임 없이 한 번 더 구른다. 두 구르기 사이의 쉬는 틈이기도 하다")]
        [SerializeField, Min(0f)] private float _chainWindow = 0.3f;

        [Tooltip("구르기가 끝나기 이 시간(초) 전부터 누른 것도 이어 구르기로 예약한다. 구르는 시간보다 짧게 둬야 아무 때나 눌러도 이어지는 일이 없다")]
        [SerializeField, Min(0f)] private float _chainBufferTime = 0.05f;

        [Tooltip("구르기 효과음. 비워 두면 소리 없이 구른다")]
        [SerializeField] private SoundData _sound;

        [Tooltip("구르기 무적으로 공격을 흘려 낸 순간의 연출(슬로모션 시간·배속 등). 구르기 한 번에 한 번만 나온다. 비워 두면 연출 없음")]
        [SerializeField] private FeedbackData _evadeFeedback;

        public float Distance => _distance;
        public float Duration => _duration;
        public float InvulnerableDuration => _invulnerableDuration;
        public float Cooldown => _cooldown;
        public float ChainWindow => _chainWindow;
        public float ChainBufferTime => _chainBufferTime;
        public SoundData Sound => _sound;
        public FeedbackData EvadeFeedback => _evadeFeedback;
    }
}
