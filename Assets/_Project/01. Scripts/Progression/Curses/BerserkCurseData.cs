using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "광폭": 낮은 확률로 붉게 달아오른 적이 정해진 속도로 달려든다. 발밑에 잔상이 남아 멀리서도 알아본다.
    // 속도는 적 종류와 상관없이 하나로 고정해, 빨간 적은 늘 같은 빠르기라는 걸 익힐 수 있게 한다.
    [CreateAssetMenu(fileName = "Curse_Berserk", menuName = "Explode It/Curses/Berserk")]
    public class BerserkCurseData : CurseData
    {
        [Header("광폭")]
        [Tooltip("적 하나가 나올 때 광폭이 될 확률 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float _chance = 0.1f;

        [Tooltip("광폭 적이 쫓아오는 속도 (유닛/초). 적 종류·웨이브 배율과 상관없이 이 값이다. 플레이어는 5")]
        [SerializeField, Min(0.1f)] private float _moveSpeed = 9f;

        [Tooltip("광폭이 될 수 있는 적. 비워 두면 보스를 뺀 모든 적")]
        [SerializeField] private EnemyData[] _targets = new EnemyData[0];

        [Header("연출")]
        [Tooltip("광폭 적 몸 색 (그림 색에 곱해진다)")]
        [SerializeField] private Color _tint = new Color(1f, 0.35f, 0.3f, 1f);

        [Tooltip("발밑 잔상 위치 (유닛, 적 위치 기준)")]
        [SerializeField] private Vector2 _trailOffset = new Vector2(0f, 0.05f);

        [Tooltip("광폭 적이 나올 때 소리")]
        [SerializeField] private SoundData _spawnSound;

        public override CurseKind Kind => CurseKind.Berserk;
        public float Chance => _chance;
        public float MoveSpeed => _moveSpeed;
        public Color Tint => _tint;
        public Vector2 TrailOffset => _trailOffset;
        public SoundData SpawnSound => _spawnSound;

        public bool CanAffect(EnemyData enemy)
        {
            if (_targets.Length == 0)
            {
                return true;
            }

            return System.Array.IndexOf(_targets, enemy) >= 0;
        }
    }
}
