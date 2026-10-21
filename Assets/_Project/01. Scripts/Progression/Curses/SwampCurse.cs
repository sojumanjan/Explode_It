using DG.Tweening;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주 "먼지바람"을 게임 씬에서 돌린다. 판이 시작되면 바람을 정해진 수만큼 깔고, 간격마다 하나씩 사라졌다 다른 자리에 나타난다.
    // 플레이어가 다 나타난 바람(가로로 긴 타원) 안에 있으면 이동속도 배율을 낮춘다(플레이어 이동이 매 프레임 읽는다).
    // 그림은 몇 장을 차례로 넘겨 바람이 이는 것처럼 보이고, 나타날 때 톡 커지고 사라질 때 오므라든다.
    public class SwampCurse : CurseBehaviour<SwampCurseData>
    {
        private enum Fade
        {
            None,
            In,
            Out
        }

        private struct Swamp
        {
            public CurseCircle Circle;
            public Fade Fade;
            public float FadeTime;
            // 모든 바람이 같은 그림을 동시에 넘기면 복사한 티가 나므로 프레임을 하나씩 어긋낸다.
            public int FrameOffset;
        }

        [SerializeField] private CurseCircle _circlePrefab;
        [Tooltip("먼지바람이 생기지 않을 구조물 레이어")]
        [SerializeField] private LayerMask _obstacleMask;

        // 겹치지 않는 자리를 찾는 횟수. 자리 고르기 안전장치라 데이터로 빼지 않는다.
        private const int SpotAttempts = 24;

        private Swamp[] _swamps;
        private float _nextMoveTime = float.PositiveInfinity;
        private int _nextIndex;
        private bool _hasPlaced;
        private int _frame = -1;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            _swamps = new Swamp[Data.Count];
            for (int i = 0; i < _swamps.Length; i++)
            {
                _swamps[i].Circle = Instantiate(_circlePrefab, transform);
                _swamps[i].Circle.gameObject.SetActive(false);
                _swamps[i].FrameOffset = i;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            RunCurses.PlayerMoveMultiplier = 1f;
        }

        protected override void OnRunStarted()
        {
            for (int i = 0; i < _swamps.Length; i++)
            {
                Appear(i);
            }

            _hasPlaced = true;
            _nextMoveTime = Time.time + Data.MoveInterval;
        }

        private void Update()
        {
            if (!_hasPlaced)
            {
                return;
            }

            TickFades(Time.deltaTime);
            TickFrames();
            RunCurses.PlayerMoveMultiplier = IsInSwamp(PlayerPosition) ? Data.MoveMultiplier : 1f;
            if (!IsRunning)
            {
                _nextMoveTime += Time.deltaTime;
                return;
            }

            if (Time.time < _nextMoveTime)
            {
                return;
            }

            _nextMoveTime = Time.time + Data.MoveInterval;
            _swamps[_nextIndex].Fade = Fade.Out;
            _swamps[_nextIndex].FadeTime = 0f;
            _nextIndex = (_nextIndex + 1) % _swamps.Length;
        }

        // 프레임이 바뀔 때만 그림을 갈아 끼운다.
        private void TickFrames()
        {
            Sprite[] frames = Data.Frames;
            if (frames.Length < 2)
            {
                return;
            }

            int frame = (int)(Time.time / Data.FrameTime);
            if (frame == _frame)
            {
                return;
            }

            _frame = frame;
            for (int i = 0; i < _swamps.Length; i++)
            {
                _swamps[i].Circle.SetSpriteFrame(frames[(frame + _swamps[i].FrameOffset) % frames.Length]);
            }
        }

        // 사라지기를 다 마친 바람은 곧바로 다른 자리에 다시 나타난다.
        private void TickFades(float deltaTime)
        {
            float fadeTime = Data.FadeTime;
            for (int i = 0; i < _swamps.Length; i++)
            {
                ref Swamp swamp = ref _swamps[i];
                if (swamp.Fade == Fade.None)
                {
                    continue;
                }

                swamp.FadeTime += deltaTime;
                float t = Mathf.Clamp01(swamp.FadeTime / fadeTime);
                bool isIn = swamp.Fade == Fade.In;
                swamp.Circle.SetOpacity(isIn ? t : 1f - t);
                swamp.Circle.SetSpriteScaleFactor(isIn
                    ? DOVirtual.EasedValue(0f, 1f, t, Data.AppearEase)
                    : DOVirtual.EasedValue(1f, 0f, t, Data.DisappearEase));
                if (t < 1f)
                {
                    continue;
                }

                if (isIn)
                {
                    swamp.Fade = Fade.None;
                }
                else
                {
                    Appear(i);
                }
            }
        }

        private void Appear(int index)
        {
            ref Swamp swamp = ref _swamps[index];
            Vector2 spot = PickSpot(index);
            swamp.Circle.gameObject.SetActive(true);
            // 원은 위치 기준으로만 쓰고 감춘다. 판정은 그림에 맞춘 타원이다.
            swamp.Circle.Show(spot, Data.RadiusX, Color.clear, Color.clear);
            swamp.Circle.HideRange();
            Sprite[] frames = Data.Frames;
            Color tint = Color.white;
            tint.a = Data.SpriteAlpha;
            swamp.Circle.SetSprite(frames.Length > 0 ? frames[0] : null, tint);
            swamp.Circle.SetSpriteSize(Data.RadiusX * 2f * Data.SpriteSizeRatio);
            swamp.Circle.SetSpriteScaleFactor(0f);
            swamp.Circle.SetOpacity(0f);
            // 새 자리에서도 지금 넘기고 있는 프레임으로 바로 맞춘다.
            _frame = -1;
            swamp.Fade = Fade.In;
            swamp.FadeTime = 0f;
        }

        // 플레이어 둘레에서, 구조물과 겹치지 않고 다른 바람과도 떨어진 자리를 찾는다.
        // 못 찾으면 구조물 조건을 빼고 다시 찾고(바람은 벽 위에 깔려도 문제없다), 그래도 없으면 최대 거리의 한 점에 둔다.
        private Vector2 PickSpot(int index)
        {
            Vector2 player = PlayerPosition;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int attempt = 0; attempt < SpotAttempts; attempt++)
                {
                    Vector2 spot = player + Random.insideUnitCircle.normalized * Random.Range(Data.MinDistance, Data.MaxDistance);
                    bool isOpen = pass == 1 || IsOpenSpot(spot, Data.RadiusY * 0.5f, _obstacleMask);
                    if (isOpen && IsInsideArena(spot) && !OverlapsOthers(spot, index))
                    {
                        return spot;
                    }
                }
            }

            return PointAround(player, Data.MaxDistance, Data.MaxDistance);
        }

        private static bool IsInsideArena(Vector2 point)
        {
            Stage.ArenaBounds arena = Stage.ArenaBounds.Current;
            return arena == null || arena.Contains(point);
        }

        // 두 타원(같은 크기)이 겹치는지. 가로·세로를 반지름으로 나눠 원으로 펴서 비교한다.
        private bool OverlapsOthers(Vector2 spot, int index)
        {
            float rx = Data.RadiusX * 2f + Data.Gap;
            float ry = Data.RadiusY * 2f + Data.Gap;
            for (int i = 0; i < _swamps.Length; i++)
            {
                if (i == index || !_swamps[i].Circle.gameObject.activeSelf)
                {
                    continue;
                }

                Vector2 delta = _swamps[i].Circle.Position - spot;
                float dx = delta.x / rx;
                float dy = delta.y / ry;
                if (dx * dx + dy * dy < 1f)
                {
                    return true;
                }
            }

            return false;
        }

        // 나타나는 중이거나 사라지는 중인 바람은 느려지게 하지 않는다. 다 보이는 바람만 바람이다.
        private bool IsInSwamp(Vector2 position)
        {
            float rx = Data.RadiusX;
            float ry = Data.RadiusY;
            for (int i = 0; i < _swamps.Length; i++)
            {
                if (_swamps[i].Fade != Fade.None)
                {
                    continue;
                }

                Vector2 delta = position - _swamps[i].Circle.Position;
                float dx = delta.x / rx;
                float dy = delta.y / ry;
                if (dx * dx + dy * dy <= 1f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
