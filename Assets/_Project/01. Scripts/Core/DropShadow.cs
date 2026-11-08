// 그림에 한 색 그림자(민 실루엣, 바닥 타원 등)를 자식으로 붙이는 컴포넌트. 맵 소품은 씬에 미리 깔아 두고 손으로 맞춘다
using UnityEngine;

namespace ExplodeIt.Core
{
    // 빛이 왼쪽 위에서 온다고 보고 그림을 오른쪽 아래로 민 실루엣을 바닥 바로 위 순서에 그린다. 아이콘, 고블린 명암과 같은 방향이다.
    // 예전 타원 그림자는 물체 밑에 깔려 거의 가려졌고 모양도 물체와 달라 바꿨다(11/8 사용자).
    // 캐릭터처럼 그림이 바뀌거나 움직이는 것은 시작할 때 그림자를 만들어 매 프레임 따라간다.
    // 맵 소품은 그림자를 씬에 미리 깔아 두고(_shadow), 씬 창에서 크기와 위치를 손으로 맞춘다. 코드는 깔아 둔 그림자의 위치, 크기를 건드리지 않는다.
    // 자동으로 맞춘 크기가 석상은 너무 작고 나무는 너무 커서 사용자가 직접 맞추기로 했다(11/8).
    public class DropShadow : MonoBehaviour
    {
        [SerializeField] private DropShadowData _data;
        [Tooltip("그림자를 만들 그림. 비우면 같은 오브젝트의 SpriteRenderer")]
        [SerializeField] private SpriteRenderer _source;
        [Tooltip("그림이 바뀌거나 움직이면 켠다(캐릭터). 끄면 미리 깔아 둔 그림자를 그대로 쓴다(맵 소품)")]
        [SerializeField] private bool _followsSource;
        [Tooltip("씬에 미리 깔아 둔 그림자. 컴포넌트 메뉴(⋮)의 '그림자 미리 깔기'로 만든다. 비어 있으면 시작할 때 만든다")]
        [SerializeField] private SpriteRenderer _shadow;

        private Transform _shadowTransform;
        private Transform _sourceTransform;

        private void Awake()
        {
            if (!Prepare())
            {
                enabled = false;
                return;
            }

            if (_shadow != null && !_followsSource)
            {
                // 미리 깔아 둔 그림자는 모양과 자리는 그대로 두고, 색과 그리는 순서만 데이터를 따른다(진하기를 한 곳에서 바꾸려고).
                ApplyLook();
                _shadow.color = ShadowColor();
                enabled = false;
                return;
            }

            if (_shadow == null)
            {
                CreateShadow();
            }

            _shadowTransform = _shadow.transform;
            Sync();
            enabled = _followsSource;
        }

        // 에디터에서 그림자를 자식으로 만들어 지금 데이터대로 놓는다. 그 뒤로는 씬 창에서 손으로 맞춘다.
        [ContextMenu("그림자 미리 깔기")]
        private void Bake()
        {
            if (!Prepare())
            {
                return;
            }

            if (_shadow == null)
            {
                CreateShadow();
            }

            _shadowTransform = _shadow.transform;
            Sync();
        }

        private bool Prepare()
        {
            if (_source == null)
            {
                TryGetComponent(out _source);
            }

            if (_source == null || _data == null)
            {
                return false;
            }

            _sourceTransform = _source.transform;
            return true;
        }

        private void CreateShadow()
        {
            var shadowObject = new GameObject("그림자");
            shadowObject.transform.SetParent(_sourceTransform, false);
            _shadow = shadowObject.AddComponent<SpriteRenderer>();
            ApplyLook();
        }

        private void ApplyLook()
        {
            _shadow.sharedMaterial = _data.Material;
            _shadow.sortingLayerID = _source.sortingLayerID;
            _shadow.sortingOrder = _data.SortingOrder;
        }

        // 사라지며 흐려지는 그림(죽는 적 등)은 그림자도 같이 흐려진다.
        private Color ShadowColor()
        {
            Color color = _data.Color;
            color.a *= _source.color.a;
            return color;
        }

        private void LateUpdate()
        {
            Sync();
        }

        private void Sync()
        {
            if (_data.Shape == DropShadowShape.Ellipse)
            {
                SyncEllipse();
                return;
            }

            _shadow.sprite = _source.sprite;
            _shadow.flipX = _source.flipX;
            _shadow.flipY = _source.flipY;
            _shadow.drawMode = _source.drawMode;
            if (_shadow.drawMode != SpriteDrawMode.Simple)
            {
                _shadow.size = _source.size;
            }

            _shadow.enabled = _source.enabled && _source.sprite != null;
            _shadow.color = ShadowColor();
            if (_data.Shape == DropShadowShape.Ground)
            {
                PlaceOnGround();
                return;
            }

            _shadowTransform.localScale = Vector3.one;
            _shadowTransform.position = _sourceTransform.position + (Vector3)_data.Offset;
        }

        // 소품은 그림 모양을 따지지 않고 바닥 가운데에 타원 하나만 깐다. 크기는 그림 너비에 맞춘다.
        private void SyncEllipse()
        {
            Sprite ellipse = _data.EllipseSprite;
            _shadow.sprite = ellipse;
            _shadow.enabled = _source.enabled && _source.sprite != null && ellipse != null;
            _shadow.color = ShadowColor();
            if (ellipse == null)
            {
                return;
            }

            Bounds bounds = _source.localBounds;
            Vector2 size = ellipse.bounds.size;
            float width = bounds.size.x * _data.EllipseWidth;
            float height = width * _data.EllipseHeight;
            _shadowTransform.localScale = new Vector3(width / size.x, height / size.y, 1f);
            _shadowTransform.localPosition = new Vector3(bounds.center.x, bounds.min.y + height * _data.EllipseLift, 0f);
            _shadowTransform.position += (Vector3)_data.Offset;
        }

        // 서 있는 그림(벤치, 가로등, 석상, 가구, 캐릭터)은 정면에서 본 그림이라, 통째로 밀면 뒤에 같은 그림이 떠 있는 것처럼 보였다(사용자 지적).
        // 그림 바닥 선을 기준으로 위아래를 뒤집어 납작하게 눌러 바닥에 눕힌다. 바닥 선은 그림마다 다르므로(캐릭터 움직임 그림) 매번 잰다.
        private void PlaceOnGround()
        {
            float bottom = _source.localBounds.min.y;
            float squash = _data.GroundSquash;
            _shadowTransform.localScale = new Vector3(1f, -squash, 1f);
            // 뒤집고 누르면 바닥 선(bottom)이 -squash * bottom으로 옮겨지므로 그만큼 되돌려 바닥 선을 맞춘다.
            _shadowTransform.localPosition = new Vector3(0f, bottom * (1f + squash), 0f);
            _shadowTransform.position += (Vector3)_data.Offset;
        }
    }
}
