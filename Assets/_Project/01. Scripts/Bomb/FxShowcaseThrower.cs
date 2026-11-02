using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Bombs
{
    // 이펙트 확인 씬(PixelFxTest) 전용. 플레이어 없이 폭탄과 블랙홀을 정해진 간격으로 던져, 방패 막힘과 블랙홀 연출을 실제 판정으로 보여 준다.
    // 폭탄은 방패병 정면(방패병이 바라보는 허수아비 쪽) 바로 앞에 떨궈 늘 막히게 한다.
    public class FxShowcaseThrower : MonoBehaviour
    {
        [Header("공통")]
        [Tooltip("던질 때 쓰는 무기 (비행 시간, 포물선 높이, 폭발 반경, 폭발 시간)")]
        [SerializeField] private WeaponData _weapon;

        [Header("방패 앞 폭탄")]
        [Tooltip("던질 폭탄 (씬에 꺼 둔 것). 비워 두면 던지지 않는다")]
        [SerializeField] private Bomb _bomb;
        [Tooltip("방패병")]
        [SerializeField] private Transform _shieldBearer;
        [Tooltip("방패병이 바라보는 쪽 (허수아비). 이 방향으로 떨궈야 정면이라 막힌다")]
        [SerializeField] private Transform _shieldFacing;
        [Tooltip("방패병 몸에서 정면으로 얼마나 앞에 떨굴지 (유닛)")]
        [SerializeField, Min(0f)] private float _bombFrontDistance = 1f;
        [Tooltip("폭탄을 던지는 간격 (초)")]
        [SerializeField, Min(0.5f)] private float _bombInterval = 2.5f;

        [Header("블랙홀")]
        [Tooltip("던질 블랙홀 (씬에 꺼 둔 것). 비워 두면 던지지 않는다")]
        [SerializeField] private BlackHoleBomb _blackHole;
        [Tooltip("블랙홀을 떨굴 자리")]
        [SerializeField] private Transform _blackHoleTarget;
        [Tooltip("블랙홀 반경 (유닛). 게임 기본값 4 (Ability_BlackHole)")]
        [SerializeField, Min(0.1f)] private float _blackHoleRadius = 4f;
        [Tooltip("블랙홀 흡입 시간 (초). 게임 기본값 2")]
        [SerializeField, Min(0.1f)] private float _pullDuration = 2f;
        [Tooltip("블랙홀이 적을 끄는 속도 (유닛/초)")]
        [SerializeField, Min(0f)] private float _pullSpeed = 4f;
        [Tooltip("블랙홀이 끝나고 다음을 던지기까지 쉬는 시간 (초)")]
        [SerializeField, Min(0f)] private float _blackHoleRest = 1.5f;

        private WeaponStats _stats;
        private float _bombTimer;
        private float _blackHoleTimer;
        private bool _isBombBusy;
        private bool _isBlackHoleBusy;

        private void Start()
        {
            _stats = new WeaponStats(_weapon);
            _bombTimer = 1f;
            _blackHoleTimer = 1f;
        }

        private void Update()
        {
            if (_bomb != null && _shieldBearer != null && _shieldFacing != null && !_isBombBusy)
            {
                _bombTimer -= Time.deltaTime;
                if (_bombTimer <= 0f)
                {
                    ThrowBomb();
                }
            }

            if (_blackHole != null && _blackHoleTarget != null && !_isBlackHoleBusy)
            {
                _blackHoleTimer -= Time.deltaTime;
                if (_blackHoleTimer <= 0f)
                {
                    ThrowBlackHole();
                }
            }
        }

        private void ThrowBomb()
        {
            _isBombBusy = true;
            Vector2 bearer = _shieldBearer.position;
            Vector2 front = ((Vector2)_shieldFacing.position - bearer).normalized;
            _bomb.transform.position = transform.position;
            _bomb.gameObject.SetActive(true);
            _bomb.Launch(bearer + front * _bombFrontDistance, _stats, bomb =>
            {
                bomb.gameObject.SetActive(false);
                _isBombBusy = false;
                _bombTimer = _bombInterval;
            });
        }

        private void ThrowBlackHole()
        {
            _isBlackHoleBusy = true;
            _blackHole.transform.position = transform.position;
            _blackHole.gameObject.SetActive(true);
            _blackHole.Launch(_blackHoleTarget.position, _blackHoleRadius, _pullDuration, _pullSpeed, _stats, () =>
            {
                _blackHole.gameObject.SetActive(false);
                _isBlackHoleBusy = false;
                _blackHoleTimer = _blackHoleRest;
            });
        }
    }
}
