// 크레딧 왼쪽에 비추는 짧은 장면 하나의 대본. 배우와 장소는 씬의 CreditsShot이 갖고, 여기는 시간과 동작만 적는다.
using UnityEngine;

namespace ExplodeIt.Stage
{
    [CreateAssetMenu(fileName = "CreditsShot", menuName = "Explode It/Story/Credits Shot")]
    public class CreditsShotData : ScriptableObject
    {
        [Tooltip("카메라 배율. 1이면 평소 크기, 작을수록 가깝다")]
        [SerializeField, Range(0.1f, 1.5f)] private float _zoom = 0.45f;

        [Tooltip("까만 화면에서 장면이 밝아지는 시간이자 끝나고 다시 까매지는 시간 (초, 각각)")]
        [SerializeField, Min(0.05f)] private float _fadeTime = 0.8f;

        [Tooltip("장면이 다 밝아진 뒤 머무는 시간 (초). 이 시간이 지나면 까매지며 다음 장면으로 넘어간다")]
        [SerializeField, Min(0.5f)] private float _holdTime = 8f;

        [Tooltip("대본. 시각 순서와 상관없이 적어도 된다")]
        [SerializeField] private CreditsShotStep[] _steps;

        public float Zoom => _zoom;
        public float FadeTime => _fadeTime;
        public float HoldTime => _holdTime;
        public CreditsShotStep[] Steps => _steps;
    }
}
