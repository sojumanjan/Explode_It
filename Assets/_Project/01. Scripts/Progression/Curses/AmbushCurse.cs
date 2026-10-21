using System;
using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "기습"의 표시를 그린다. 자리 고르기와 스폰은 스테이지 진행이 하고, 나오기 전에 알림을 보내면 그 자리에 표시를 낸다.
    // 왕국 깃발이 하늘에서 떨어져 꽂히고, 플레이어 쪽을 바라본 채 흔들리다가 적이 나오는 순간 작아지며 사라진다.
    // 깃발 그림이 없으면 차오르는 원으로 대신 표시한다.
    public class AmbushCurse : CurseBehaviour<AmbushCurseData>
    {
        [SerializeField] private CurseCircle _circlePrefab;
        [Tooltip("깃발 프리팹 (SpriteRenderer)")]
        [SerializeField] private SpriteRenderer _flagPrefab;
        [SerializeField, Min(1)] private int _prewarm = 6;
        [SerializeField, Min(1)] private int _maxMarkers = 24;

        // 꽂히는 순간 눌렸다 펴지는 비율과 시간 (초). 그림 전용 연출이라 데이터로 빼지 않는다.
        private static readonly Vector2 LandSquash = new Vector2(1.18f, 0.82f);
        private const float LandSquashTime = 0.07f;
        private const float LandRecoverTime = 0.12f;

        private ComponentPool<CurseCircle> _pool;
        private ComponentPool<SpriteRenderer> _flagPool;
        private Action<CurseCircle> _onDone;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            _pool = new ComponentPool<CurseCircle>(_circlePrefab, transform, _prewarm, _maxMarkers);
            _pool.Prewarm(_prewarm);
            _flagPool = new ComponentPool<SpriteRenderer>(_flagPrefab, transform, _prewarm, _maxMarkers);
            _flagPool.Prewarm(_prewarm);
            _onDone = Release;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GameEvents.SpawnWarned += OnSpawnWarned;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameEvents.SpawnWarned -= OnSpawnWarned;
        }

        private void OnSpawnWarned(Vector2 position, float radius, float duration)
        {
            if (Data.FlagSprite != null)
            {
                PlantFlag(position, duration);
            }
            else
            {
                CurseCircle circle = _pool.Get(position);
                circle.Show(position, radius, Data.RangeColor, Data.FillColor);
                circle.Countdown(duration, true, 0f, _onDone);
            }

            AudioManager.Play(Data.WarnSound);
        }

        // 떨어져 꽂힘 → 눌렸다 펴짐 → 흔들림 → 적이 나오는 순간 작아지며 사라짐. 사라진 뒤 풀로 돌아간다.
        private void PlantFlag(Vector2 position, float duration)
        {
            SpriteRenderer flag = _flagPool.Get(position);
            Transform body = flag.transform;
            body.DOKill();
            flag.DOKill();
            flag.sprite = Data.FlagSprite;
            flag.color = Color.white;

            float height = Data.FlagSprite.bounds.size.y;
            float scale = height > 0.0001f ? Data.FlagHeight / height : 1f;
            // 그림이 보는 쪽과 플레이어가 있는 쪽이 다르면 좌우를 뒤집는다.
            bool playerIsRight = PlayerPosition.x > position.x;
            float facing = playerIsRight == Data.FlagFacesRight ? 1f : -1f;
            var baseScale = new Vector3(scale * facing, scale, 1f);
            body.localScale = baseScale;
            body.localRotation = Quaternion.identity;
            body.position = new Vector3(position.x, position.y + Data.FlagDropHeight, 0f);

            float sway = Data.FlagSwayAngle;
            float landTime = Data.FlagDropTime + LandSquashTime + LandRecoverTime;
            float holdTime = Mathf.Max(0f, duration - landTime);
            var squash = new Vector3(baseScale.x * LandSquash.x, baseScale.y * LandSquash.y, 1f);
            Sequence sequence = DOTween.Sequence()
                .Append(body.DOMoveY(position.y, Data.FlagDropTime).SetEase(Ease.InQuad))
                .Append(body.DOScale(squash, LandSquashTime).SetEase(Ease.OutQuad))
                .Append(body.DOScale(baseScale, LandRecoverTime).SetEase(Ease.OutBack));
            if (sway > 0f && holdTime > 0f)
            {
                // 가운데에서 한쪽 끝으로 간 뒤 양쪽을 오간다. 기준점이 깃대 밑동이라 땅에 꽂힌 채 흔들린다.
                // 처음 반 번 + swings번이 꽂혀 있는 시간에 딱 맞게 끝나도록 한 번 시간을 맞춘다. 적이 나오는 순간 사라진다.
                int swings = Mathf.Max(1, Mathf.FloorToInt(holdTime / Data.FlagSwayTime));
                float swingTime = holdTime / (swings + 0.5f);
                sequence.Append(body.DOLocalRotate(new Vector3(0f, 0f, sway), swingTime * 0.5f).SetEase(Ease.OutSine));
                sequence.Append(body.DOLocalRotate(new Vector3(0f, 0f, -sway), swingTime).SetEase(Ease.InOutSine)
                    .SetLoops(swings, LoopType.Yoyo));
            }
            else
            {
                sequence.AppendInterval(holdTime);
            }

            sequence.Append(body.DOScale(Vector3.zero, Data.FlagVanishTime).SetEase(Ease.InBack))
                .Join(flag.DOFade(0f, Data.FlagVanishTime))
                .OnComplete(() => _flagPool.Release(flag))
                .SetId(RunCurses.TweenId)
                .SetLink(flag.gameObject);
        }

        private void Release(CurseCircle circle)
        {
            _pool.Release(circle);
        }
    }
}
