using System;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 크레딧 장면 대본의 한 줄: 몇 초에, 어느 배우가, 무엇을 하는지.
    [Serializable]
    public class CreditsShotStep
    {
        [Tooltip("장면이 밝아진 뒤 이 동작을 시작하는 시각 (초)")]
        [SerializeField, Min(0f)] private float _time;

        [Tooltip("배우 번호. 씬의 장면(CreditsShot) 배우 목록 순서 (0부터)")]
        [SerializeField, Min(0)] private int _actor;

        [Tooltip("이 순간 배우 그림을 이 그림으로 바꾼다(표정 등). 비우면 그대로 둔다")]
        [SerializeField] private Sprite _sprite;

        [Tooltip("동작 종류")]
        [SerializeField] private CreditsShotAction _action;

        [Tooltip("세기. 뽀잉 = 커지는 비율, 떨기 = 좌우 폭 (유닛), 콩콩 = 뛰는 높이 (유닛)")]
        [SerializeField, Min(0f)] private float _strength = 0.2f;

        [Tooltip("옮겨 갈 거리 (유닛). 옮기기와 콩콩(뛰면서 옮겨 가는 걷기)에서 쓴다")]
        [SerializeField] private Vector2 _move;

        [Tooltip("동작에 걸리는 시간 (초). 콩콩은 전체 시간")]
        [SerializeField, Min(0.01f)] private float _duration = 0.3f;

        [Tooltip("콩콩 뛰는 횟수. 떨기에서는 흔들림 수 배율(1초쯤에 1, 길게 떨면 초만큼)")]
        [SerializeField, Min(1)] private int _count = 1;

        public float Time => _time;
        public int Actor => _actor;
        public Sprite Sprite => _sprite;
        public CreditsShotAction Action => _action;
        public float Strength => _strength;
        public Vector2 Move => _move;
        public float Duration => _duration;
        public int Count => _count;
    }
}
