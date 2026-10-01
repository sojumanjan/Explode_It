using System;
using ExplodeIt.Bombs;
using ExplodeIt.Core;
using ExplodeIt.Player;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 처치로 게이지를 채우고, 다 차면 블랙홀 폭탄을 던진다.
    public class PlayerAbility : MonoBehaviour
    {
        [SerializeField] private AbilityData _data;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private BombLauncher _launcher;
        [SerializeField] private BlackHoleBomb _blackHolePrefab;

        private BlackHoleBomb _blackHole;
        private Action _onBlackHoleFinished;
        private int _charge;
        private bool _isActive;
        private bool _canUse = true;

        private void Awake()
        {
            // 한 번에 하나만 존재하므로 풀 대신 하나를 미리 만들어 재사용한다. 전투 중 생성이 일어나지 않는다.
            // 던진 뒤 플레이어를 따라가면 안 되므로 부모 없이 월드에 둔다.
            _blackHole = Instantiate(_blackHolePrefab);
            _blackHole.gameObject.SetActive(false);
            _onBlackHoleFinished = OnBlackHoleFinished;

            // 첫 위기에 바로 쓸 수 있게 가득 찬 채로 시작한다. 능력을 처음부터 알게 되는 효과도 있다.
            _charge = _data.KillsToCharge;
        }

        private void OnEnable()
        {
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        // 게이지 UI가 OnEnable에서 구독을 마친 뒤 첫 값을 받도록 Start에서 알린다.
        private void Start()
        {
            GameEvents.RaiseAbilityChargeChanged(_charge, _data.KillsToCharge);
        }

        // 개발자 패널용. 처치 없이 바로 시험해 볼 수 있게 한다.
        public void FillCharge()
        {
            _charge = _data.KillsToCharge;
            GameEvents.RaiseAbilityChargeChanged(_charge, _data.KillsToCharge);
        }

        private void Update()
        {
            if (!_canUse || _isActive || _charge < _data.KillsToCharge || !_input.AbilityPressed)
            {
                return;
            }

            Use();
        }

        private void Use()
        {
            _charge = 0;
            _isActive = true;
            GameEvents.RaiseAbilityChargeChanged(_charge, _data.KillsToCharge);

            WeaponStats weapon = _launcher.Stats;
            _blackHole.transform.position = transform.position;
            _blackHole.gameObject.SetActive(true);
            _blackHole.Launch(_launcher.GetThrowTarget(_data.MaxThrowRange),weapon.ExplosionRadius * _data.RadiusMultiplier,
                _data.PullDuration, _data.PullSpeed, weapon, _onBlackHoleFinished);
        }

        // 블랙홀로 뭉친 적을 지우면 게이지가 곧바로 다시 차서 연달아 쓰게 되므로,
        // 블랙홀이 날아가 터질 때까지는 처치를 세지 않는다.
        private void OnEnemyKilled(Vector2 position)
        {
            if (_isActive || _charge >= _data.KillsToCharge)
            {
                return;
            }

            _charge++;
            GameEvents.RaiseAbilityChargeChanged(_charge, _data.KillsToCharge);
        }

        private void OnBlackHoleFinished()
        {
            _blackHole.gameObject.SetActive(false);
            _isActive = false;
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _canUse = current == GameState.Playing;
        }
    }
}
