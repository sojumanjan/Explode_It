using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.Stage;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 게임 씬에서 저주 하나를 실제로 돌리는 컴포넌트의 공통 틀. 이번 판에 그 저주가 걸리지 않았으면 스스로 꺼진다.
    // 판이 진행 중이고 연출로 조작이 막히지 않았을 때만 새 공격을 낸다(보스 등장 대화 중에 운석이 떨어지면 억울하다).
    // 연출 동안에는 이미 깔아 둔 공격(예고 원·운석·덫·바람)까지 저주 시간 전체가 멈췄다가, 풀리는 순간 이어서 흐른다.
    public abstract class CurseBehaviour<TData> : MonoBehaviour where TData : CurseData
    {
        [SerializeField] private TData _data;
        [SerializeField] private Transform _player;

        private Collider2D _playerCollider;
        private bool _isPlaying;
        private bool _isLocked;
        private bool _hasStarted;

        protected TData Data => _data;
        protected bool IsRunning => _hasStarted && _isPlaying && !_isLocked;

        // 적이 노리는 점과 같게 플레이어 피격 판정 한가운데.
        protected Vector2 PlayerPosition => _playerCollider != null && _playerCollider.enabled
            ? (Vector2)_playerCollider.bounds.center : (Vector2)_player.position;

        // RunCurses는 장비 적용기(-200)가 Awake에서 채우므로 여기서는 이미 차 있다.
        protected virtual void Awake()
        {
            if (_data == null || !RunCurses.Has(_data.Kind))
            {
                enabled = false;
                return;
            }

            _playerCollider = _player.GetComponent<Collider2D>();
        }

        protected virtual void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.PlayerControlLockChanged += OnControlLockChanged;
            GameEvents.StageStarted += OnStageStarted;
        }

        protected virtual void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.PlayerControlLockChanged -= OnControlLockChanged;
            GameEvents.StageStarted -= OnStageStarted;
        }

        // 판이 시작되는 순간. 주기 공격은 여기서 첫 타이머를 잡는다.
        protected virtual void OnRunStarted()
        {
        }

        private void OnStageStarted(bool isStoryMode)
        {
            _hasStarted = true;
            OnRunStarted();
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _isPlaying = current == GameState.Playing;
        }

        // 연출로 조작이 막히면 저주 시간 전체를 멈춘다. 저주 컴포넌트가 여럿이라 여러 번 불려도 같은 값을 넣을 뿐이다.
        private void OnControlLockChanged(bool isLocked)
        {
            _isLocked = isLocked;
            RunCurses.IsFrozen = isLocked;
            if (isLocked)
            {
                DOTween.Pause(RunCurses.TweenId);
            }
            else
            {
                DOTween.Play(RunCurses.TweenId);
            }
        }

        // 맵 안이고 구조물과 겹치지 않는 자리인지. 맵에 무언가를 까는 저주가 자리를 고를 때 쓴다.
        protected static bool IsOpenSpot(Vector2 point, float radius, LayerMask obstacleMask)
        {
            ArenaBounds arena = ArenaBounds.Current;
            if (arena != null && !arena.Contains(point))
            {
                return false;
            }

            return Physics2D.OverlapCircle(point, radius, obstacleMask) == null;
        }

        // 기준점 둘레 minRadius~maxRadius 거리 안의 빈자리를 몇 번 찾아본다. 못 찾으면 false.
        protected static bool TryPickSpot(Vector2 center, float minRadius, float maxRadius, float clearRadius,
            LayerMask obstacleMask, out Vector2 spot)
        {
            for (int attempt = 0; attempt < SpotAttempts; attempt++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                spot = center + direction * Random.Range(minRadius, maxRadius);
                if (IsOpenSpot(spot, clearRadius, obstacleMask))
                {
                    return true;
                }
            }

            spot = center;
            return false;
        }

        // 기준점 둘레 minDistance~maxDistance 거리의 한 점. 구조물은 보지 않고 맵 안으로만 당긴다(하늘에서 떨어지는 공격용).
        protected static Vector2 PointAround(Vector2 center, float minDistance, float maxDistance)
        {
            Vector2 point = center + Random.insideUnitCircle.normalized * Random.Range(minDistance, maxDistance);
            ArenaBounds arena = ArenaBounds.Current;
            if (arena != null)
            {
                Bounds bounds = arena.Bounds;
                point.x = Mathf.Clamp(point.x, bounds.min.x, bounds.max.x);
                point.y = Mathf.Clamp(point.y, bounds.min.y, bounds.max.y);
            }

            return point;
        }

        // 빈자리를 찾는 횟수. 자리 고르기 안전장치라 데이터로 빼지 않는다.
        private const int SpotAttempts = 12;
    }
}
