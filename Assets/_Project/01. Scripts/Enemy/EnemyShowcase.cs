using System;
using TMPro;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 소개 영상 촬영용 씬(EnemyShowcase)에서 쓴다. 씬에 놓은 적마다 허수아비 목표를 하나씩 붙여, 다가가 예고하고 공격하는 동작을 계속 되풀이하게 한다.
    // 허수아비는 콜라이더가 없어 공격에 맞지 않고, 폭탄이 없으니 적도 죽지 않는다. 게임 씬에는 두지 않는다.
    public class EnemyShowcase : MonoBehaviour
    {
        [Serializable]
        private class Entry
        {
            [SerializeField] private Enemy _enemy;
            [SerializeField] private Transform _target;
            [SerializeField] private TMP_Text _label;

            public Enemy Enemy => _enemy;
            public Transform Target => _target;
            public TMP_Text Label => _label;
        }

        [SerializeField] private Entry[] _entries;

        private void Start()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                Entry entry = _entries[i];
                if (entry.Enemy == null || entry.Target == null)
                {
                    continue;
                }

                // 사망 콜백은 쓰지 않는다(죽을 일이 없다). 맵 범위가 없는 씬이라 진입 유예만 건너뛰어 바로 싸움에 들게 한다.
                entry.Enemy.Initialize(entry.Target, null, null);
                entry.Enemy.SkipArenaEntryDelay();
                if (entry.Label != null)
                {
                    entry.Label.text = entry.Enemy.EnemyData.KillerName;
                }
            }
        }
    }
}
