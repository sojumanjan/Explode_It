using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 적 그림의 상태별 트윈 동작 수치. 판정과 무관하고 그림(Body)만 움직인다.
    // 기울기 각도는 "+ = 바라보는 쪽으로 숙임, - = 뒤로 젖힘"으로 통일한다.
    [CreateAssetMenu(fileName = "Motion_", menuName = "Explode It/Enemies/Enemy Motion")]
    public class EnemyMotionData : ScriptableObject
    {
        [Header("복귀 탄성")]
        [Tooltip("자세가 목표로 돌아가는 빠르기 (Hz). 높을수록 빠릿하게 돌아온다")]
        [SerializeField, Min(0.1f)] private float _springFrequency = 6f;

        [Tooltip("복귀 감쇠 (0~1). 낮을수록 출렁이며 돌아오고, 1이면 출렁임 없이 멈춘다")]
        [SerializeField, Range(0.05f, 1f)] private float _springDamping = 0.45f;

        [Header("이동: 통통 뛰기")]
        [Tooltip("초당 뛰는 횟수 (회/초)")]
        [SerializeField, Min(0f)] private float _hopRate = 3f;

        [Tooltip("뛰는 높이 (유닛)")]
        [SerializeField, Min(0f)] private float _hopHeight = 0.12f;

        [Tooltip("공중에서 위로 늘어나는 정도 (비율). 0.1이면 세로 10% 늘어남")]
        [SerializeField, Min(0f)] private float _hopStretch = 0.08f;

        [Tooltip("착지 순간 납작해지는 정도 (비율). 0.15면 세로 15% 줄어듦")]
        [SerializeField, Min(0f)] private float _hopSquash = 0.15f;

        [Tooltip("한 번 뛸 때마다 좌우로 번갈아 기우는 각도 (도)")]
        [SerializeField, Min(0f)] private float _hopTilt = 6f;

        [Header("예고: 웅크리며 떨기")]
        [Tooltip("예고 끝에 도달하는 크기 배율 (가로, 세로). 가로로 퍼지고 세로로 눌리면 웅크린 모습이 된다")]
        [SerializeField] private Vector2 _telegraphScale = new Vector2(1.12f, 0.85f);

        [Tooltip("예고 끝에 도달하는 기울기 (도). 음수면 뒤로 젖혀 힘을 모으는 모습이 된다")]
        [SerializeField] private float _telegraphLean = -8f;

        [Tooltip("예고 끝에 도달하는 좌우 떨림 폭 (유닛). 예고 시작엔 0이고 끝으로 갈수록 커진다")]
        [SerializeField, Min(0f)] private float _telegraphShake = 0.04f;

        [Tooltip("떨림 빠르기 (회/초)")]
        [SerializeField, Min(0f)] private float _telegraphShakeRate = 30f;

        [Header("공격: 순간 자세 → 탄성 복귀")]
        [Tooltip("공격 순간 공격 방향으로 튀는 거리 (유닛). 음수면 반동으로 뒤로 밀린다")]
        [SerializeField] private float _kickOffset = 0.1f;

        [Tooltip("공격 순간 크기 배율 (가로, 세로)")]
        [SerializeField] private Vector2 _kickScale = new Vector2(1.15f, 0.9f);

        [Tooltip("공격 순간 기울기 (도). 휘두르기는 크게, 사격은 음수로 반동")]
        [SerializeField] private float _kickAngle = 15f;

        [Tooltip("공격이 이어지는 동안(돌진 등) 유지할 크기 배율 (가로, 세로). 한 번에 끝나는 공격은 1,1로 둔다")]
        [SerializeField] private Vector2 _attackHoldScale = Vector2.one;

        [Tooltip("공격이 이어지는 동안 유지할 기울기 (도)")]
        [SerializeField] private float _attackHoldLean;

        [Tooltip("공격 그림 유지 시간 (초). 적 그림 컴포넌트에 공격 그림을 넣었을 때, 공격 순간부터 이 시간 동안 보여주고 걷기 그림으로 돌아간다")]
        [SerializeField, Min(0f)] private float _attackSpriteDuration = 0.35f;

        [Header("회복: 처진 자세")]
        [Tooltip("회복 중 크기 배율 (가로, 세로). 살짝 처져 지금이 반격할 틈임을 보여준다")]
        [SerializeField] private Vector2 _recoverScale = new Vector2(1.05f, 0.93f);

        [Header("기절(그로기): 무너진 자세")]
        [Tooltip("기절 중 크기 배율 (가로, 세로). 보스 그로기처럼 기믹으로 무너진 상태")]
        [SerializeField] private Vector2 _stunnedScale = new Vector2(1.15f, 0.8f);

        [Tooltip("기절 중 기울기 (도). 음수면 뒤로 젖혀 넘어진 모습이 된다")]
        [SerializeField] private float _stunnedLean = -15f;


        [Header("사망: 밀려남 → 부풀기 → 줄어들기 (전체 시간은 적 데이터의 사망 연출 시간)")]
        [Tooltip("플레이어 반대쪽으로 밀려나는 거리 (유닛)")]
        [SerializeField, Min(0f)] private float _deathKnockDistance = 2.5f;

        [Tooltip("밀려나는 구간이 끝나는 지점 (전체 시간 대비 비율)")]
        [SerializeField, Range(0.05f, 0.9f)] private float _deathKnockEnd = 0.45f;

        [Tooltip("최대로 부푸는 지점 (전체 시간 대비 비율). 밀려남이 끝난 뒤여야 한다")]
        [SerializeField, Range(0.1f, 0.95f)] private float _deathPopPeak = 0.6f;

        [Tooltip("부풀 때 최대 크기 배율")]
        [SerializeField, Min(1f)] private float _deathPopScale = 1.2f;

        [Tooltip("죽는 순간 바뀌는 투명도 (0 = 완전 투명, 1 = 불투명). 사라질 때까지 유지한다")]
        [SerializeField, Range(0f, 1f)] private float _deathAlpha = 0.5f;

        public float SpringFrequency => _springFrequency;
        public float SpringDamping => _springDamping;
        public float HopRate => _hopRate;
        public float HopHeight => _hopHeight;
        public float HopStretch => _hopStretch;
        public float HopSquash => _hopSquash;
        public float HopTilt => _hopTilt;
        public Vector2 TelegraphScale => _telegraphScale;
        public float TelegraphLean => _telegraphLean;
        public float TelegraphShake => _telegraphShake;
        public float TelegraphShakeRate => _telegraphShakeRate;
        public float KickOffset => _kickOffset;
        public Vector2 KickScale => _kickScale;
        public float KickAngle => _kickAngle;
        public Vector2 AttackHoldScale => _attackHoldScale;
        public float AttackHoldLean => _attackHoldLean;
        public float AttackSpriteDuration => _attackSpriteDuration;
        public Vector2 RecoverScale => _recoverScale;
        public Vector2 StunnedScale => _stunnedScale;
        public float StunnedLean => _stunnedLean;
        public float DeathKnockDistance => _deathKnockDistance;
        public float DeathKnockEnd => _deathKnockEnd;
        public float DeathPopPeak => Mathf.Max(_deathPopPeak, _deathKnockEnd + 0.01f);
        public float DeathPopScale => _deathPopScale;
        public float DeathAlpha => _deathAlpha;
    }
}
