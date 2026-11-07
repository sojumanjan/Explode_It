// 동적 글꼴에 전투 중 띄울 문구의 글자를 미리 넣어 두는 도우미
using TMPro;

namespace ExplodeIt.UI
{
    // 웹 빌드는 동적 글꼴의 글자 모음이 비어 있는 채로 시작해서, 처음 보는 글자는 화면에 띄우는 그 프레임에 모양을 계산하고 글자 텍스처를 다시 올린다.
    // 업적 알림, 왕 컷인처럼 전투 중에 처음 보는 한글이 한꺼번에 나오면 그 순간 끊긴다. 판이 시작될 때 미리 넣어 비용을 로딩으로 옮긴다.
    public static class GlyphWarmup
    {
        public static void Add(TMP_Text text, string characters)
        {
            if (text == null || string.IsNullOrEmpty(characters))
            {
                return;
            }

            TMP_FontAsset font = text.font;
            if (font == null || font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
            {
                return;
            }

            // 이미 있는 글자는 건너뛰므로 같은 문구를 다시 넣어도 비용이 거의 없다.
            font.TryAddCharacters(characters, out string _);
        }
    }
}
