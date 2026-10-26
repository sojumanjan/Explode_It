using System;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExplodeIt.Stage
{
    // 첫 판 오프닝 연출 감독. 만화 컷 → 정원(아레나 북쪽, 카메라 범위 밖) → 아레나 입장 순서로 진행하고, 끝나면 진행 흐름에 알린다.
    //   1. 만화 4컷: 마을에서 책을 펼쳤다가 책 속으로 빨려 들어간다.
    //   2. 정원: 낮잠 자던 2등신 왕 옆 스티커북에서 고블린이 튀어나오고(엔딩의 빨려 들어가기를 거꾸로), 왕이 놀라 병사를 부른다.
    //   3. 고블린이 아래로 도망쳐 화면 밖으로 사라지면, 까만 화면 뒤에서 아레나 위쪽 입구로 옮겨 걸어 내려오며 판이 시작된다.
    // 한 번 끝까지 보면(건너뛰어도) 기록해 두고 다음 판부터는 바로 시작한다.
    // 카메라를 정원과 아레나 사이에서 순간 이동시켜야 해서 카메라와 범위 제한을 직접 잡는다.
    public class IntroSequence : MonoBehaviour
    {
        // 병사 걸음의 통통 튀는 높이(유닛)와 빠르기(초당 걸음 수). 그림만 움직이는 연출 값이다.
        private const float SoldierBobHeight = 0.12f;
        private const float SoldierStepRate = 6f;
        // 말하는 쪽 머리 위와 말풍선 사이 간격(유닛), 표정이 바뀔 때 뽀잉 세기(비율).
        private const float BubbleGap = 0.15f;
        private const float MoodPunch = 0.25f;
        // 착지 순간 고블린이 납작해지는 비율과 원래 모양으로 튕겨 돌아오는 시간(초). 무게감을 주는 그림 연출 값이다.
        private const float LandSquash = 0.35f;
        private const float LandSquashTime = 0.06f;
        private const float LandRecoverTime = 0.35f;
        // 쿵 소리에 낮잠 자던 왕이 위로 움찔하는 세기(비율).
        private const float KingJolt = 0.2f;
        // 건너뛰기 칸이 손을 뗐을 때 줄어드는 빠르기(누를 때의 배수).
        private const float SkipDrainRate = 3f;
        // 건너뛰기 칸은 평소 흐리게 두어 컷씬을 가리지 않고, 누르기 시작하면 또렷해진다.
        private const float SkipIdleAlpha = 0.9f;

        private enum Step
        {
            None,
            Comic,
            Garden,
            Talk,
            Chase,
            Arena
        }

        [SerializeField] private IntroData _data;
        [SerializeField] private IntroComic _comic;

        [Header("정원")]
        // 정원을 비출 카메라 중심.
        [SerializeField] private Transform _gardenView;
        [SerializeField] private SpriteRenderer _king;
        [SerializeField] private KingSpriteSet _kingSprites;
        [SerializeField] private SpriteRenderer _book;
        // 고블린이 책에서 튀어나와 착지할 자리.
        [SerializeField] private Transform _popLanding;
        // 병사 그림. 부모 트랜스폼이 위치, 이 그림이 통통 튀는 몸이다. 처음 놓인 자리에서 아래로 걸어 들어온다.
        [SerializeField] private SpriteRenderer[] _soldiers;
        [SerializeField] private bool _soldierArtFacesRight;
        // 대사 말풍선. 보스 대화가 쓰는 씬 말풍선을 같이 쓴다(동시에 쓰는 일이 없다).
        [SerializeField] private SpeechBubble _kingBubble;
        [SerializeField] private SpeechBubble _playerBubble;

        [Header("플레이어")]
        [SerializeField] private Transform _player;
        [SerializeField] private Rigidbody2D _playerBody;
        [SerializeField] private SpriteRenderer _playerSprite;

        [Header("아레나")]
        // 아레나 위쪽 입구 안쪽 자리. 고블린이 여기서 나타나 아래로 걸어 내려온다.
        [SerializeField] private Transform _arenaEntry;

        [Header("카메라")]
        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private CinemachineConfiner2D _confiner;

        [Header("화면")]
        // 화면 전체를 덮는 이미지. 만화 → 정원은 하얗게, 정원 → 아레나는 까맣게 덮는다.
        [SerializeField] private Image _fade;
        [SerializeField] private RectTransform _skipButton;
        [SerializeField] private Image _skipFill;
        // 건너뛰기 칸 전체의 투명도. 비워 두면 늘 또렷하다.
        [SerializeField] private CanvasGroup _skipGroup;
        // 건너뛰려면 누르고 있어야 하는 시간(초). 대사를 넘기려고 연타하다 실수로 건너뛰지 않게 한다.
        [SerializeField, Min(0.1f)] private float _skipHoldTime = 0.8f;
        // 만화·정원 연출 동안 숨길 판 화면 UI(HUD 등). 까만 화면 뒤에서 아레나로 옮길 때 다시 켠다.
        [SerializeField] private GameObject[] _hideDuringIntro;

        private Step _step;
        private Action _onFinished;
        private Sequence _sequence;
        private Vector3 _playerScale = Vector3.one;
        private Vector3 _kingScale;
        private Vector3 _bookScale;
        private Vector3[] _soldierHomes;
        private Vector3[] _soldierBodyHomes;
        private bool _areSoldiersCalled;
        // 고블린이 책에서 튀어나왔는지, 몇 번째 줄 전에 튀어나오는지.
        private bool _hasPopped;
        private bool _isBgmPending;
        private int _popLine;
        private int _line;
        private float _stepTime;
        private float _skipHold;
        // 카메라 순간 이동을 몇 프레임 더 유지할지. 따라가는 대상이 한 프레임 늦게 옮겨 가도 미끄러지지 않게 한다.
        private int _cameraCutFrames;
        private bool _isCameraFocused;
        private Tween _punch;
        // 정원 배율을 걸기 전 카메라 화면 크기. 씬 창 기즈모가 플레이 중에도 같은 범위를 그리게 한다.
        private float _cameraBaseSize;

        public bool IsPlaying => _step != Step.None;
        public static bool HasSeen => SaveSystem.Data.IntroSeen;
        public bool ShouldPlay => _data != null && !HasSeen;

        public static void ClearSeen()
        {
            SaveSystem.Data.IntroSeen = false;
            SaveSystem.Save();
        }

        private void Awake()
        {
            _kingScale = _king.transform.localScale;
            _cameraBaseSize = _camera.Lens.OrthographicSize;
            _bookScale = _book.transform.localScale;
            _soldierHomes = new Vector3[_soldiers.Length];
            _soldierBodyHomes = new Vector3[_soldiers.Length];
            for (int i = 0; i < _soldiers.Length; i++)
            {
                _soldierHomes[i] = _soldiers[i].transform.parent.position;
                _soldierBodyHomes[i] = _soldiers[i].transform.localPosition;
                _soldiers[i].enabled = false;
            }
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _punch?.Kill();
        }

        // 씬 창에서 정원을 꾸밀 때 기준선: 정원 카메라에 실제로 보이는 범위(노랑), 고블린 착지 자리(초록),
        // 병사가 걸어 들어와 설 자리(빨강), 아레나 입구에서 걸어 내려오는 길(하늘).
        private void OnDrawGizmos()
        {
            if (_gardenView != null && _camera != null && _data != null)
            {
                // 플레이 중에는 렌즈 크기가 배율만큼 바뀌어 있으므로, 평소 크기를 기준으로 다시 계산한다.
                float baseSize = Application.isPlaying ? _cameraBaseSize : _camera.Lens.OrthographicSize;
                float halfHeight = baseSize * _data.GardenZoom;
                Camera view = Camera.main;
                float aspect = view != null ? view.aspect : 16f / 9f;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(_gardenView.position, new Vector3(halfHeight * aspect * 2f, halfHeight * 2f, 0f));
            }

            if (_popLanding != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_popLanding.position, 0.4f);
                if (_data != null)
                {
                    Gizmos.DrawLine(_popLanding.position, _popLanding.position + Vector3.down * _data.RunDistance);
                }
            }

            if (_soldiers != null && _data != null)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < _soldiers.Length; i++)
                {
                    if (_soldiers[i] == null)
                    {
                        continue;
                    }

                    Vector3 home = Application.isPlaying && _soldierHomes != null ? _soldierHomes[i] : _soldiers[i].transform.parent.position;
                    Vector3 stand = home + Vector3.down * _data.SoldierEnterDistance;
                    Gizmos.DrawLine(home, stand);
                    Gizmos.DrawWireSphere(stand, 0.35f);
                }
            }

            if (_arenaEntry != null && _data != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 goal = _arenaEntry.position + Vector3.down * _data.ArenaWalkDistance;
                Gizmos.DrawWireSphere(_arenaEntry.position, 0.4f);
                Gizmos.DrawLine(_arenaEntry.position, goal);
                Gizmos.DrawWireSphere(goal, 0.25f);
            }
        }

        public void Play(Action onFinished)
        {
            _onFinished = onFinished;
            GameEvents.RaisePlayerControlLockChanged(true);

            // 고블린은 아직 책 속에 있다.
            _playerScale = _player.localScale;
            _player.localScale = Vector3.zero;
            Teleport(_book.transform.position);
            // 튀어나와 착지하는 순간부터 왕을 본다. 대사가 뜰 때 바라보게 하면 그 전까지 커서 쪽을 보고 있다가 홱 돌아선다.
            GameEvents.RaisePlayerLookRequested(_king.transform.position);
            _king.sprite = _kingSprites.Get(KingMood.Lazy);
            _king.transform.localScale = _kingScale;
            _areSoldiersCalled = false;
            CutCamera(_gardenView.position, _data.GardenZoom, false);
            SetFade(Color.white, 0f);
            ShowSkip();
            SetIntroUiHidden(true);

            EnterStep(Step.Comic);
            _comic.Play(_data, OnComicDone);
            // 판 배경음은 다른 오브젝트의 Start에서 켜지므로, 그 뒤 첫 Update에서 오프닝 배경음으로 덮는다.
            _isBgmPending = true;
        }

        private void EnterStep(Step step)
        {
            _step = step;
            _stepTime = 0f;
        }

        private void Update()
        {
            if (_step == Step.None)
            {
                return;
            }

            if (_isBgmPending)
            {
                _isBgmPending = false;
                if (_data.Bgm != null)
                {
                    AudioManager.PlayBgm(_data.Bgm);
                }
            }

            _stepTime += Time.deltaTime;
            if (_cameraCutFrames > 0)
            {
                _cameraCutFrames--;
                _camera.PreviousStateIsValid = false;
            }

            if (_step == Step.Arena)
            {
                TickArena();
                return;
            }

            bool isOverSkip = IsPointerOverSkip();
            if (TickSkipHold(isOverSkip))
            {
                return;
            }

            bool isClicked = IntroComic.WasAdvancePressed() && !isOverSkip;
            switch (_step)
            {
                case Step.Comic:
                    if (isClicked)
                    {
                        _comic.Advance();
                    }
                    break;

                case Step.Talk:
                    TickSoldiers(false);
                    if (isClicked)
                    {
                        AdvanceLine();
                    }
                    break;

                case Step.Chase:
                    TickSoldiers(true);
                    TickChase();
                    break;
            }
        }

        // ── 만화 → 정원

        private void OnComicDone()
        {
            EnterStep(Step.Garden);
            float fade = _data.WhiteFadeTime;
            SetFade(Color.white, 0f);
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(_fade.DOFade(1f, fade))
                .AppendCallback(_comic.Hide)
                .Append(_fade.DOFade(0f, fade))
                .AppendInterval(_data.GardenStartWait)
                .AppendCallback(StartTalk)
                .SetLink(gameObject);
        }

        // 책이 들썩이며 번쩍이고 고블린이 튀어나온다. 그동안은 클릭을 받지 않고, 다 나오면 미뤄 둔 대사를 띄운다.
        private void PlayPop()
        {
            _hasPopped = true;
            EnterStep(Step.Garden);
            _kingBubble.Hide();
            _playerBubble.Hide();
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .AppendCallback(() => AudioManager.Play(_data.BookGlowSound))
                .Append(_book.transform.DOPunchScale(_bookScale * 0.15f, _data.BookGlowTime, 8))
                .Join(_book.DOColor(new Color(1f, 1f, 0.7f, 1f), _data.BookGlowTime * 0.25f).SetLoops(4, LoopType.Yoyo))
                .AppendCallback(() => AudioManager.Play(_data.PopSound))
                .Append(PopOutOfBook())
                .AppendCallback(PlayLandImpact)
                .Append(_book.transform.DOPunchScale(_bookScale * 0.2f, 0.3f, 6))
                .Join(LandSquashTween())
                .AppendInterval(_data.AfterPopWait)
                .AppendCallback(OnPopped)
                .SetLink(gameObject);
        }

        private void OnPopped()
        {
            EnterStep(Step.Talk);
            GameEvents.RaisePlayerLookRequested(_king.transform.position);
            ShowLine(_data.GetLine(_line));
        }

        // 표시한 줄이 없으면 첫 줄 전에 튀어나온다.
        private int PopLineIndex()
        {
            for (int i = 0; i < _data.LineCount; i++)
            {
                if (_data.GetLine(i).PopsGoblinBefore)
                {
                    return i;
                }
            }

            return 0;
        }

        // 엔딩에서 빨려 들어가던 모습을 거꾸로: 책 한가운데에서 빙글빙글 돌며 커져 포물선을 그리고 옆에 착지한다.
        private Tween PopOutOfBook()
        {
            Vector2 from = _book.transform.position;
            Vector2 to = _popLanding.position;
            return DOVirtual.Float(0f, 1f, _data.PopTime, t =>
            {
                Teleport(Vector2.Lerp(from, to, t) + Vector2.up * (4f * _data.PopArc * t * (1f - t)));
                _player.localScale = _playerScale * t;
                _player.localRotation = Quaternion.Euler(0f, 0f, 360f * _data.PopSpins * (1f - t));
            }).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                _player.localRotation = Quaternion.identity;
                _player.localScale = _playerScale;
                Teleport(to);
                AudioManager.Play(_data.LandSound);
            });
        }

        // 쿵: 화면이 흔들리고 흙먼지가 튀며, 왕이 소리에 움찔한다. 왕의 움찔은 표정 뽀잉과 같은 트윈 칸을 써서 다음 대사가 뜨면 깔끔하게 끊긴다.
        private void PlayLandImpact()
        {
            FeedbackPlayer.Play(_data.LandFeedback, _popLanding.position);
            _punch?.Kill();
            _king.transform.localScale = _kingScale;
            _punch = _king.transform.DOPunchScale(Vector3.Scale(_kingScale, new Vector3(-KingJolt * 0.5f, KingJolt, 0f)), 0.3f, 6).SetLink(gameObject);
        }

        // 납작하게 눌렸다가 탄력 있게 원래 모양으로 돌아온다. 좌우 뒤집힌 크기도 부호를 그대로 둔다.
        private Tween LandSquashTween()
        {
            Vector3 squashed = Vector3.Scale(_playerScale, new Vector3(1f + LandSquash, 1f - LandSquash, 1f));
            return DOTween.Sequence()
                .Append(_player.DOScale(squashed, LandSquashTime).SetEase(Ease.OutQuad))
                .Append(_player.DOScale(_playerScale, LandRecoverTime).SetEase(Ease.OutElastic));
        }

        // ── 정원 대사

        private void StartTalk()
        {
            EnterStep(Step.Talk);
            _hasPopped = false;
            _popLine = PopLineIndex();
            _line = -1;
            NextLine();
        }

        private void AdvanceLine()
        {
            SpeechBubble bubble = _data.GetLine(_line).Speaker == DialogueSpeaker.Player ? _playerBubble : _kingBubble;
            if (bubble.IsTyping)
            {
                bubble.CompleteTyping();
                return;
            }

            AudioManager.Play(_data.ClickSound);
            NextLine();
        }

        private void NextLine()
        {
            _line++;
            if (_line >= _data.LineCount)
            {
                StartChase();
                return;
            }

            if (!_hasPopped && _line >= _popLine)
            {
                PlayPop();
                return;
            }

            ShowLine(_data.GetLine(_line));
        }

        private void ShowLine(IntroData.Line line)
        {
            if (line.Speaker == DialogueSpeaker.Player)
            {
                _kingBubble.Hide();
                _playerBubble.transform.position = new Vector3(_player.position.x, _playerSprite.bounds.max.y + BubbleGap, 0f);
                _playerBubble.Show(line.Text);
            }
            else
            {
                // 표정이 바뀔 때마다 뽀잉 하고 튀어서 대사보다 표정이 먼저 읽히게 한다.
                _king.sprite = _kingSprites.Get(line.Mood);
                _punch?.Kill();
                _king.transform.localScale = _kingScale;
                _punch = _king.transform.DOPunchScale(_kingScale * MoodPunch, 0.25f, 6).SetLink(gameObject);
                _playerBubble.Hide();
                _kingBubble.transform.position = new Vector3(_king.transform.position.x, _king.bounds.max.y + BubbleGap, 0f);
                _kingBubble.Show(line.Text);
            }

            if (line.CallsSoldiers && !_areSoldiersCalled)
            {
                _areSoldiersCalled = true;
                for (int i = 0; i < _soldiers.Length; i++)
                {
                    _soldiers[i].enabled = true;
                }
            }
        }

        // 부른 뒤에는 처음 자리에서 정한 거리만큼 걸어 내려와 서고, 도망이 시작되면 고블린을 쫓는다.
        private void TickSoldiers(bool isChasing)
        {
            if (!_areSoldiersCalled)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            Vector2 target = _player.position;
            for (int i = 0; i < _soldiers.Length; i++)
            {
                Transform root = _soldiers[i].transform.parent;
                Vector2 position = root.position;
                Vector2 goal;
                float speed;
                if (isChasing)
                {
                    // 같은 점으로 몰려 겹치지 않게, 처음 늘어선 가로 간격을 유지하며 쫓는다.
                    goal = new Vector2(target.x + (_soldierHomes[i].x - _gardenView.position.x) * 0.5f, target.y);
                    speed = _data.SoldierChaseSpeed;
                }
                else
                {
                    goal = (Vector2)_soldierHomes[i] + Vector2.down * _data.SoldierEnterDistance;
                    speed = _data.SoldierEnterSpeed;
                }

                Vector2 next = Vector2.MoveTowards(position, goal, speed * deltaTime);
                bool isWalking = (next - position).sqrMagnitude > 0.000001f;
                root.position = next;

                float bob = isWalking ? Mathf.Abs(Mathf.Sin(_stepTime * SoldierStepRate * Mathf.PI)) * SoldierBobHeight : 0f;
                _soldiers[i].transform.localPosition = _soldierBodyHomes[i] + Vector3.up * bob;
                float faceX = target.x - next.x;
                if (Mathf.Abs(faceX) > 0.05f)
                {
                    _soldiers[i].flipX = (faceX > 0f) != _soldierArtFacesRight;
                }
            }
        }

        // ── 도망

        private void StartChase()
        {
            EnterStep(Step.Chase);
            _kingBubble.Hide();
            _playerBubble.Hide();
            GameEvents.RaisePlayerWalkRequested((Vector2)_player.position + Vector2.down * _data.RunDistance);
        }

        private void TickChase()
        {
            float bottom = _gardenView.position.y - _camera.Lens.OrthographicSize;
            if (_playerSprite.bounds.max.y < bottom || _stepTime >= _data.ChaseTimeout)
            {
                EnterArena(_data.BlackFadeTime);
            }
        }

        // ── 아레나 입장: 까맣게 덮고, 그 뒤에서 고블린과 카메라를 아레나로 옮긴 뒤 걷어 내고 걸어 내려오게 한다.

        private void EnterArena(float fadeIn)
        {
            EnterStep(Step.Arena);
            HideSkip();
            _sequence?.Kill();
            _punch?.Kill();
            _comic.Hide();
            _kingBubble.Hide();
            _playerBubble.Hide();
            GameEvents.RaisePlayerWalkCancelled();
            // 맵으로 넘어가는 순간 곡도 함께 바꾼다. 화면이 덮였다 걷히는 동안 넘어가 있어야, 걸어 나온 뒤에 곡이 바뀌어 어색하지 않다.
            if (_data.ArenaBgm != null)
            {
                AudioManager.PlayBgm(_data.ArenaBgm, fadeIn + _data.BlackHoldTime);
            }

            SetFade(Color.black, _fade.color.a);
            _sequence = DOTween.Sequence()
                .Append(_fade.DOFade(1f, fadeIn))
                .AppendCallback(PlaceInArena)
                .AppendInterval(_data.BlackHoldTime)
                .AppendCallback(WalkIntoArena)
                .Append(_fade.DOFade(0f, _data.BlackFadeTime))
                .SetLink(gameObject);
        }

        private void PlaceInArena()
        {
            _player.localScale = _playerScale;
            _player.localRotation = Quaternion.identity;
            Teleport(_arenaEntry.position);
            for (int i = 0; i < _soldiers.Length; i++)
            {
                _soldiers[i].enabled = false;
                _soldiers[i].transform.parent.position = _soldierHomes[i];
            }

            _king.sprite = _kingSprites.Get(KingMood.Lazy);
            CutCamera(_arenaEntry.position, 1f, true);
            SetIntroUiHidden(false);
        }

        private void SetIntroUiHidden(bool isHidden)
        {
            for (int i = 0; i < _hideDuringIntro.Length; i++)
            {
                _hideDuringIntro[i].SetActive(!isHidden);
            }
        }

        private void WalkIntoArena()
        {
            _stepTime = 0f;
            GameEvents.RaisePlayerWalkRequested((Vector2)_arenaEntry.position + Vector2.down * _data.ArenaWalkDistance);
        }

        private void TickArena()
        {
            if (_sequence != null && _sequence.IsActive() && _sequence.IsPlaying())
            {
                return;
            }

            Vector2 goal = (Vector2)_arenaEntry.position + Vector2.down * _data.ArenaWalkDistance;
            if (((Vector2)_player.position - goal).magnitude <= 0.3f || _stepTime >= _data.ArenaWalkTimeout)
            {
                Finish();
            }
        }

        private void Finish()
        {
            EnterStep(Step.None);
            GameEvents.RaisePlayerWalkCancelled();
            SaveSystem.Data.IntroSeen = true;
            SaveSystem.Save();
            GameEvents.RaisePlayerControlLockChanged(false);
            _onFinished?.Invoke();
        }

        // ── 카메라: 정원은 카메라 범위 밖이라 범위 제한을 끄고, 아레나로 돌아오면 다시 켠다. 둘 다 미끄러지지 않고 바로 옮겨 간다.

        private void CutCamera(Vector2 point, float zoom, bool followsPlayer)
        {
            _confiner.enabled = followsPlayer;
            if (followsPlayer)
            {
                if (_isCameraFocused)
                {
                    GameEvents.RaiseCameraFocusReleased(0f);
                }
            }
            else
            {
                GameEvents.RaiseCameraFocusRequested(point, 0f);
            }

            _isCameraFocused = !followsPlayer;
            GameEvents.RaiseCameraZoomRequested(zoom, 0f);
            _camera.PreviousStateIsValid = false;
            _cameraCutFrames = 2;
        }

        // 물리 몸까지 함께 옮겨, 다음 물리 스텝에서 예전 자리로 되돌아가지 않게 한다.
        private void Teleport(Vector2 position)
        {
            _player.position = position;
            _playerBody.position = position;
            _playerBody.linearVelocity = Vector2.zero;
        }

        private void SetFade(Color color, float alpha)
        {
            color.a = alpha;
            _fade.color = color;
            _fade.gameObject.SetActive(true);
        }

        // ── 건너뛰기: 오른쪽 아래 작은 칸을 길게 누르거나 스페이스를 길게 누르면 바로 아레나 입장으로 간다.

        private void ShowSkip()
        {
            _skipHold = 0f;
            SetSkipFill(0f);
            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(true);
            }
        }

        private void HideSkip()
        {
            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(false);
            }
        }

        private void SetSkipFill(float amount)
        {
            if (_skipFill != null)
            {
                _skipFill.fillAmount = amount;
            }

            if (_skipGroup != null)
            {
                _skipGroup.alpha = amount > 0f ? 1f : SkipIdleAlpha;
            }
        }

        private bool IsPointerOverSkip()
        {
            return _skipButton != null && _skipButton.gameObject.activeSelf && Mouse.current != null
                && RectTransformUtility.RectangleContainsScreenPoint(_skipButton, Mouse.current.position.ReadValue(), null);
        }

        private bool TickSkipHold(bool isOverSkip)
        {
            bool isHolding = (isOverSkip && Mouse.current.leftButton.isPressed)
                || (Keyboard.current != null && Keyboard.current.spaceKey.isPressed);
            float deltaTime = Time.unscaledDeltaTime;
            _skipHold = isHolding ? _skipHold + deltaTime : Mathf.Max(0f, _skipHold - deltaTime * SkipDrainRate);
            SetSkipFill(_skipHold / _skipHoldTime);
            if (_skipHold < _skipHoldTime)
            {
                return false;
            }

            _skipHold = 0f;
            EnterArena(_data.BlackFadeTime * 0.5f);
            return true;
        }
    }
}
