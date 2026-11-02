using UnityEngine;

namespace ExplodeIt.Core
{
    // pixelfx로 렌더한 스프라이트 시트 한 장의 재생 정보. 에디터 메뉴 "Tools/pixelfx/Import Sheet"가 JSON을 읽어 채운다.
    // 다시 가져오면 덮어쓰므로 손으로 고친 값은 재가져오기 때 사라진다. 재생 속도를 바꾸려면 플레이어의 배율을 쓴다.
    [CreateAssetMenu(fileName = "PxFx_", menuName = "Explode It/Pixel Fx Clip")]
    public class PixelFxClip : ScriptableObject
    {
        [Tooltip("pixelfx 이펙트 id (카테고리/이름). 어떤 원본에서 뽑았는지 찾을 때 쓴다")]
        [SerializeField] private string _sourceId;
        [Tooltip("재생 순서대로 놓인 프레임 그림")]
        [SerializeField] private Sprite[] _frames;
        [Tooltip("초당 프레임 수 (fps). pixelfx 시트의 meta.pixelfx.fps")]
        [SerializeField, Min(1f)] private float _fps = 24f;
        [Tooltip("켜면 끝없이 반복한다. 끄면 한 번 재생하고 끝난다")]
        [SerializeField] private bool _loop;

        public string SourceId => _sourceId;
        public Sprite[] Frames => _frames;
        public float Fps => _fps;
        public bool Loop => _loop;
        public float Duration => _frames != null && _frames.Length > 0 ? _frames.Length / _fps : 0f;

#if UNITY_EDITOR
        public void EditorSet(string sourceId, Sprite[] frames, float fps, bool loop)
        {
            _sourceId = sourceId;
            _frames = frames;
            _fps = fps;
            _loop = loop;
        }
#endif
    }
}
