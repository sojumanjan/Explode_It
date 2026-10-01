using TMPro;
using UnityEngine;

namespace ExplodeIt.UI
{
    // "mm:ss"를 문자열 할당 없이 써 넣는다. HUD 시계는 매초 바뀌므로 string.Format을 쓰면 계속 쓰레기가 생긴다.
    public static class TimeText
    {
        public static void Write(TMP_Text text, char[] buffer, int totalSeconds)
        {
            int minutes = Mathf.Min(totalSeconds / 60, 99);
            int seconds = totalSeconds % 60;

            buffer[0] = (char)('0' + minutes / 10);
            buffer[1] = (char)('0' + minutes % 10);
            buffer[2] = ':';
            buffer[3] = (char)('0' + seconds / 10);
            buffer[4] = (char)('0' + seconds % 10);
            text.SetCharArray(buffer, 0, 5);
        }
    }
}
