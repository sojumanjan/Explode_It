using System.Collections.Generic;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 판이 끝나면(사망·클리어) 결과 창 위에 "이번 판에 새로 해금한 것" 창을 겹쳐 띄운다. 판 도중 알림을 놓쳐도 잊지 않게 한다.
    // 이번 판에 해금한 게 없으면 뜨지 않는다. 확인을 누르면 닫히고 결과 창이 보인다.
    // 이 컴포넌트는 늘 켜 두고 이벤트를 들으며, 창(_window)만 켜고 끈다.
    // 줄은 판이 끝난 뒤에만 견본을 복제해 만든다(전투 중이 아니라 Instantiate를 써도 된다).
    public class UnlockSummaryPanel : MonoBehaviour
    {
        private const float PopTime = 0.25f;

        [Tooltip("결과 창이 먼저 뜨고 나서 겹쳐 뜨도록 기다리는 시간 (초, 실제 시간)")]
        [SerializeField, Min(0f)] private float _showDelay = 0.8f;
        [SerializeField] private GameObject _window;
        [SerializeField] private RectTransform _box;
        // 견본 줄: 자식으로 Icon(Image)과 Name(TMP_Text)을 둔다. 씬에서는 꺼 둔다.
        [SerializeField] private RectTransform _rowTemplate;
        [SerializeField] private Transform _list;
        [SerializeField] private SoundData _sound;
        [SerializeField] private string _weaponFormat = "무기 · {0}";
        [SerializeField] private string _artifactFormat = "아티팩트 · {0}";

        private readonly List<AchievementData> _completed = new List<AchievementData>();
        private float _showAt = -1f;
        private bool _hasShown;

        private void Awake()
        {
            _window.SetActive(false);
            _rowTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            ProgressionEvents.AchievementCompleted += OnAchievementCompleted;
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            ProgressionEvents.AchievementCompleted -= OnAchievementCompleted;
            GameEvents.GameStateChanged -= OnGameStateChanged;
            _box.DOKill();
        }

        private void OnAchievementCompleted(AchievementData achievement)
        {
            _completed.Add(achievement);
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            bool isOver = current == GameState.PlayerDead || current == GameState.StageClear;
            if (isOver && !_hasShown && _completed.Count > 0)
            {
                _showAt = Time.unscaledTime + _showDelay;
            }
        }

        private void Update()
        {
            if (_showAt >= 0f && Time.unscaledTime >= _showAt)
            {
                _showAt = -1f;
                Show();
            }
        }

        // 확인 버튼.
        public void Close()
        {
            _box.DOKill();
            _window.SetActive(false);
        }

        private void Show()
        {
            _hasShown = true;
            _window.SetActive(true);
            for (int i = 0; i < _completed.Count; i++)
            {
                AchievementData achievement = _completed[i];
                if (achievement.RewardWeapon != null)
                {
                    AddRow(achievement.RewardWeapon.Icon, string.Format(_weaponFormat, achievement.RewardWeapon.DisplayName));
                }

                if (achievement.RewardArtifact != null)
                {
                    AddRow(achievement.RewardArtifact.Icon, string.Format(_artifactFormat, achievement.RewardArtifact.DisplayName));
                }
            }

            AudioManager.Play(_sound);
            _box.DOKill();
            _box.localScale = Vector3.one * 0.8f;
            _box.DOScale(1f, PopTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        private void AddRow(Sprite icon, string text)
        {
            RectTransform row = Instantiate(_rowTemplate, _list);
            row.gameObject.SetActive(true);
            Image image = row.Find("Icon").GetComponent<Image>();
            image.sprite = icon;
            image.enabled = icon != null;
            row.Find("Name").GetComponent<TMP_Text>().text = text;
        }
    }
}
