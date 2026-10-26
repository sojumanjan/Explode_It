using System;
using DG.Tweening;
using ExplodeIt.Bombs;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 저주가 땅에 까는 원 하나. 범위(바깥 원)를 보여 주고, 필요하면 안쪽 원을 채워 남은 시간을 알린다.
    // 유폭·낙뢰·기습 표시·덫·늪이 모두 이 그림 하나를 풀에서 꺼내 쓴다.
    public class CurseCircle : MonoBehaviour
    {
        // 낙뢰 직전 번쩍임이 한 번 켜졌다 꺼지는 주기 (초). 그림 전용 연출이라 데이터로 빼지 않는다.
        private const float FlashPeriod = 0.1f;
        // 번쩍임 줄이기를 켜면 초당 3번 아래로 느리게 깜빡인다.
        private const float ReducedFlashPeriod = 0.4f;

        [SerializeField] private ExplosionShape _range;
        [SerializeField] private ExplosionShape _fill;
        // 덫처럼 원 위에 물건 그림이 필요할 때만 쓴다.
        [SerializeField] private SpriteRenderer _sprite;

        private Action<CurseCircle> _onDone;
        private float _radius;
        private float _duration;
        private float _time;
        private float _flashTime;
        private bool _fills;
        private bool _isCounting;
        private Color _rangeColor;
        private Color _fillColor;
        private Vector3 _spriteScale = Vector3.one;
        // 그림을 반투명하게 깔 때(먼지바람)의 최대 투명도. 나타나고 사라지는 연출이 이 값에 비율을 곱한다.
        private float _spriteAlpha = 1f;
        private bool _hasSpriteScale;

        public Vector2 Position { get; private set; }
        public float Radius => _radius;

        public void Show(Vector2 position, float radius, Color rangeColor, Color fillColor)
        {
            Position = position;
            transform.position = position;
            // 원 크기는 크기 배율 1을 기준으로 그리므로, 커지는 연출 중에 다시 꺼냈어도 먼저 원래 크기로 돌린다.
            transform.DOKill();
            transform.localScale = Vector3.one;
            _rangeColor = rangeColor;
            _fillColor = fillColor;
            _isCounting = false;
            _onDone = null;
            _range.SetColor(rangeColor);
            _range.Visible = true;
            _fill.SetColor(fillColor);
            _fill.Visible = false;
            SetRadius(radius);
            if (_sprite != null)
            {
                RememberSpriteScale();
                _sprite.transform.DOKill();
                _sprite.DOKill();
                _sprite.transform.localScale = _spriteScale;
                _sprite.enabled = false;
            }
        }

        // 덫이 밟혔을 때: 탁 닫히듯 가로로 오므라들었다가 작아지며 사라진다. 원(폭발 범위)은 그대로 남아 차오른다.
        // 오므라드는 순간(초)과 비율은 그림 전용 연출이라 데이터로 빼지 않는다.
        public void PlaySpriteSnap(float vanishTime)
        {
            if (_sprite == null || !_sprite.enabled)
            {
                return;
            }

            RememberSpriteScale();
            Transform body = _sprite.transform;
            body.DOKill();
            _sprite.DOKill();
            Vector3 snap = new Vector3(_spriteScale.x * 0.7f, _spriteScale.y * 1.12f, _spriteScale.z);
            DOTween.Sequence()
                .Append(body.DOScale(snap, 0.06f).SetEase(Ease.OutQuad))
                .Append(body.DOScale(_spriteScale * 0.55f, vanishTime).SetEase(Ease.InQuad))
                .Join(_sprite.DOFade(0f, vanishTime).SetEase(Ease.InQuad))
                .SetId(RunCurses.TweenId)
                .SetLink(gameObject);
        }

        private void RememberSpriteScale()
        {
            if (!_hasSpriteScale)
            {
                _spriteScale = _sprite.transform.localScale;
                _hasSpriteScale = true;
            }
        }

        // 0에서 원래 크기로 커지며 나타난다. 판정 범위와 시간은 그대로라 그림만 커진다.
        // 넘쳐 커졌다 줄어드는 움직임은 실제보다 넓어 보이므로 쓰지 않는다.
        public void PlayAppear(float duration, Ease ease = Ease.OutCubic)
        {
            if (duration <= 0f)
            {
                return;
            }

            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, duration).SetEase(ease).SetId(RunCurses.TweenId).SetLink(gameObject);
        }

        // 안쪽 원을 끝까지 채운 채 둔다(늪처럼 시간 없이 깔려 있는 영역).
        public void ShowFilled()
        {
            _isCounting = false;
            _fill.SetRadius(_radius);
            _fill.Visible = true;
        }

        // 그림을 지름 diameter(유닛)에 맞춘다. 그림 크기와 상관없이 판정 원과 같은 크기로 깔린다.
        public void SetSpriteSize(float diameter)
        {
            if (_sprite == null || _sprite.sprite == null)
            {
                return;
            }

            float width = _sprite.sprite.bounds.size.x;
            _spriteScale = Vector3.one * (width > 0.0001f ? diameter / width : 1f);
            _hasSpriteScale = true;
            _sprite.transform.localScale = _spriteScale;
        }

        // 그림만 다른 프레임으로 바꾼다. 색·크기는 그대로 둔다.
        public void SetSpriteFrame(Sprite sprite)
        {
            if (_sprite != null && _sprite.enabled && sprite != null)
            {
                _sprite.sprite = sprite;
            }
        }

        // 맞춰 둔 그림 크기에 배율을 곱한다(나타날 때 커지고 사라질 때 작아지는 연출).
        public void SetSpriteScaleFactor(float factor)
        {
            if (_sprite != null)
            {
                _sprite.transform.localScale = _spriteScale * factor;
            }
        }

        // 원을 감추고 물건 그림만 남긴다(밟힌 덫이 닫히며 사라지는 동안).
        public void HideRange()
        {
            _range.Visible = false;
            _fill.Visible = false;
        }

        // 원 크기만 바꾼다(덫이 밟히면 밟힘 판정 원에서 폭발 범위로 커진다).
        public void SetRadius(float radius)
        {
            _radius = radius;
            _range.BuildCircle(radius);
            _fill.CopyLimits(_range);
            _fill.SetRadius(0f);
        }

        // duration(초)이 지나면 onDone을 부른다. fills면 안쪽 원이 차오르고, 아니면 flashTime(초)이 남았을 때부터 안쪽 원이 번쩍인다.
        public void Countdown(float duration, bool fills, float flashTime, Action<CurseCircle> onDone)
        {
            _duration = Mathf.Max(duration, 0.01f);
            _time = 0f;
            _fills = fills;
            _flashTime = flashTime;
            _onDone = onDone;
            _isCounting = true;
            _fill.Visible = fills;
            _fill.SetRadius(0f);
        }

        public void SetSprite(Sprite sprite, Color color)
        {
            if (_sprite == null)
            {
                return;
            }

            _sprite.sprite = sprite;
            _sprite.color = color;
            _spriteAlpha = color.a;
            _sprite.enabled = sprite != null;
            _sprite.transform.localRotation = Quaternion.identity;
        }

        // 나타나고 사라지는 연출용. 처음 정한 색의 알파에 비율을 곱한다.
        public void SetOpacity(float opacity)
        {
            Color range = _rangeColor;
            range.a *= opacity;
            _range.SetColor(range);
            Color fill = _fillColor;
            fill.a *= opacity;
            _fill.SetColor(fill);
            if (_sprite != null)
            {
                Color color = _sprite.color;
                color.a = _spriteAlpha * opacity;
                _sprite.color = color;
            }
        }

        private void Update()
        {
            if (!_isCounting)
            {
                return;
            }

            _time += RunCurses.DeltaTime;
            if (_fills)
            {
                _fill.SetRadius(_radius * Mathf.Clamp01(_time / _duration));
            }
            else if (_flashTime > 0f && _duration - _time <= _flashTime)
            {
                _fill.SetRadius(_radius);
                float period = FlashSafety.IsReduced ? ReducedFlashPeriod : FlashPeriod;
                _fill.Visible = Mathf.Repeat(_time, period) < period * 0.5f;
            }

            if (_time >= _duration)
            {
                _isCounting = false;
                _onDone?.Invoke(this);
            }
        }
    }
}
