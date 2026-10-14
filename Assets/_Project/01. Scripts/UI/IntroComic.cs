using System;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 오프닝 만화 페이지. 2×2 칸이 차례로 튀어나오고, 칸 아래 상자에 대사가 타자 치듯 나온다.
    // 대사가 다 나오고 잠시 뒤 저절로, 또는 클릭하면 바로 다음 칸으로 넘어간다. 클릭 판정은 연출 감독(IntroSequence)이 한다.
    // 대사는 그림에 그리지 않고 여기서 얹는다. 대사를 고칠 때 그림을 다시 만들지 않아도 된다.
    public class IntroComic : MonoBehaviour
    {
        // 칸이 튀어나올 때 처음 크기 배율과 기울기(도). 만화 칸을 탁 붙이는 느낌만 내는 연출 값이다.
        private const float PopStartScale = 1.25f;
        private const float PopStartTilt = 6f;

        [SerializeField] private CanvasGroup _group;
        // 칸 틀(테두리 포함). 튀어나오는 연출을 이 틀에 준다.
        [SerializeField] private RectTransform[] _panels;
        [SerializeField] private Image[] _pictures;
        [SerializeField] private TMP_Text[] _captions;

        private IntroData _data;
        private Action _onDone;
        private int _index;
        private float _shown;
        private float _holdTime;
        private bool _isPlaying;
        private Tween _pop;

        // 씬에서는 꺼 둔다. 켜지는 순간 Awake가 돌기 때문에 Awake에서 끄면 Play로 켠 직후 다시 꺼진다.
        private void OnDisable()
        {
            _pop?.Kill();
        }

        public void Play(IntroData data, Action onDone)
        {
            _data = data;
            _onDone = onDone;
            gameObject.SetActive(true);
            _group.alpha = 1f;
            for (int i = 0; i < _panels.Length; i++)
            {
                _panels[i].gameObject.SetActive(false);
            }

            _isPlaying = true;
            _index = -1;
            ShowNext();
        }

        public void Hide()
        {
            _pop?.Kill();
            _isPlaying = false;
            gameObject.SetActive(false);
        }

        // 대사가 나오는 중이면 한 번에 다 보여 주고, 다 나왔으면 다음 칸으로 넘긴다.
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

            _holdTime += Time.deltaTime;
            if (_holdTime >= _data.PanelHoldTime)
            {
                ShowNext();
            }
        }

        private void ShowNext()
        {
            int count = Mathf.Min(_data.PanelCount, _panels.Length);
            if (_index + 1 >= count)
            {
                _isPlaying = false;
                _onDone?.Invoke();
                return;
            }

            _index++;
            _shown = 0f;
            _holdTime = 0f;

            Sprite sprite = _data.GetPanelSprite(_index);
            if (sprite != null)
            {
                _pictures[_index].sprite = sprite;
                _pictures[_index].color = Color.white;
            }

            TMP_Text caption = _captions[_index];
            caption.text = _data.GetPanelLine(_index);
            caption.ForceMeshUpdate();
            caption.maxVisibleCharacters = 0;

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
    }
}
