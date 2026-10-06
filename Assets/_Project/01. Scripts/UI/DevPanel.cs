using System.Text;
using ExplodeIt.Bombs;
using ExplodeIt.Core;
using ExplodeIt.Player;
using ExplodeIt.Progression;
using ExplodeIt.Stage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExplodeIt.UI
{
    // 재미 검증용 개발자 패널. 플레이를 멈추지 않고 수치를 바꿔 손맛을 비교한다.
    // 개발 도구라서 예외적으로 각 시스템을 직접 참조한다. 게임 코드는 이 패널을 몰라야 한다.
    // 바꾼 값은 런타임 복사본에만 들어가고 재시작하면 SO 값으로 돌아간다.
    // IMGUI 기본 폰트에는 한글이 없어 웹 빌드에서 깨지므로 화면 라벨은 영어로 쓴다.
    public class DevPanel : MonoBehaviour
    {
        // IMGUI 좌표는 이 해상도 기준으로 그리고 화면 높이에 맞춰 키운다. 웹 고해상도에서 글씨가 너무 작아지지 않게 한다.
        private const float ReferenceHeight = 1080f;
        private const float PanelWidth = 340f;
        private const float Margin = 10f;

        [SerializeField] private InputActionReference _toggleAction;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private BombLauncher _launcher;
        [SerializeField] private StageRunner _stageRunner;
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private PlayerAbility _ability;
        [SerializeField] private HitReceiver _playerHitReceiver;

        private bool _isOpen;
        private float _fps;
        private float _scale = 1f;
        private Vector2 _scroll;

        private void Awake()
        {
            // 배포 빌드에서는 열리지 않게 한다. 에디터와 Development Build에서만 쓴다.
            if (!Debug.isDebugBuild)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _toggleAction.action.performed += OnToggle;
            _toggleAction.action.Enable();
        }

        private void OnDisable()
        {
            _toggleAction.action.performed -= OnToggle;
            _input.PointerBlocked = false;
        }

        // timeScale은 씬을 다시 불러와도 유지되므로, 재시작 시 느려진 채로 남지 않게 되돌린다.
        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            _isOpen = !_isOpen;
        }

        private void Update()
        {
            // 순간값은 숫자가 너무 튀어서 읽기 어려우므로 부드럽게 섞는다. timeScale과 무관하게 실제 프레임을 잰다.
            float current = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            _fps = Mathf.Lerp(_fps, current, 0.1f);

            _scale = Mathf.Max(1f, Screen.height / ReferenceHeight);
            _input.PointerBlocked = _isOpen && IsPointerOverPanel();
        }

        private bool IsPointerOverPanel()
        {
            // 입력 좌표는 아래가 0, IMGUI 좌표는 위가 0이다.
            Vector2 aim = _input.Aim;
            float x = aim.x / _scale;
            float y = (Screen.height - aim.y) / _scale;
            return x >= Margin && x <= Margin + PanelWidth && y >= Margin && y <= Screen.height / _scale - Margin;
        }

        // 패널이 열려 있을 때만 그리므로 라벨 문자열 할당은 측정 중이 아닐 때만 생긴다.
        private void OnGUI()
        {
            if (!_isOpen)
            {
                return;
            }

            GUI.matrix = Matrix4x4.Scale(new Vector3(_scale, _scale, 1f));
            float height = Screen.height / _scale - Margin * 2f;

            GUILayout.BeginArea(new Rect(Margin, Margin, PanelWidth, height), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);

            DrawStatus();
            DrawWeapon();
            DrawStage();
            DrawCheats();
            DrawCommon();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawStatus()
        {
            GUILayout.Label($"FPS {_fps:0}   Enemies {_spawner.ActiveCount}   Speed {Time.timeScale:0.0}x");
        }

        // 슬라이더 범위는 시험해 볼 만한 폭일 뿐 밸런스 값이 아니다. 실제 값은 SO에 있다.
        private void DrawWeapon()
        {
            Header("Bomb");
            WeaponStats stats = _launcher.Stats;

            GUI.changed = false;
            stats.FuseDelay = Slider("Fuse Delay (s)", stats.FuseDelay, 0.3f, 2f);
            stats.ExplosionRadius = Slider("Explosion Radius", stats.ExplosionRadius, 0.5f, 4f);
            stats.Charges = IntSlider("Charges", stats.Charges, 1, 10);
            stats.RechargeTime = Slider("Recharge Time (s)", stats.RechargeTime, 0f, 5f);
            stats.ThrowInterval = Slider("Throw Interval (s)", stats.ThrowInterval, 0.05f, 1f);
            stats.MaxThrowRange = Slider("Max Throw Range", stats.MaxThrowRange, 2f, 12f);

            if (GUI.changed)
            {
                _launcher.ApplyStats();
            }
        }

        private void DrawStage()
        {
            Header("Stage");
            int waveCount = _stageRunner.WaveCount;
            GUILayout.Label($"Wave #{_stageRunner.WaveNumber}  {_stageRunner.CurrentPhase} {_stageRunner.PhaseTime:0.0}s");
            _stageRunner.IsPaused = GUILayout.Toggle(_stageRunner.IsPaused, " Pause Spawn");

            // 웨이브가 많아져도 패널이 길어지지 않게 한 줄에 몇 개씩 끊는다.
            const int buttonsPerRow = 5;
            for (int i = 0; i < waveCount; i += buttonsPerRow)
            {
                GUILayout.BeginHorizontal();
                for (int w = i; w < Mathf.Min(i + buttonsPerRow, waveCount); w++)
                {
                    if (GUILayout.Button($"W{w + 1}"))
                    {
                        _stageRunner.SkipToWave(w);
                    }
                }
                GUILayout.EndHorizontal();
            }

            // 보스 종류(B1~B3) × 바퀴(L1~L3). 버튼 하나가 곧 보스 번호(100마리 단위 순번)다.
            int kinds = _stageRunner.BossKindCount;
            for (int lap = 0; lap < _stageRunner.BossLaps; lap++)
            {
                GUILayout.BeginHorizontal();
                for (int kind = 0; kind < kinds; kind++)
                {
                    if (GUILayout.Button($"B{kind + 1} L{lap + 1}"))
                    {
                        _stageRunner.SkipToBoss(lap * kinds + kind + 1);
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Final Boss"))
            {
                _stageRunner.SkipToBoss(kinds * _stageRunner.BossLaps + 1);
            }
        }

        private void DrawCheats()
        {
            Header("Cheats");
            _playerHitReceiver.CheatInvulnerable = GUILayout.Toggle(_playerHitReceiver.CheatInvulnerable, " Player Invulnerable");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fill Ability"))
            {
                _ability.FillCharge();
            }

            if (GUILayout.Button("Kill All"))
            {
                _spawner.KillAll();
            }
            GUILayout.EndHorizontal();

            // 연출은 있을 때와 없을 때를 번갈아 봐야 효과를 판단할 수 있다.
            GUILayout.BeginHorizontal();
            FeedbackPlayer.ShakeEnabled = GUILayout.Toggle(FeedbackPlayer.ShakeEnabled, " Shake");
            FeedbackPlayer.HitStopEnabled = GUILayout.Toggle(FeedbackPlayer.HitStopEnabled, " HitStop");
            FeedbackPlayer.EffectsEnabled = GUILayout.Toggle(FeedbackPlayer.EffectsEnabled, " Effects");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Time Scale");
            if (GUILayout.Button("0.5x"))
            {
                Time.timeScale = 0.5f;
            }

            if (GUILayout.Button("1x"))
            {
                Time.timeScale = 1f;
            }

            if (GUILayout.Button("2x"))
            {
                Time.timeScale = 2f;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawCommon()
        {
            Header("Common");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to SO"))
            {
                _launcher.ResetStats();
            }

            if (GUILayout.Button("Log Values"))
            {
                LogValues();
            }
            GUILayout.EndHorizontal();
        }

        // 마음에 드는 값을 찾으면 콘솔에서 보고 SO에 옮겨 적는다.
        private void LogValues()
        {
            WeaponStats stats = _launcher.Stats;
            var builder = new StringBuilder("[DevPanel] 현재 값\n");
            builder.Append($"WeaponData: 딜레이 {stats.FuseDelay:0.00}, 반경 {stats.ExplosionRadius:0.00}, 보유 {stats.Charges}, " +
                           $"쿨타임 {stats.RechargeTime:0.00}, 연사 {stats.ThrowInterval:0.00}, 사거리 {stats.MaxThrowRange:0.00}");
            Debug.Log(builder.ToString());
        }

        private static void Header(string title)
        {
            GUILayout.Space(8f);
            GUILayout.Label($"== {title} ==");
        }

        private static float Slider(string label, float value, float min, float max)
        {
            GUILayout.Label($"{label}: {value:0.00}");
            return GUILayout.HorizontalSlider(value, min, max);
        }

        private static int IntSlider(string label, int value, int min, int max)
        {
            GUILayout.Label($"{label}: {value}");
            return Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max));
        }
    }
}
