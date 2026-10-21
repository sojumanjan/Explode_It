using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Player
{
    // 플레이어 체력과 사망. 스토리·무한 모드마다 정한 칸 수만큼 버틴다.
    // 맞고 살아남으면 잠깐 무적이 되어 몸이 반투명해진다. 연달아 맞아 한꺼번에 여러 칸이 사라지는 억울함을 막는다.
    // 죽으면 몸이 납작하게 눌렸다가 쓰러진 그림으로 퐁 바뀌고, 슬로모션이 걸린 채 카메라가 고블린에게 다가간다.
    // 결과 창은 사망 패널의 대기 시간이 지난 뒤 뜬다.
    // 슬로모션은 연출 재생기(히트스톱)로 걸지 않는다. 사망으로 판이 멈추면 재생기가 히트스톱을 풀기 때문이다.
    // 피격 → 판정 → 사망 단계는 HitReceiver가 나누고, 여기서는 판정을 통과한 피격(체력 감소)과 사망만 받는다.
    [RequireComponent(typeof(HitReceiver))]
    public class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private PlayerData _data;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Color _deadColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        // 쓰러질 때 눌리는 크기 배율과 시간(초), 쓰러진 그림이 퐁 돌아오는 시간(초). 그림 전용 연출 값이다.
        private static readonly Vector2 DeathSquash = new Vector2(1.3f, 0.6f);
        private const float DeathSquashTime = 0.1f;
        private const float DeathPopTime = 0.3f;

        private HitReceiver _hitReceiver;
        private Vector3 _bodyScale;
        private float _slowStart = -1f;
        private int _maxHealth;
        private float _hurtUntil;
        private Color _baseColor;

        private void Awake()
        {
            _hitReceiver = GetComponent<HitReceiver>();
            _baseColor = _sprite.color;
            _bodyScale = _sprite.transform.localScale;
            _maxHealth = (RunSetup.Mode == RunMode.Story ? _data.StoryHealth : _data.EndlessHealth) + RunModifiers.ExtraHealth;
            // 저주 "한 방"은 팬던트까지 무시하고 칸 수를 고정한다. 저주로만 체력이 줄어드는 유일한 예외다.
            if (RunCurses.HealthOverride > 0)
            {
                _maxHealth = RunCurses.HealthOverride;
            }

            _hitReceiver.ResetHits(_maxHealth);
        }

        private void Start()
        {
            GameEvents.RaisePlayerHealthChanged(_maxHealth, _maxHealth);
        }

        private void OnEnable()
        {
            _hitReceiver.Damaged += OnDamaged;
            _hitReceiver.Died += OnDied;
        }

        private void OnDisable()
        {
            _hitReceiver.Damaged -= OnDamaged;
            _hitReceiver.Died -= OnDied;
        }

        private void OnDestroy()
        {
            _sprite.transform.DOKill();
        }

        private void Update()
        {
            if (_hurtUntil > 0f && Time.time >= _hurtUntil)
            {
                _hurtUntil = 0f;
                _sprite.color = _baseColor;
            }

            TickDeathSlow();
        }

        // 느린 데서 시작해 점점 빨리 정상 속도로 돌아온다. 일시정지는 사망 뒤에는 열리지 않는다.
        private void TickDeathSlow()
        {
            if (_slowStart < 0f)
            {
                return;
            }

            float t = _data.DeathSlowTime > 0f ? Mathf.Clamp01((Time.unscaledTime - _slowStart) / _data.DeathSlowTime) : 1f;
            Time.timeScale = Mathf.Lerp(_data.DeathSlowScale, 1f, t * t);
            if (t >= 1f)
            {
                _slowStart = -1f;
            }
        }

        private void OnDamaged(HitInfo hit)
        {
            _hitReceiver.GrantInvulnerability(_data.HurtInvulnerability);
            _hurtUntil = Time.time + _data.HurtInvulnerability;
            Color color = _baseColor;
            color.a = _data.HurtAlpha;
            _sprite.color = color;
            FeedbackPlayer.Play(_data.HurtFeedback, transform.position);
            AudioManager.Play(_data.HurtSound);
            GameEvents.RaisePlayerHealthChanged(_hitReceiver.RemainingHits, _maxHealth);
        }

        private void OnDied(HitInfo hit)
        {
            _hurtUntil = 0f;
            _sprite.color = _baseColor;
            PlayDeathPose();
            AudioManager.Play(_data.HurtSound);

            GameEvents.RaiseCameraFocusRequested(transform.position, _data.DeathZoomTime);
            GameEvents.RaiseCameraZoomRequested(_data.DeathZoom, _data.DeathZoomTime);
            GameEvents.RaisePlayerHealthChanged(0, _maxHealth);
            if (hit.IsFromEnemy)
            {
                GameEvents.RaisePlayerKilledBy(hit.Attacker);
            }

            GameEvents.RaisePlayerKillerKnown(hit.Killer);

            GameEvents.RaisePlayerDied();

            // 사망 알림을 받은 쪽(판 상태·연출 재생기)이 속도를 되돌린 뒤에 건다.
            _slowStart = Time.unscaledTime;
            Time.timeScale = _data.DeathSlowScale;
        }

        // 납작하게 눌렸다가, 쓰러진 그림으로 바뀌며 말랑하게 원래 크기로 돌아온다. 슬로모션과 상관없이 실제 시간으로 움직인다.
        private void PlayDeathPose()
        {
            Transform body = _sprite.transform;
            body.DOKill();
            Vector3 squash = new Vector3(_bodyScale.x * DeathSquash.x, _bodyScale.y * DeathSquash.y, _bodyScale.z);
            DOTween.Sequence()
                .Append(body.DOScale(squash, DeathSquashTime).SetEase(Ease.OutQuad))
                .AppendCallback(SwapToDeathSprite)
                .Append(body.DOScale(_bodyScale, DeathPopTime).SetEase(Ease.OutBack))
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void SwapToDeathSprite()
        {
            if (_data.DeathSprite != null)
            {
                _sprite.sprite = _data.DeathSprite;
            }
            else
            {
                _sprite.color = _deadColor;
            }
        }
    }
}
