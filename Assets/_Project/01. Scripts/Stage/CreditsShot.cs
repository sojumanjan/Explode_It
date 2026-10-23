using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 크레딧 왼쪽에 비추는 장면 하나의 장소와 배우. 맵 어딘가(정원 등)에 꺼 둔 채 놓아 두고, 크레딧이 비출 때만 켠다.
    // 무엇을 언제 하는지는 대본(CreditsShotData)에 있다. 끝나면 배우를 처음 모습으로 되돌리고 다시 끈다.
    public class CreditsShot : MonoBehaviour
    {
        // 같은 시각의 동작은 대본에 적은 순서대로 이어 붙인다.
        private static readonly Comparison<CreditsShotStep> ByTime = (a, b) => a.Time.CompareTo(b.Time);

        [SerializeField] private CreditsShotData _data;
        // 화면 왼쪽 칸 가운데에 올 자리.
        [SerializeField] private Transform _subject;
        [SerializeField] private SpriteRenderer[] _actors;
        // 같은 자리에 늘 놓여 있어 장면을 가리는 것(인트로의 누운 왕 등). 장면 동안만 숨긴다.
        [SerializeField] private GameObject[] _hideWhilePlaying;

        private readonly List<CreditsShotStep> _sorted = new List<CreditsShotStep>();
        private Vector3[] _homePositions;
        private Vector3[] _homeScales;
        private Sprite[] _homeSprites;
        private bool[] _homeFlips;
        private bool[] _hiddenWasActive;

        public CreditsShotData Data => _data;
        public Vector2 SubjectPoint => _subject.position;

        // 배우를 켜고 처음 모습을 기억한 뒤, 대본대로 움직이는 트윈을 만들어 돌려준다(아직 시작하지 않음).
        public Sequence Prepare()
        {
            gameObject.SetActive(true);
            Remember();
            SetHidden(true);

            _sorted.Clear();
            if (_data.Steps != null)
            {
                _sorted.AddRange(_data.Steps);
            }

            _sorted.Sort(ByTime);
            var offsets = new Vector3[_actors.Length];
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject).Pause();
            for (int i = 0; i < _sorted.Count; i++)
            {
                CreditsShotStep step = _sorted[i];
                if (step.Actor < 0 || step.Actor >= _actors.Length)
                {
                    continue;
                }

                SpriteRenderer actor = _actors[step.Actor];
                if (step.Sprite != null)
                {
                    Sprite sprite = step.Sprite;
                    sequence.InsertCallback(step.Time, () => actor.sprite = sprite);
                }

                // 콩콩·옮기기는 끝 자리를 미리 정해 두는 트윈이라, 앞서 옮겨 간 만큼을 더해 그 자리에서 움직이게 한다.
                Vector3 position = _homePositions[step.Actor] + offsets[step.Actor];
                Tween tween = BuildTween(actor.transform, step, position, _homeScales[step.Actor]);
                if (step.Action == CreditsShotAction.Move || step.Action == CreditsShotAction.Hop)
                {
                    offsets[step.Actor] += (Vector3)step.Move;
                }

                if (tween != null)
                {
                    sequence.Insert(step.Time, tween);
                }
                else if (step.Action == CreditsShotAction.Flip)
                {
                    sequence.InsertCallback(step.Time, () => actor.flipX = !actor.flipX);
                }
            }

            // 대본이 머무는 시간보다 짧아도 장면 길이는 머무는 시간에 맞춘다.
            sequence.InsertCallback(_data.HoldTime, () => { });
            return sequence;
        }

        public void Restore()
        {
            for (int i = 0; _homePositions != null && i < _actors.Length; i++)
            {
                Transform actor = _actors[i].transform;
                actor.DOKill();
                actor.localPosition = _homePositions[i];
                actor.localScale = _homeScales[i];
                _actors[i].sprite = _homeSprites[i];
                _actors[i].flipX = _homeFlips[i];
            }

            SetHidden(false);
            gameObject.SetActive(false);
        }

        private static Tween BuildTween(Transform actor, CreditsShotStep step, Vector3 position, Vector3 homeScale)
        {
            switch (step.Action)
            {
                case CreditsShotAction.Punch:
                    return actor.DOPunchScale(homeScale * step.Strength, step.Duration, 6);
                case CreditsShotAction.Shake:
                    return actor.DOShakePosition(step.Duration, new Vector3(step.Strength, 0f, 0f), 25, 0f, false, true);
                case CreditsShotAction.Hop:
                    // 이동 거리를 주면 콩콩 뛰며 그만큼 옮겨 간다(걷기).
                    return actor.DOLocalJump(position + (Vector3)step.Move, step.Strength, step.Count, step.Duration).SetEase(Ease.Linear);
                case CreditsShotAction.Move:
                    return actor.DOLocalMove(position + (Vector3)step.Move, step.Duration).SetEase(Ease.InOutQuad);
                default:
                    return null;
            }
        }

        private void Remember()
        {
            int count = _actors.Length;
            _homePositions = new Vector3[count];
            _homeScales = new Vector3[count];
            _homeSprites = new Sprite[count];
            _homeFlips = new bool[count];
            for (int i = 0; i < count; i++)
            {
                _homePositions[i] = _actors[i].transform.localPosition;
                _homeScales[i] = _actors[i].transform.localScale;
                _homeSprites[i] = _actors[i].sprite;
                _homeFlips[i] = _actors[i].flipX;
            }
        }

        private void SetHidden(bool isHidden)
        {
            if (_hideWhilePlaying == null)
            {
                return;
            }

            if (isHidden)
            {
                _hiddenWasActive = new bool[_hideWhilePlaying.Length];
            }

            for (int i = 0; i < _hideWhilePlaying.Length; i++)
            {
                GameObject target = _hideWhilePlaying[i];
                if (target == null)
                {
                    continue;
                }

                if (isHidden)
                {
                    _hiddenWasActive[i] = target.activeSelf;
                    target.SetActive(false);
                }
                else if (_hiddenWasActive != null)
                {
                    target.SetActive(_hiddenWasActive[i]);
                }
            }
        }
    }
}
