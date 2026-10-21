using System;
using ExplodeIt.Core;
using ExplodeIt.Enemies;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "광폭": 낮은 확률로 붉게 달아오른 적이 정해진 속도로 달려든다. 발밑에 잔상이 남아 멀리서도 알아본다.
    // 속도는 적 종류와 상관없이 하나로 정해, 빨간 적은 늘 같은 빠르기라는 걸 익힐 수 있게 한다(웨이브가 쌓이면 다른 적과 함께 빨라진다).
    // 근접 적은 공격 박자도 빨라진다(예고가 짧고 쉬는 시간이 짧다). 원거리 적은 연사가 되면 억울하므로 이동속도만 빨라진다.
    [CreateAssetMenu(fileName = "Curse_Berserk", menuName = "Explode It/Curses/Berserk")]
    public class BerserkCurseData : CurseData
    {
        // 광폭일 때 공격 박자를 당길 적 하나.
        [Serializable]
        private class AttackTempo
        {
            [Tooltip("공격 박자를 당길 근접 적")]
            public EnemyData Enemy;
            [Tooltip("광폭일 때 예고 길이 (초). 단계가 있는 적(골렘)은 데이터의 단계 순서대로 적는다. 단계가 없으면 하나만")]
            public float[] TelegraphDurations = { 0.3f };
            [NonSerialized] public float[] Clamped;
        }

        [Header("광폭")]
        [Tooltip("적 하나가 나올 때 광폭이 될 확률 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float _chance = 0.1f;

        [Tooltip("광폭 적이 쫓아오는 기본 속도 (유닛/초). 적 종류와 상관없이 이 값에 무한 모드 웨이브 속도 배율이 곱해진다. 플레이어는 5")]
        [SerializeField, Min(0.1f)] private float _moveSpeed = 9f;

        [Tooltip("광폭이 될 수 있는 적. 비워 두면 보스를 뺀 모든 적")]
        [SerializeField] private EnemyData[] _targets = new EnemyData[0];

        [Header("공격 박자 (근접 적만)")]
        [Tooltip("공격 박자를 당길 근접 적과 광폭일 때 예고 길이. 여기 없는 적(원거리 등)은 이동속도만 빨라진다")]
        [SerializeField] private AttackTempo[] _attackTempos = new AttackTempo[0];

        [Tooltip("광폭 근접 적의 공격 뒤 쉬는 시간 (초)")]
        [SerializeField, Min(0.05f)] private float _recoverDuration = 0.5f;

        [Tooltip("예고 최소 길이 (초). 위에 더 짧게 적어도 이보다 짧아지지 않는다(모든 공격에 예고가 있어야 한다)")]
        [SerializeField, Min(0.05f)] private float _minTelegraph = 0.2f;

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

        public float RecoverDuration => _recoverDuration;

        // 이 적의 광폭 예고 길이(최소값 적용). 근접 목록에 없으면 null이라 공격 박자는 그대로다.
        public float[] GetTelegraphs(EnemyData enemy)
        {
            for (int i = 0; i < _attackTempos.Length; i++)
            {
                AttackTempo tempo = _attackTempos[i];
                if (tempo.Enemy != enemy || tempo.TelegraphDurations == null || tempo.TelegraphDurations.Length == 0)
                {
                    continue;
                }

                // 최소값을 적용한 배열은 처음 한 번만 만들어 적마다 같이 쓴다.
                if (tempo.Clamped == null)
                {
                    tempo.Clamped = new float[tempo.TelegraphDurations.Length];
                    for (int k = 0; k < tempo.Clamped.Length; k++)
                    {
                        tempo.Clamped[k] = Mathf.Max(_minTelegraph, tempo.TelegraphDurations[k]);
                    }
                }

                return tempo.Clamped;
            }

            return null;
        }

        // 인스펙터에서 값을 바꾸면 최소값을 적용한 배열을 다시 만든다.
        private void OnValidate()
        {
            for (int i = 0; i < _attackTempos.Length; i++)
            {
                if (_attackTempos[i] != null)
                {
                    _attackTempos[i].Clamped = null;
                }
            }
        }

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
