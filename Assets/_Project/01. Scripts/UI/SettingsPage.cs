using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 설정: 효과음·배경음 크기, 화면 흔들림, 번쩍임 줄이기, 자동 일시정지, 플레이 기록 보내기, 저장 초기화.
    // 슬라이더를 끄는 동안은 값만 바꾸고, 페이지를 닫을 때 한 번 저장한다(웹은 저장마다 디스크에 쓴다).
    public class SettingsPage : MonoBehaviour, IMenuPage
    {
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Toggle _shakeToggle;
        [SerializeField] private Toggle _reduceFlashToggle;
        [SerializeField] private Toggle _autoPauseToggle;
        // 처음 켤 때 동의 창에서 고른 답을 여기서 바꾼다.
        [SerializeField] private Toggle _analyticsToggle;
        [SerializeField] private ConfirmDialog _dialog;
        // 효과음 크기를 바꿀 때 바로 들려 줄 소리.
        [SerializeField] private SoundData _previewSound;
        [SerializeField, TextArea] private string _resetAskText = "저장을 초기화할까요?\n업적, 해금, 기록이 모두 사라져요.";
        [SerializeField, TextArea] private string _resetFinalText = "정말로 지울까요?\n되돌릴 수 없어요!";

        private void Awake()
        {
            _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            _shakeToggle.onValueChanged.AddListener(OnShakeChanged);
            _reduceFlashToggle.onValueChanged.AddListener(OnReduceFlashChanged);
            _autoPauseToggle.onValueChanged.AddListener(OnAutoPauseChanged);
            _analyticsToggle.onValueChanged.AddListener(OnAnalyticsChanged);
        }

        public void OnPageOpened()
        {
            Refresh();
        }

        public void OnPageClosed()
        {
            SaveSystem.Save();
        }

        // 초기화는 두 번 묻는다. 기록이 통째로 사라지기 때문이다.
        public void AskReset()
        {
            _dialog.Show(_resetAskText, () => _dialog.Show(_resetFinalText, ResetSave));
        }

        private void ResetSave()
        {
            SaveSystem.ResetAll();
            // 동의 답도 지워지므로 수집을 멈춘다. 다음에 타이틀을 열면 다시 묻는다.
            PlayAnalytics.Apply();
            AudioManager.SfxVolume = SaveSystem.Data.SfxVolume;
            AudioManager.BgmVolume = SaveSystem.Data.BgmVolume;
            Refresh();
        }

        private void Refresh()
        {
            SaveData data = SaveSystem.Data;
            _sfxSlider.SetValueWithoutNotify(data.SfxVolume);
            _bgmSlider.SetValueWithoutNotify(data.BgmVolume);
            _shakeToggle.SetIsOnWithoutNotify(data.ShakeEnabled);
            _reduceFlashToggle.SetIsOnWithoutNotify(data.ReduceFlashes);
            _autoPauseToggle.SetIsOnWithoutNotify(data.AutoPauseEnabled);
            _analyticsToggle.SetIsOnWithoutNotify(data.AnalyticsEnabled);
        }

        private void OnSfxChanged(float value)
        {
            AudioManager.SfxVolume = value;
            AudioManager.Play(_previewSound);
        }

        private void OnBgmChanged(float value)
        {
            AudioManager.BgmVolume = value;
        }

        private void OnShakeChanged(bool isOn)
        {
            SaveSystem.Data.ShakeEnabled = isOn;
        }

        private void OnReduceFlashChanged(bool isOn)
        {
            SaveSystem.Data.ReduceFlashes = isOn;
        }

        private void OnAutoPauseChanged(bool isOn)
        {
            SaveSystem.Data.AutoPauseEnabled = isOn;
        }

        private void OnAnalyticsChanged(bool isOn)
        {
            PlayAnalytics.SetConsent(isOn);
        }
    }
}
