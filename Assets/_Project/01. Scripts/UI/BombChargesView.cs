using System.Collections.Generic;
using ExplodeIt.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 최대 보유 수만큼 점을 두고, 남은 수만큼 채운다.
    public class BombChargesView : MonoBehaviour
    {
        [SerializeField] private Image _pipPrefab;
        // 점을 나란히 놓을 부모. Horizontal Layout Group이 붙어 있어야 한다.
        [SerializeField] private Transform _container;
        [SerializeField] private Color _filledColor = new Color(1f, 0.6f, 0.2f, 1f);
        // 바닥이 밝은 색이라 빈 점은 어둡게 둬야 보인다.
        [SerializeField] private Color _emptyColor = new Color(0f, 0f, 0f, 0.25f);

        private readonly List<Image> _pips = new List<Image>();

        private void OnEnable()
        {
            GameEvents.BombChargesChanged += OnChargesChanged;
        }

        private void OnDisable()
        {
            GameEvents.BombChargesChanged -= OnChargesChanged;
        }

        private void OnChargesChanged(int current, int max)
        {
            EnsurePipCount(max);
            for (int i = 0; i < _pips.Count; i++)
            {
                _pips[i].color = i < current ? _filledColor : _emptyColor;
            }
        }

        // 최대 수는 시작할 때와 개발자 패널·강화로 바뀔 때만 달라지므로, 그때만 점을 만든다.
        // 줄어들 때는 지우지 않고 꺼 두었다가 다시 늘면 켠다.
        private void EnsurePipCount(int count)
        {
            while (_pips.Count < count)
            {
                _pips.Add(Instantiate(_pipPrefab, _container));
            }

            for (int i = 0; i < _pips.Count; i++)
            {
                _pips[i].gameObject.SetActive(i < count);
            }
        }
    }
}
