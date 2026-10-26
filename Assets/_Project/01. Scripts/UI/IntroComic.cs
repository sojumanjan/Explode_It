using System;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 만화 페이지(오프닝·엔딩 공용). 2×2 칸이 차례로 튀어나오고, 칸 아래 상자에 대사가 타자 치듯 나온다.
    // 클릭해야만 다음 칸으로 넘어간다. 대사가 다 나오면 대사 상자 오른쪽 끝에 ▼가 통통 튀어 넘길 수 있음을 알리고,
    // 넘기는 순간 지금 칸이 꾹 눌렸다 돌아오며 딸깍 소리가 난다. 클릭 판정은 연출 감독(IntroSequence·OutroSequence)이 한다.
    // 대사는 그림에 그리지 않고 여기서 얹는다. 대사를 고칠 때 그림을 다시 만들지 않아도 된다.
    public class IntroComic : MonoBehaviour
    {
        // 칸이 튀어나올 때 처음 크기 배율과 기울기(도). 만화 칸을 탁 붙이는 느낌만 내는 연출 값이다.
        private const float PopStartScale = 1.25f;
        private const float PopStartTilt = 6f;
        // ▼ 표시가 위아래로 튀는 폭(UI 단위)과 빠르기, 넘길 때 지금 칸이 눌리는 세기(비율).
        private const float HintBob = 10f;
        private const float HintBobRate = 3f;
        private const float PressPunch = 0.06f;
        // ▼ 표시가 대사 상자 오른쪽 끝에서 안쪽으로 들어온 거리(UI 단위).
        private const float HintInset = 20f;

        [SerializeField] private CanvasGroup _group;
        // 칸 틀(테두리 포함). 튀어나오는 연출을 이 틀에 준다.
        [SerializeField] private RectTransform[] _panels;
        [SerializeField] private Image[] _pictures;
        [SerializeField] private TMP_Text[] _captions;
        // 대사가 다 나오면 지금 칸의 대사 상자 오른쪽 끝에 뜨는 ▼ 표시. 칸이 바뀔 때마다 그 칸의 상자로 옮겨 붙인다.
        [SerializeField] private RectTransform _nextHint;

        private IComicData _data;
        private Action _onDone;
        private int _index;
        private float _shown;
        private bool _isPlaying;
        private Tween _pop;
        private Tween _press;
        private Vector2 _hintHome;
        // 데이터의 몇 번째 컷부터, 몇 장을 이 페이지에 보여 줄지.
        private int _first;
        private int _count;
        // 씬에 놓은 2×2 칸 자리. 한 줄 페이지는 이 자리의 가로 위치와 전체 세로 가운데를 쓴다.
        private Vector2[] _panelHomes;

        // 씬에서는 꺼 둔다. 켜지는 순간 Awake가 돌기 때문에 Awake에서 끄면 Play로 켠 직후 다시 꺼진다.
        private void OnDisable()
        {
            _pop?.Kill();
            _press?.Kill();
        }

        // 기본은 모든 컷을 2×2로 보여 준다(오프닝). 엔딩은 컷을 두 장씩 끊어 가로 한 줄(세로 가운데)로 보여 준다.
        public void Play(IComicData data, Action onDone, int first = 0, int count = -1, bool isRow = false)
        {
            _data = data;
            _onDone = onDone;
            _first = first;
            _count = count < 0 ? data.PanelCount - first : count;
            gameObject.SetActive(true);
            _group.alpha = 1f;
            if (_panelHomes == null)
            {
                _panelHomes = new Vector2[_panels.Length];
                for (int i = 0; i < _panels.Length; i++)
                {
                    _panelHomes[i] = _panels[i].anchoredPosition;
                }
            }

            float middleY = 0f;
            for (int i = 0; i < _panelHomes.Length; i++)
            {
                middleY += _panelHomes[i].y / _panelHomes.Length;
            }

            for (int i = 0; i < _panels.Length; i++)
            {
                _panels[i].gameObject.SetActive(false);
                _panels[i].localScale = Vector3.one;
                _panels[i].anchoredPosition = isRow ? new Vector2(_panelHomes[i].x, middleY) : _panelHomes[i];
            }

            _nextHint.gameObject.SetActive(false);
            _isPlaying = true;
            _index = -1;
            ShowNext();
        }

        public void Hide()
        {
            _pop?.Kill();
            _press?.Kill();
            _isPlaying = false;
            gameObject.SetActive(false);
        }

        // 넘기기 입력. 마우스만 쓰면 키보드에 손을 둔 사람이 헤매므로 스페이스·엔터도 받는다.
        // 스페이스는 길게 누르면 건너뛰기이기도 해서, 누르는 순간 한 칸 넘어간 뒤 계속 누르면 건너뛴다.
        public static bool WasAdvancePressed()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            {
                return true;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame);
        }

        // 대사가 나오는 중이면 한 번에 다 보여 주고, 다 나왔으면 지금 칸을 꾹 누르는 연출과 함께 다음 칸으로 넘긴다.
        public void Advance()
        {
            if (!_isPlaying)
            {
                return;
            }

            TMP_Text caption = _captions[_index];
            if (caption.maxVisibleCharacters < caption.textInfo.characterCount)
            {
                _shown = caption.textInfo.characterCount;
                caption.maxVisibleCharacters = caption.textInfo.characterCount;
                return;
            }

            AudioManager.Play(_data.ClickSound);
            RectTransform panel = _panels[_index];
            _press?.Kill();
            panel.localScale = Vector3.one;
            _press = panel.DOPunchScale(Vector3.one * -PressPunch, 0.2f, 4).SetLink(gameObject);
            ShowNext();
        }

        private void Update()
        {
            if (!_isPlaying)
            {
                return;
            }

            TMP_Text caption = _captions[_index];
            int total = caption.textInfo.characterCount;
            if (caption.maxVisibleCharacters < total)
            {
                _shown += _data.CaptionCharsPerSecond * Time.deltaTime;
                caption.maxVisibleCharacters = Mathf.Min(total, (int)_shown);
                return;
            }

            if (!_nextHint.gameObject.activeSelf)
            {
                _nextHint.gameObject.SetActive(true);
            }

            _nextHint.anchoredPosition = _hintHome + Vector2.up * (Mathf.Abs(Mathf.Sin(Time.time * HintBobRate * Mathf.PI)) * HintBob);
        }

        private void ShowNext()
        {
            int count = Mathf.Min(Mathf.Min(_count, _data.PanelCount - _first), _panels.Length);
            if (_index + 1 >= count)
            {
                _isPlaying = false;
                _nextHint.gameObject.SetActive(false);
                _onDone?.Invoke();
                return;
            }

            _index++;
            _shown = 0f;
            _nextHint.gameObject.SetActive(false);

            Sprite sprite = _data.GetPanelSprite(_first + _index);
            if (sprite != null)
            {
                _pictures[_index].sprite = sprite;
                _pictures[_index].color = Color.white;
            }

            TMP_Text caption = _captions[_index];
            caption.text = _data.GetPanelLine(_first + _index);
            caption.ForceMeshUpdate();
            caption.maxVisibleCharacters = 0;
            AttachHint((RectTransform)caption.rectTransform.parent);

            RectTransform panel = _panels[_index];
            panel.gameObject.SetActive(true);
            panel.localScale = Vector3.one * PopStartScale;
            panel.localRotation = Quaternion.Euler(0f, 0f, _index % 2 == 0 ? PopStartTilt : -PopStartTilt);
            _pop?.Kill();
            _pop = DOTween.Sequence()
                .Append(panel.DOScale(1f, _data.PanelPopTime).SetEase(Ease.OutBack))
                .Join(panel.DOLocalRotate(Vector3.zero, _data.PanelPopTime).SetEase(Ease.OutQuad))
                .SetLink(gameObject);
            AudioManager.Play(_data.PanelSound);
        }

        // 대사 상자의 자식으로 붙여, 칸이 눌리고 튀는 연출도 같이 따라가게 한다.
        private void AttachHint(RectTransform box)
        {
            _nextHint.SetParent(box, false);
            _nextHint.anchorMin = new Vector2(1f, 0.5f);
            _nextHint.anchorMax = new Vector2(1f, 0.5f);
            _nextHint.pivot = new Vector2(1f, 0.5f);
            _hintHome = new Vector2(-HintInset, -HintBob * 0.5f);
            _nextHint.anchoredPosition = _hintHome;
        }
    }
}
