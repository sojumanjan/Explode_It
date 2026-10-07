using System;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 표정별 왕 그림. 맵 위 왕과 우측 하단 컷인이 같은 그림을 쓴다.
    [CreateAssetMenu(fileName = "KingSprites", menuName = "Explode It/Story/King Sprite Set")]
    public class KingSpriteSet : ScriptableObject
    {
        [Serializable]
        private class Entry
        {
            [Tooltip("표정")]
            [SerializeField] private KingMood _mood;

            [Tooltip("그 표정의 왕 그림")]
            [SerializeField] private Sprite _sprite;

            public KingMood Mood => _mood;
            public Sprite Sprite => _sprite;
        }

        [Tooltip("표정마다 그림을 하나씩 연결한다. 없는 표정은 첫 칸 그림을 쓴다")]
        [SerializeField] private Entry[] _entries;

        public Sprite Get(KingMood mood)
        {
            if (_entries == null || _entries.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Mood == mood && _entries[i].Sprite != null)
                {
                    return _entries[i].Sprite;
                }
            }

            return _entries[0].Sprite;
        }
    }
}
