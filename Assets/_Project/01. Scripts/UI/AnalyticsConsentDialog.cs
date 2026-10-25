using DG.Tweening;
using ExplodeIt.Core;
using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // 게임을 처음 켰을 때 타이틀 위에 한 번 뜨는 플레이 기록 수집 동의 창. 답은 저장되고, 설정에서 언제든 바꿀 수 있다.
    // 반드시 하나를 골라야 닫힌다(뒤에 깔린 타이틀은 창 바깥 어두운 판이 클릭을 막는다).
    // 씬에서는 꺼 둔다. Awake에서 끄면 Show로 켠 직후 다시 꺼지기 때문이다.
    public class AnalyticsConsentDialog : MonoBehaviour
    {
        private const float PopTime = 0.25f;

        [SerializeField] private RectTransform _box;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;

        [Header("글")]
        [SerializeField] private string _title = "플레이 기록 수집 안내";
        [SerializeField, TextArea(8, 16)] private string _body =
            "더 재미있는 게임을 만들기 위해 플레이 기록(판 결과, 고른 장비, 진행 상황)과 기본 기기 정보를 익명으로 수집하려고 합니다.\n\n" +
            "이름, 연락처 같은 개인정보는 수집하지 않고, 설정에서 언제든 끌 수 있습니다.";

        public bool IsOpen { get; private set; }

        // 아직 묻지 않았으면 띄운다. 타이틀이 열릴 때 부른다.
        public void ShowIfNeeded()
        {
            if (SaveSystem.Data.AnalyticsAsked)
            {
                return;
            }

            _titleText.text = _title;
            _bodyText.text = _body;
            gameObject.SetActive(true);
            IsOpen = true;
            _box.DOKill();
            _box.localScale = Vector3.one * 0.85f;
            _box.DOScale(1f, PopTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        public void Agree()
        {
            Answer(true);
        }

        public void Decline()
        {
            Answer(false);
        }

        private void Answer(bool isGranted)
        {
            PlayAnalytics.SetConsent(isGranted);
            IsOpen = false;
            _box.DOKill();
            gameObject.SetActive(false);
        }
    }
}
