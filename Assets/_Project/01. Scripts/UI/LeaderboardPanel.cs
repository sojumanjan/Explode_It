// 무한 모드 기록 창 왼쪽의 순위표 패널: 페이지 넘기기와 내 순위로 가기
using System.Collections.Generic;
using System.Threading.Tasks;
using ExplodeIt.Core;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 무한 모드 창이 열릴 때 첫 페이지를 불러온다. 한 페이지 인원은 LeaderboardData, 줄은 미리 놓아 둔 LeaderboardRowView를 쓴다.
    // 요청이 늦게 와도 그 사이 다른 페이지를 눌렀으면 버린다(마지막으로 누른 페이지만 그린다).
    public class LeaderboardPanel : MonoBehaviour
    {
        [SerializeField] private UnlockCatalog _catalog;
        [SerializeField] private TMP_Text _title;
        // 줄 위의 열 이름.
        [SerializeField] private TMP_Text _rankHeader;
        [SerializeField] private TMP_Text _nameHeader;
        [SerializeField] private TMP_Text _killsHeader;
        [SerializeField] private TMP_Text _cursesHeader;
        [SerializeField] private LeaderboardRowView[] _rows;
        // 불러오는 중, 실패, 빈 순위표 안내. 줄 자리 가운데에 겹쳐 보여 준다.
        [SerializeField] private TMP_Text _status;
        [SerializeField] private TMP_Text _pageText;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _myRankButton;
        [SerializeField] private TMP_Text _myRankLabel;

        [Header("문구 (한국어 원문, 번역 표의 키)")]
        [SerializeField] private string _titleText = "순위표";
        [SerializeField] private string _rankHeaderText = "순위";
        [SerializeField] private string _nameHeaderText = "닉네임";
        [SerializeField] private string _killsHeaderText = "처치 수";
        [SerializeField] private string _cursesHeaderText = "장착한 저주";
        [SerializeField] private string _loadingText = "불러오는 중...";
        [SerializeField] private string _failedText = "순위표를 불러오지 못했습니다";
        [SerializeField] private string _emptyText = "아직 기록이 없습니다";
        [SerializeField] private string _noRankText = "아직 내 기록이 없습니다";
        [SerializeField] private string _myRankText = "내 순위로";

        private int _page;
        private int _pageCount = 1;
        private int _requestId;
        private int _lastTotal;

        private void Awake()
        {
            _prevButton.onClick.AddListener(OnPrevClicked);
            _nextButton.onClick.AddListener(OnNextClicked);
            _myRankButton.onClick.AddListener(OnMyRankClicked);
        }

        // 무한 모드 창을 열 때 부른다.
        public void Open()
        {
            gameObject.SetActive(true);
            _title.text = L10n.Tr(_titleText);
            _rankHeader.text = L10n.Tr(_rankHeaderText);
            _nameHeader.text = L10n.Tr(_nameHeaderText);
            _killsHeader.text = L10n.Tr(_killsHeaderText);
            _cursesHeader.text = L10n.Tr(_cursesHeaderText);
            _myRankLabel.text = L10n.Tr(_myRankText);
            _myRankButton.gameObject.SetActive(LeaderboardService.HasName);
            _pageCount = 1;
            _ = Load(0);
        }

        private void OnPrevClicked()
        {
            _ = Load(_page - 1);
        }

        private void OnNextClicked()
        {
            _ = Load(_page + 1);
        }

        private async void OnMyRankClicked()
        {
            int id = ++_requestId;
            SetLoading();
            int rank = await LeaderboardService.GetMyRankAsync();
            if (this == null || id != _requestId)
            {
                return;
            }

            if (rank > 0)
            {
                await Load((rank - 1) / LeaderboardService.PageSize);
                return;
            }

            // 내 기록이 없으면 보던 페이지를 다시 그리고 안내만 덧붙인다.
            if (await Load(_page) && this != null)
            {
                _status.text = L10n.Tr(_noRankText);
            }
        }

        // 다 그렸으면 true. 그 사이 다른 요청이 들어와 버렸으면 false.
        private async Task<bool> Load(int page)
        {
            // 내 순위로 갈 때는 아직 모르는 뒤쪽 페이지일 수 있어 위쪽은 막지 않는다(넘치면 빈 페이지로 보인다).
            _page = Mathf.Max(0, page);
            int id = ++_requestId;
            SetLoading();
            IReadOnlyList<LeaderboardRow> rows = await LeaderboardService.GetPageAsync(_page, SetTotal);
            if (this == null || id != _requestId)
            {
                return false;
            }

            if (_lastTotal < 0)
            {
                ShowRows(null);
                _status.text = L10n.Tr(_failedText);
                UpdatePaging();
                return true;
            }

            _pageCount = Mathf.Max(1, Mathf.CeilToInt(_lastTotal / (float)LeaderboardService.PageSize));
            ShowRows(rows);
            _status.text = rows.Count == 0 ? L10n.Tr(_emptyText) : "";
            UpdatePaging();
            return true;
        }

        private void SetTotal(int total)
        {
            _lastTotal = total;
        }

        private void SetLoading()
        {
            ShowRows(null);
            _status.text = L10n.Tr(_loadingText);
            _prevButton.interactable = false;
            _nextButton.interactable = false;
        }

        private void ShowRows(IReadOnlyList<LeaderboardRow> rows)
        {
            int count = rows != null ? rows.Count : 0;
            for (int i = 0; i < _rows.Length; i++)
            {
                if (i < count)
                {
                    _rows[i].Set(rows[i], _catalog);
                }
                else
                {
                    _rows[i].Hide();
                }
            }
        }

        private void UpdatePaging()
        {
            _pageText.text = (_page + 1) + " / " + _pageCount;
            _prevButton.interactable = _page > 0;
            _nextButton.interactable = _page < _pageCount - 1;
        }
    }
}
