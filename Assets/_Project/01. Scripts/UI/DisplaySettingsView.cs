using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 설정의 화면 항목: 전체화면과 해상도. 설정 페이지에 같이 붙이고, 페이지가 켜질 때마다 지금 화면 상태로 맞춘다.
    // UI는 1920×1080 기준으로 화면 크기에 맞춰 늘고 줄어서, 16:9 해상도만 고르게 하면 배치가 흐트러지지 않는다.
    // 고른 해상도·전체화면은 PC 빌드에서 유니티가 스스로 기억해 다음 실행에도 그대로 연다. 그래서 따로 저장하지 않는다.
    // 웹은 해상도를 페이지의 게임 칸이 정하므로 해상도 줄을 숨기고 전체화면만 둔다.
    public class DisplaySettingsView : MonoBehaviour
    {
        private const float WideAspect = 16f / 9f;
        private const float AspectTolerance = 0.01f;

        [SerializeField] private Toggle _fullScreenToggle;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        // 웹에서 숨길 해상도 줄(이름 글자와 드롭다운).
        [SerializeField] private GameObject[] _resolutionRow;

        private readonly List<Vector2Int> _resolutions = new List<Vector2Int>();
        private readonly List<string> _options = new List<string>();

        private static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;

        private void Awake()
        {
            _fullScreenToggle.onValueChanged.AddListener(OnFullScreenChanged);
            _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (IsWeb)
            {
                for (int i = 0; i < _resolutionRow.Length; i++)
                {
                    _resolutionRow[i].SetActive(false);
                }
            }
        }

        private void OnEnable()
        {
            _fullScreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            if (!IsWeb)
            {
                BuildResolutions();
            }
        }

        // 모니터가 지원하는 해상도 중 16:9만 고른다. 주사율만 다른 같은 크기가 여러 번 나오므로 한 번씩만 넣는다.
        private void BuildResolutions()
        {
            _resolutions.Clear();
            Resolution[] all = Screen.resolutions;
            for (int i = 0; i < all.Length; i++)
            {
                if (Mathf.Abs((float)all[i].width / all[i].height - WideAspect) > AspectTolerance)
                {
                    continue;
                }

                var size = new Vector2Int(all[i].width, all[i].height);
                if (!_resolutions.Contains(size))
                {
                    _resolutions.Add(size);
                }
            }

            // 창 크기를 직접 바꾼 경우처럼 지금 크기가 목록에 없으면 넣어 둔다. 고른 칸이 비어 보이지 않게 하려는 것이다.
            var current = new Vector2Int(Screen.width, Screen.height);
            if (!_resolutions.Contains(current))
            {
                _resolutions.Add(current);
            }

            _resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            // 게임 글꼴에 곱하기 기호(×)가 없어 x로 쓴다.
            _options.Clear();
            for (int i = 0; i < _resolutions.Count; i++)
            {
                _options.Add(_resolutions[i].x + " x " + _resolutions[i].y);
            }

            _resolutionDropdown.ClearOptions();
            _resolutionDropdown.AddOptions(_options);
            _resolutionDropdown.SetValueWithoutNotify(_resolutions.IndexOf(current));
            _resolutionDropdown.RefreshShownValue();
        }

        private void OnFullScreenChanged(bool isOn)
        {
            Screen.fullScreen = isOn;
        }

        private void OnResolutionChanged(int index)
        {
            Vector2Int size = _resolutions[index];
            Screen.SetResolution(size.x, size.y, Screen.fullScreenMode);
        }
    }
}
