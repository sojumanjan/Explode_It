// 순위표 패널의 한 줄: 순위, 닉네임, 처치 수, 장착한 저주 아이콘
using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 패널에 페이지 인원만큼 미리 놓아 두고 값만 바꿔 쓴다(페이지를 넘길 때마다 만들고 지우지 않는다).
    public class LeaderboardRowView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _rank;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _kills;
        // 저주 칸 수만큼 미리 놓아 둔다. 남는 칸은 숨긴다.
        [SerializeField] private Image[] _curseIcons;
        // 아이콘마다 붙은 설명 창. 마우스를 올리면 그 저주의 이름과 설명을 보여 준다(아이콘만으로는 어떤 저주인지 헷갈린다).
        [SerializeField] private HoverTooltip[] _curseTooltips;
        [SerializeField] private Color _normalColor = new Color(1f, 1f, 1f, 0f);
        // 내 줄은 바탕을 칠해 바로 찾게 한다.
        [SerializeField] private Color _mineColor = new Color(1f, 0.82f, 0.35f, 0.45f);

        public void Set(LeaderboardRow row, UnlockCatalog catalog)
        {
            gameObject.SetActive(true);
            _background.color = row.IsMine ? _mineColor : _normalColor;
            _rank.text = row.Rank.ToString();
            _name.text = row.Name;
            _kills.text = row.Kills.ToString();
            ShowCurses(row.CurseIds, catalog);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // 저장된 ID 순서가 아니라 카탈로그 순서(저주 고르기 페이지 순서)로 늘어놓아, 줄마다 같은 저주가 같은 자리에 온다.
        private void ShowCurses(string curseIds, UnlockCatalog catalog)
        {
            int shown = 0;
            if (!string.IsNullOrEmpty(curseIds) && catalog != null)
            {
                CurseData[] curses = catalog.Curses;
                string wrapped = "," + curseIds + ",";
                for (int i = 0; i < curses.Length && shown < _curseIcons.Length; i++)
                {
                    if (curses[i] == null || !wrapped.Contains("," + curses[i].Id + ","))
                    {
                        continue;
                    }

                    _curseIcons[shown].sprite = curses[i].Icon;
                    _curseIcons[shown].gameObject.SetActive(true);
                    if (shown < _curseTooltips.Length && _curseTooltips[shown] != null)
                    {
                        _curseTooltips[shown].Text = curses[i].DisplayName + "\n" + curses[i].Description;
                    }

                    shown++;
                }
            }

            for (int i = shown; i < _curseIcons.Length; i++)
            {
                _curseIcons[i].gameObject.SetActive(false);
            }
        }
    }
}
