using System;
using TMPro;
using UnityEngine;

namespace ExplodeIt.Enemies
{
    // 소개 영상 촬영용 씬(EnemyShowcase)에서 쓴다. 씬에 놓은 적마다 허수아비 목표를 하나씩 붙여, 걸어와 예고하고 공격하는 한 바퀴를 되풀이하게 한다.
    // 공격이 끝나 다시 걷기 시작하면 처음 자리로 되돌려, 매번 같은 장면(걸어옴 → 예고 → 공격)이 찍히게 한다.
    // 허수아비는 플레이어와 같은 몸통 판정을 달아 적이 몸 한가운데를 노리게 하되, 판정 층이 달라 맞지는 않는다. 폭탄이 없으니 적도 죽지 않는다.
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

        private Vector3[] _starts;
        private int[] _attackCounts;

        private void Start()
        {
            _starts = new Vector3[_entries.Length];
            _attackCounts = new int[_entries.Length];
            for (int i = 0; i < _entries.Length; i++)
            {
                Entry entry = _entries[i];
                if (entry.Enemy == null || entry.Target == null)
                {
                    continue;
                }

                _starts[i] = entry.Enemy.transform.position;
                // 사망 콜백은 쓰지 않는다(죽을 일이 없다). 맵 범위가 없는 씬이라 진입 유예만 건너뛰어 바로 싸움에 들게 한다.
                entry.Enemy.Initialize(entry.Target, null, null);
                entry.Enemy.SkipArenaEntryDelay();
                _attackCounts[i] = entry.Enemy.AttackCount;
                if (entry.Label != null)
                {
                    entry.Label.text = entry.Enemy.EnemyData.KillerName;
                }
            }
        }

        private void Update()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                Enemy enemy = _entries[i].Enemy;
                if (enemy == null || _entries[i].Target == null)
                {
                    continue;
                }

                if (enemy.AttackCount != _attackCounts[i] && enemy.CurrentState == EnemyState.Move)
                {
                    Restart(i);
                }
            }
        }

        // 껐다 켜면 적이 이동 상태부터 새로 시작한다(목표는 그대로 남는다). 리지드바디 위치도 함께 옮겨야 한 스텝 늦게 순간이동하지 않는다.
        private void Restart(int index)
        {
            Enemy enemy = _entries[index].Enemy;
            GameObject go = enemy.gameObject;
            go.SetActive(false);
            go.transform.position = _starts[index];
            go.GetComponent<Rigidbody2D>().position = _starts[index];
            go.SetActive(true);
            enemy.SkipArenaEntryDelay();
            _attackCounts[index] = enemy.AttackCount;
        }
    }
}
