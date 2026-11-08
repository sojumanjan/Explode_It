// 그림 모양 그림자(DropShadow)의 모양, 방향, 색, 그리는 순서를 담는 데이터
using UnityEngine;

namespace ExplodeIt.Core
{
    // 그림자 모양. 위에서 내려다본 그림과 서 있는 그림은 그림자를 다르게 만들어야 자연스럽다.
    public enum DropShadowShape
    {
        // 그림을 통째로 오른쪽 아래로 민다. 풀 울타리, 쇠 울타리, 건물처럼 위에서 내려다본 그림
        Offset,
        // 그림 바닥 선에서 위아래로 뒤집어 납작하게 눕힌다. 정면에서 본 서 있는 그림(캐릭터)
        Ground,
        // 그림 모양과 상관없이 바닥 가운데에 타원 하나를 깐다. 벤치, 가로등, 석상, 가구처럼 정면에서 본 소품
        // (뒤집어 눕히는 방식도 소품에서는 어색하다는 사용자 판단, 11/8)
        Ellipse,
    }

    // 맵 울타리·건물, 맵 소품, 캐릭터가 따로 하나씩 쓴다.
    [CreateAssetMenu(fileName = "DropShadow", menuName = "Explode It/Drop Shadow")]
    public class DropShadowData : ScriptableObject
    {
        [Tooltip("Offset: 그림을 통째로 민다(위에서 본 울타리, 건물). Ground: 바닥 선에서 뒤집어 눕힌다(캐릭터). Ellipse: 바닥에 타원(소품)")]
        [SerializeField] private DropShadowShape _shape = DropShadowShape.Offset;

        [Tooltip("그림자를 미는 거리와 방향 (유닛). 빛이 왼쪽 위에서 오므로 오른쪽 아래(+x, -y). Ground는 눕힌 뒤에 더 미는 양")]
        [SerializeField] private Vector2 _offset = new Vector2(0.35f, -0.45f);

        [Tooltip("Ground일 때 그림자 길이 (그림 높이에 대한 비율). 작을수록 납작하게 눕는다")]
        [SerializeField, Range(0.05f, 1f)] private float _groundSquash = 0.3f;

        [Tooltip("Ellipse일 때 깔 타원 그림")]
        [SerializeField] private Sprite _ellipseSprite;

        [Tooltip("Ellipse일 때 타원 너비 (그림 너비에 대한 비율)")]
        [SerializeField, Range(0.2f, 1.5f)] private float _ellipseWidth = 0.85f;

        [Tooltip("Ellipse일 때 타원 높이 (타원 너비에 대한 비율). 작을수록 납작하다")]
        [SerializeField, Range(0.1f, 1f)] private float _ellipseHeight = 0.3f;

        [Tooltip("Ellipse일 때 타원 가운데를 그림 바닥 선에서 올리는 정도 (타원 높이에 대한 비율). 0이면 바닥 선이 타원 가운데")]
        [SerializeField, Range(-0.5f, 1f)] private float _ellipseLift = 0.2f;

        [Tooltip("그림자 색. 불투명도가 그림자 진하기다")]
        [SerializeField] private Color _color = new Color(0.2f, 0.13f, 0.1f, 0.4f);

        [Tooltip("그림자를 그리는 순서(Default 층). 바닥 무늬(-19 이하)보다 위, 소품(-17 이상)보다 아래")]
        [SerializeField] private int _sortingOrder = -18;

        [Tooltip("그림 모양만 한 색으로 칠하고 겹친 그림자는 한 번만 칠하는 재질(SpriteDropShadow). 테두리를 흐리게 하려면 SpriteDropShadowSoft")]
        [SerializeField] private Material _material;

        public DropShadowShape Shape => _shape;
        public Vector2 Offset => _offset;
        public float GroundSquash => _groundSquash;
        public Sprite EllipseSprite => _ellipseSprite;
        public float EllipseWidth => _ellipseWidth;
        public float EllipseHeight => _ellipseHeight;
        public float EllipseLift => _ellipseLift;
        public Color Color => _color;
        public int SortingOrder => _sortingOrder;
        public Material Material => _material;
    }
}
