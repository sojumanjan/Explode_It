using System;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 무한 모드 웨이브의 리듬 한 구간(소개, 섞기, 쉼, 압박 등). 구간 마리 수를 구간 길이에 고르게 펼쳐 박자를 낸다.
    // 박자는 스토리 웨이브에서 쓰는 세 가지를 따른다. 단독(1구역 1마리), 포위(여러 구역 1마리씩 동시에), 뭉치(1구역 여러 마리).
    // 스토리는 단독이 절반 이상이고 뭉치는 양념이라, 비율로 그 느낌을 맞춘다. 마리 수가 0이면 쉬는 구간이다.
    [Serializable]
    public class EndlessSegment
    {
        [Tooltip("구간 이름. 알아보기 위한 이름일 뿐 동작에는 영향이 없다")]
        [SerializeField] private string _label = "구간";

        [Tooltip("구간 길이 (초). 1웨이브 기준이고, 웨이브가 쌓일수록 무한 모드 데이터의 줄어드는 비율만큼 짧아진다")]
        [SerializeField, Min(0f)] private float _duration = 10f;

        [Tooltip("구간 마리 수 (마리). 0이면 쉬는 구간. 모든 구간의 합이 웨이브당 스폰 수(100)와 같아야 한다")]
        [SerializeField, Min(0)] private int _count = 10;

        [Header("박자 비율")]
        [Tooltip("단독 박자 가중치. 한 구역에서 1마리")]
        [SerializeField, Min(0f)] private float _singleWeight = 60f;

        [Tooltip("포위 박자 가중치. 여러 구역에서 1마리씩 동시에. 웨이브가 쌓일수록 무한 모드 데이터의 상승분만큼 커진다")]
        [SerializeField, Min(0f)] private float _surroundWeight = 25f;

        [Tooltip("뭉치 박자 가중치. 한 구역에서 여러 마리가 연달아. 웨이브가 쌓일수록 무한 모드 데이터의 상승분만큼 커진다")]
        [SerializeField, Min(0f)] private float _clumpWeight = 15f;

        [Header("박자 크기")]
        [Tooltip("포위 구역 수 최소 (개)")]
        [SerializeField, Min(2)] private int _surroundAreasMin = 2;

        [Tooltip("포위 구역 수 최대 (개). 웨이브가 쌓이면 무한 모드 데이터의 추가 구역 수만큼 더해진다")]
        [SerializeField, Min(2)] private int _surroundAreasMax = 3;

        [Tooltip("뭉치 크기 최소 (마리)")]
        [SerializeField, Min(2)] private int _clumpSizeMin = 2;

        [Tooltip("뭉치 크기 최대 (마리). 적마다 정한 최대 무리 크기를 넘지 않는다")]
        [SerializeField, Min(2)] private int _clumpSizeMax = 3;

        public string Label => _label;
        public float Duration => _duration;
        public int Count => _count;
        public float SingleWeight => _singleWeight;
        public float SurroundWeight => _surroundWeight;
        public float ClumpWeight => _clumpWeight;
        public int SurroundAreasMin => _surroundAreasMin;
        public int SurroundAreasMax => Mathf.Max(_surroundAreasMin, _surroundAreasMax);
        public int ClumpSizeMin => _clumpSizeMin;
        public int ClumpSizeMax => Mathf.Max(_clumpSizeMin, _clumpSizeMax);
    }
}
