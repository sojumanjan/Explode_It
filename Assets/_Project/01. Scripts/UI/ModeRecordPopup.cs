using System;
using DG.Tweening;
using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 메인에서 모드를 누르면 뜨는 작은 창. 그 모드의 최고 기록을 보여 주고, 창의 시작 버튼을 눌러야 판이 시작된다.
    // 무한 모드는 아래에 지금 장착한 저주 아이콘을 늘어놓아, 들어가기 전에 무엇을 짊어지는지 한 번 더 보여 준다.
    // 줄은 세로 레이아웃 안에 있어 숨긴 줄만큼 자리가 줄어든다.
    // 씬에서는 꺼 둔다. Awake에서 끄면 Show로 켠 직후 다시 꺼지기 때문이다.
    public class ModeRecordPopup : MonoBehaviour
    {
        private const float PopTime = 0.2f;

        [SerializeField] private UnlockCatalog _catalog;
        [SerializeField] private RectTransform _box;
        [SerializeField] private RectTransform _content;
        [SerializeField] private TMP_Text _titleText;
        // 모드 버튼의 클릭 소리. 잠긴 무한 모드는 막힘 소리만 나야 해서 버튼이 아니라 창이 뜰 때 낸다.
        [SerializeField] private SoundData _openSound;

        [Header("기록 줄")]
        [SerializeField] private RecordRow _clearRow;
        [SerializeField] private RecordRow _killsRow;
        [SerializeField] private RecordRow _survivalRow;

        [Header("장착한 저주")]
        [SerializeField] private GameObject _curseRow;
        // 저주 칸 수만큼 미리 놓아 둔다. 남는 칸은 숨긴다.
        [SerializeField] private Image[] _curseIcons;

        [Header("글")]
        [SerializeField] private string _storyTitle = "스토리 모드";
        [SerializeField] private string _endlessTitle = "무한 모드";
        [SerializeField] private string _clearTimeLabel = "최단 클리어";
        [SerializeField] private string _killsLabel = "최다 처치";
        [SerializeField] private string _survivalLabel = "최장 생존";
        [Tooltip("처치 수 표시. {0} = 마리 수")]
        [SerializeField] private string _killsFormat = "{0}마리";
        [Tooltip("아직 기록이 없을 때 값 자리에 쓰는 글")]
        [SerializeField] private string _emptyValue = "-";

        private Action _onStart;

        public bool IsOpen { get; private set; }

        public void Show(RunMode mode, Action onStart)
        {
            _onStart = onStart;
            AudioManager.Play(_openSound);
            SaveData data = SaveSystem.Data;
            if (mode == RunMode.Story)
            {
                _titleText.text = _storyTitle;
                _clearRow.Set(_clearTimeLabel, FormatTime(data.BestStoryTime), data.StoryCleared);
                _killsRow.Set(_killsLabel, FormatKills(data.BestStoryKills), false);
                _survivalRow.Set(_survivalLabel, FormatTime(data.BestSurvival(true)), false);
                _curseRow.SetActive(false);
            }
            else
            {
                _titleText.text = _endlessTitle;
                _clearRow.Hide();
                _killsRow.Set(_killsLabel, FormatKills(data.BestKills(false)), false);
                _survivalRow.Set(_survivalLabel, FormatTime(data.BestSurvival(false)), false);
                ShowEquippedCurses();
            }

            gameObject.SetActive(true);
            IsOpen = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _box.DOKill();
            _box.localScale = Vector3.one * 0.8f;
            _box.DOScale(1f, PopTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        // 시작 버튼. 판 시작은 화면을 덮으며 넘어가므로 창은 닫지 않고 그대로 덮이게 둔다.
        public void StartRun()
        {
            Action onStart = _onStart;
            _onStart = null;
            onStart?.Invoke();
        }

        public void Close()
        {
            IsOpen = false;
            _onStart = null;
            _box.DOKill();
            gameObject.SetActive(false);
        }

        // 카탈로그 순서(저주 고르기 페이지 순서)대로 장착한 저주만 앞에서부터 채운다. 하나도 없으면 줄째 숨긴다.
        private void ShowEquippedCurses()
        {
            CurseData[] curses = _catalog.Curses;
            int shown = 0;
            for (int i = 0; i < curses.Length && shown < _curseIcons.Length; i++)
            {
                if (!curses[i].IsEquipped)
                {
                    continue;
                }

                _curseIcons[shown].sprite = curses[i].Icon;
                _curseIcons[shown].gameObject.SetActive(true);
                shown++;
            }

            for (int i = shown; i < _curseIcons.Length; i++)
            {
                _curseIcons[i].gameObject.SetActive(false);
            }

            _curseRow.SetActive(shown > 0);
        }

        private string FormatKills(int kills)
        {
            return kills > 0 ? string.Format(_killsFormat, kills) : _emptyValue;
        }

        private string FormatTime(float seconds)
        {
            if (seconds <= 0f)
            {
                return _emptyValue;
            }

            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
