using UnityEngine;

namespace ExplodeIt.Enemies
{
    // EnemyVisual 그림을 한 번 움찔하게 한다. 확 밀렸다가 빠르게 돌아오고, 그동안 납작해졌다 펴지며 번쩍인다.
    // 그림 위치(Shift)와 크기(ExtraScale)만 건드린다. 크기를 다른 연출에 쓰는 보스(광부)는 납작함을 0으로 두면 크기를 건드리지 않는다.
    public class VisualFlinch
    {
        private readonly EnemyVisual _visual;
        private FlinchSettings _settings;
        private Vector2 _direction;
        // 음수면 움찔 중이 아니다.
        private float _time = -1f;

        public VisualFlinch(EnemyVisual visual)
        {
            _visual = visual;
        }

        public void Play(FlinchSettings settings, Vector2 away)
        {
            if (_visual == null || settings == null)
            {
                return;
            }

            _settings = settings;
            _direction = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.up;
            _time = 0f;
            // 이미 자기 번쩍임이 있는 보스(광부 과열)는 번쩍임 시간을 0으로 두어 덮어쓰지 않게 한다.
            if (settings.FlashDuration > 0f)
            {
                _visual.Flash(settings.FlashColor, settings.FlashDuration);
            }
        }

        // 남은 비율의 제곱으로 줄여 처음 한두 프레임에 대부분이 보인다.
        public void Tick(float deltaTime)
        {
            if (_time < 0f)
            {
                return;
            }

            _time += deltaTime;
            float k = _time / _settings.Duration;
            if (k >= 1f)
            {
                Stop();
                return;
            }

            float strength = (1f - k) * (1f - k);
            float squash = _settings.Squash * strength;
            _visual.Shift = _direction * (_settings.Distance * strength);
            if (_settings.Squash > 0f)
            {
                _visual.ExtraScale = new Vector2(1f + squash, 1f - squash);
            }
        }

        public void Stop()
        {
            if (_time < 0f)
            {
                return;
            }

            _time = -1f;
            _visual.Shift = Vector2.zero;
            if (_settings.Squash > 0f)
            {
                _visual.ExtraScale = Vector2.one;
            }
        }
    }
}
