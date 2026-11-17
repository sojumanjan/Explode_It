// 타격감 일지 촬영용 씬(HitFeelShowcase) 전용. 같은 장면을 연출 전, 후, 적 반응만, 처치 고리만, 픽셀 섬광만, 폭발 파티클만, 히트스톱만으로 바꿔 가며 되풀이한다
using System;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExplodeIt.Bombs
{
    // 플레이어 없이 고블린 자리에서 폭탄을 던진다. 적 무리가 걸어오면 "터질 때 있을 자리"를 계산해 던져, 매번 같은 장면이 찍히게 한다.
    // 게임 코드는 건드리지 않는다. 흔들림, 히트스톱, 파티클은 FeedbackPlayer의 스위치로, 나머지는 오브젝트를 켜고 꺼서 바꾼다.
    // 플레이 중 숫자 키 1~7로 장면을 바꾼다.
    public class HitFeelShowcase : MonoBehaviour
    {
        private enum Shot
        {
            // 1: 폭탄이 날아가 범위가 차면 적이 그냥 사라진다
            Before,
            // 2: 연출을 모두 켠 지금 게임
            After,
            // 3: 1번에 적 반응(밀려남, 부풀었다 쪼그라듦)만 더한 것
            EnemyReactionOnly,
            // 4: 폭탄과 적 없이 처치 고리(흰 원)만
            KillRingOnly,
            // 5: 폭탄과 적 없이 픽셀 폭발 섬광만
            PixelFlashOnly,
            // 6: 폭탄과 적 없이 폭발 파티클(사각형 조각)만
            BombParticleOnly,
            // 7: 적 몇 마리만 두고 1번에 히트스톱과 연쇄 처치 숫자만 더한 것
            HitStopOnly
        }

        [Header("장면")]
        [Tooltip("시작할 때 보여 줄 장면. 플레이 중에는 숫자 키 1~7로 바꾼다 (1 전, 2 후, 3 적 반응만, 4 처치 고리만, 5 픽셀 섬광만, 6 폭발 파티클만, 7 히트스톱과 연쇄 숫자만)")]
        [SerializeField] private Shot _shot;

        [Header("폭탄")]
        [Tooltip("던질 때 쓰는 무기 (비행 시간, 포물선 높이, 폭발 반경, 터지기까지 시간)")]
        [SerializeField] private WeaponData _weapon;
        [Tooltip("두근거림과 폭발 섬광을 끈 폭탄 (씬에 꺼 둔 것). 전, 적 반응만 장면에서 던진다")]
        [SerializeField] private Bomb _plainBomb;
        [Tooltip("게임 그대로인 폭탄 (씬에 꺼 둔 것). 후 장면에서 던진다")]
        [SerializeField] private Bomb _fullBomb;
        [Tooltip("폭탄이 나가는 자리 (고블린 손)")]
        [SerializeField] private Transform _throwPoint;

        [Header("적")]
        [Tooltip("죽으면 바로 사라지는 적 (사망 시간 0인 시연용 데이터). 전 장면에 나온다")]
        [SerializeField] private Enemy[] _plainEnemies;
        [Tooltip("게임 그대로인 적. 후, 적 반응만 장면에 나온다")]
        [SerializeField] private Enemy[] _reactingEnemies;
        [Tooltip("적이 걸어가는 목표 (고블린 자리의 허수아비). 적은 죽을 때 이 목표 반대쪽으로 밀려난다")]
        [SerializeField] private Transform _enemyTarget;
        [Tooltip("적 무리 가운데가 목표에 이 거리만큼 다가오면 폭탄을 던진다 (유닛)")]
        [SerializeField, Min(0f)] private float _throwDistance = 10f;
        [Tooltip("적이 걷기 시작하고 속도를 재기까지 기다리는 시간 (초). 이 시간 동안은 던지지 않는다")]
        [SerializeField, Min(0.1f)] private float _measureTime = 0.4f;
        [Tooltip("폭발이 끝나고 적을 처음 자리로 되돌리기까지 쉬는 시간 (초)")]
        [SerializeField, Min(0f)] private float _restAfterBlast = 1.5f;

        [Header("연출 오브젝트 (후 장면에서만 켠다)")]
        [Tooltip("타격 피드백 (GameFeedback)")]
        [SerializeField] private GameObject _gameFeedback;
        [Tooltip("폭탄 픽셀 이펙트 (BombPixelFx). 픽셀 섬광만 장면에서도 켠다")]
        [SerializeField] private GameObject _bombPixelFx;
        [Tooltip("연쇄 처치 숫자 (MultiKillPopup)")]
        [SerializeField] private GameObject _multiKillPopup;
        [Tooltip("폭발음 (GameSfx)")]
        [SerializeField] private GameObject _gameSfx;

        [Header("히트스톱만 장면")]
        [Tooltip("이 장면에서만 켜는 타격 피드백 (GameFeedback). 게임은 5마리부터 히트스톱이 걸리므로, 적은 수로도 걸리게 기준을 낮춘 사본을 넣는다")]
        [SerializeField] private GameObject _hitStopFeedback;
        [Tooltip("이 장면에 내보낼 적 수 (마리). 바로 사라지는 적 목록의 앞에서부터 쓴다")]
        [SerializeField, Min(1)] private int _hitStopEnemyCount = 3;

        [Header("단독 재생 (처치 고리만, 픽셀 섬광만, 폭발 파티클만)")]
        [Tooltip("이펙트를 틀 자리")]
        [SerializeField] private Transform _soloPoint;
        [Tooltip("폭발 파티클 (Fb_BombExplode)")]
        [SerializeField] private FeedbackData _bombParticle;
        [Tooltip("처치 고리 (Fb_EnemyKilled)")]
        [SerializeField] private FeedbackData _killRing;
        [Tooltip("처치 고리를 몇 개 틀지 (개). 1개면 자리 한가운데에, 여러 개면 둘레에 고르게 놓는다")]
        [SerializeField, Min(0)] private int _killRingCount = 5;
        [Tooltip("처치 고리가 여러 개일 때 가운데에서 얼마나 떨어뜨려 놓을지 (유닛)")]
        [SerializeField, Min(0f)] private float _killRingSpread = 1.1f;
        [Tooltip("픽셀 섬광 크기를 정하는 폭발 반경 (유닛). 기본 무기는 1.7")]
        [SerializeField, Min(0.1f)] private float _soloRadius = 1.7f;
        [Tooltip("단독 재생 간격 (초)")]
        [SerializeField, Min(0.3f)] private float _soloInterval = 1.6f;

        private WeaponStats _stats;
        private Vector3[] _plainStarts;
        private Vector3[] _reactingStarts;
        private bool[] _plainReady;
        private bool[] _reactingReady;
        private Action<Bomb> _onBombFinished;

        private float _cycleTime;
        private Vector2 _measureStart;
        private bool _hasMeasureStart;
        private bool _isBombBusy;
        private bool _isResting;
        private float _restTimer;
        private float _soloTimer;

        private void Start()
        {
            _stats = new WeaponStats(_weapon);
            _onBombFinished = OnBombFinished;
            _plainStarts = ReadStarts(_plainEnemies);
            _reactingStarts = ReadStarts(_reactingEnemies);
            _plainReady = new bool[_plainEnemies.Length];
            _reactingReady = new bool[_reactingEnemies.Length];
            // 히트스톱은 판이 진행 중일 때만 걸린다. 이 씬에는 게임 상태 관리자가 없어 진행 중이라고 직접 알린다.
            GameEvents.RaiseGameStateChanged(GameState.None, GameState.Playing);
            Apply(_shot);
        }

        private void OnDisable()
        {
            FeedbackPlayer.ShakeEnabled = true;
            FeedbackPlayer.HitStopEnabled = true;
            FeedbackPlayer.EffectsEnabled = true;
        }

        private void Update()
        {
            ReadKeys();
            if (UsesEnemies(_shot))
            {
                TickThrow();
            }
            else
            {
                TickSolo();
            }
        }

        private void ReadKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame) Apply(Shot.Before);
            else if (keyboard.digit2Key.wasPressedThisFrame) Apply(Shot.After);
            else if (keyboard.digit3Key.wasPressedThisFrame) Apply(Shot.EnemyReactionOnly);
            else if (keyboard.digit4Key.wasPressedThisFrame) Apply(Shot.KillRingOnly);
            else if (keyboard.digit5Key.wasPressedThisFrame) Apply(Shot.PixelFlashOnly);
            else if (keyboard.digit6Key.wasPressedThisFrame) Apply(Shot.BombParticleOnly);
            else if (keyboard.digit7Key.wasPressedThisFrame) Apply(Shot.HitStopOnly);
        }

        private static bool UsesEnemies(Shot shot)
        {
            return shot == Shot.Before || shot == Shot.After || shot == Shot.EnemyReactionOnly || shot == Shot.HitStopOnly;
        }

        private void Apply(Shot shot)
        {
            _shot = shot;
            bool isAfter = shot == Shot.After;
            bool isHitStopOnly = shot == Shot.HitStopOnly;

            FeedbackPlayer.ShakeEnabled = isAfter;
            FeedbackPlayer.HitStopEnabled = isAfter || isHitStopOnly;
            FeedbackPlayer.EffectsEnabled = isAfter || shot == Shot.KillRingOnly || shot == Shot.BombParticleOnly;
            SetActive(_gameFeedback, isAfter);
            SetActive(_hitStopFeedback, isHitStopOnly);
            SetActive(_multiKillPopup, isAfter || isHitStopOnly);
            SetActive(_gameSfx, isAfter);
            SetActive(_bombPixelFx, isAfter || shot == Shot.PixelFlashOnly);

            _plainBomb.gameObject.SetActive(false);
            _fullBomb.gameObject.SetActive(false);
            _isBombBusy = false;
            _isResting = false;
            _soloTimer = 0.5f;

            SetGroup(_plainEnemies, false);
            SetGroup(_reactingEnemies, false);
            if (UsesEnemies(shot))
            {
                StartCycle();
            }
        }

        private static void SetActive(GameObject go, bool isActive)
        {
            if (go != null)
            {
                go.SetActive(isActive);
            }
        }

        private static Vector3[] ReadStarts(Enemy[] enemies)
        {
            var starts = new Vector3[enemies.Length];
            for (int i = 0; i < enemies.Length; i++)
            {
                starts[i] = enemies[i].transform.position;
            }

            return starts;
        }

        private static void SetGroup(Enemy[] enemies, bool isActive)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].gameObject.SetActive(isActive);
            }
        }

        private bool UsesPlainEnemies => _shot == Shot.Before || _shot == Shot.HitStopOnly;

        private Enemy[] ActiveEnemies => UsesPlainEnemies ? _plainEnemies : _reactingEnemies;

        // 이번 장면에 내보내는 적 수. 히트스톱만 장면은 앞의 몇 마리만 쓴다.
        private int ActiveCount => _shot == Shot.HitStopOnly ? Mathf.Min(_hitStopEnemyCount, _plainEnemies.Length) : ActiveEnemies.Length;

        // 적을 처음 자리로 되돌리고 다시 걷게 한다. 껐다 켜면 적이 이동 상태부터 새로 시작한다.
        private void StartCycle()
        {
            bool isPlain = UsesPlainEnemies;
            Enemy[] enemies = isPlain ? _plainEnemies : _reactingEnemies;
            Vector3[] starts = isPlain ? _plainStarts : _reactingStarts;
            bool[] ready = isPlain ? _plainReady : _reactingReady;
            int count = ActiveCount;
            for (int i = 0; i < count; i++)
            {
                GameObject go = enemies[i].gameObject;
                go.SetActive(false);
                go.transform.position = starts[i];
                go.GetComponent<Rigidbody2D>().position = starts[i];
                go.SetActive(true);
                if (!ready[i])
                {
                    // 사망 콜백은 쓰지 않는다. 풀이 없으니 적은 사망 연출이 끝나면 스스로 꺼진다.
                    enemies[i].Initialize(_enemyTarget, null, null);
                    ready[i] = true;
                }

                enemies[i].SkipArenaEntryDelay();
            }

            _cycleTime = 0f;
            _hasMeasureStart = false;
            _isResting = false;
        }

        private void TickThrow()
        {
            if (_isResting)
            {
                _restTimer -= Time.deltaTime;
                if (_restTimer <= 0f)
                {
                    StartCycle();
                }

                return;
            }

            if (_isBombBusy)
            {
                return;
            }

            _cycleTime += Time.deltaTime;
            Vector2 center = Centroid(ActiveEnemies, ActiveCount);
            if (!_hasMeasureStart)
            {
                _measureStart = center;
                _hasMeasureStart = true;
                return;
            }

            if (_cycleTime < _measureTime)
            {
                return;
            }

            if (((Vector2)_enemyTarget.position - center).sqrMagnitude > _throwDistance * _throwDistance)
            {
                return;
            }

            // 걷기 시작한 뒤의 평균 속도로, 폭탄이 터지는 순간 무리가 있을 자리를 노린다.
            Vector2 velocity = (center - _measureStart) / _cycleTime;
            Vector2 target = center + velocity * (_stats.FlightDuration + _stats.FuseDelay);
            Bomb bomb = _shot == Shot.After ? _fullBomb : _plainBomb;
            _isBombBusy = true;
            bomb.transform.position = _throwPoint.position;
            bomb.gameObject.SetActive(true);
            bomb.Launch(target, _stats, _onBombFinished);
        }

        private void OnBombFinished(Bomb bomb)
        {
            bomb.gameObject.SetActive(false);
            _isBombBusy = false;
            _isResting = true;
            _restTimer = _restAfterBlast;
        }

        private static Vector2 Centroid(Enemy[] enemies, int count)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                sum += (Vector2)enemies[i].transform.position;
            }

            return count > 0 ? sum / count : Vector2.zero;
        }

        private void TickSolo()
        {
            _soloTimer -= Time.deltaTime;
            if (_soloTimer > 0f)
            {
                return;
            }

            _soloTimer = _soloInterval;
            Vector2 point = _soloPoint.position;
            if (_shot == Shot.PixelFlashOnly)
            {
                // 픽셀 섬광은 폭발 알림을 듣고 튼다. 이 장면에서는 다른 연출 오브젝트가 꺼져 있어 섬광만 나온다.
                GameEvents.RaiseBombExploded(point, _soloRadius, 0);
                return;
            }

            if (_shot == Shot.BombParticleOnly)
            {
                FeedbackPlayer.Play(_bombParticle, point);
                return;
            }

            float spread = _killRingCount > 1 ? _killRingSpread : 0f;
            for (int i = 0; i < _killRingCount; i++)
            {
                float angle = Mathf.PI * 2f * i / _killRingCount;
                FeedbackPlayer.Play(_killRing, point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spread);
            }
        }
    }
}
