using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 설정: 효과음·배경음 크기, 화면 흔들림, 저장 초기화.
    // 슬라이더를 끄는 동안은 값만 바꾸고, 페이지를 닫을 때 한 번 저장한다(웹은 저장마다 디스크에 쓴다).
    public class SettingsPage : MonoBehaviour, IMenuPage
    {
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Toggle _shakeToggle;
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
    }
}
