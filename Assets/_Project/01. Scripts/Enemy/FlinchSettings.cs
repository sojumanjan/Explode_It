using System;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 보스가 아픈 듯 움찔하는 연출 값. 보스 실드를 붙잡은 과녁(야수 혼불, 법사 요정)이 터질 때 쓴다.
    [Serializable]
    public class FlinchSettings
    {
        [Tooltip("과녁 반대쪽으로 튕겨 밀리는 거리 (유닛). 그림만 움직이고 판정 위치는 그대로다")]
        [SerializeField, Min(0f)] private float _distance = 0.3f;
        [Tooltip("움찔이 돌아오는 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _duration = 0.25f;
        [Tooltip("움찔하는 순간 납작해지는 정도 (비율). 0.15면 가로 15% 넓어지고 세로 15% 줄었다가 돌아온다")]
        [SerializeField, Range(0f, 0.5f)] private float _squash = 0.15f;
        [Tooltip("움찔할 때 번쩍이는 색")]
        [SerializeField] private Color _flashColor = new Color(1f, 0.55f, 0.45f, 1f);
        [Tooltip("번쩍이는 시간 (초)")]
        [SerializeField, Min(0f)] private float _flashDuration = 0.1f;

        public float Distance => _distance;
        public float Duration => _duration;
        public float Squash => _squash;
        public Color FlashColor => _flashColor;
        public float FlashDuration => _flashDuration;
    }
}
