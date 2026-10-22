// 만화 페이지(IntroComic)가 읽는 컷 데이터. 오프닝과 엔딩이 같은 페이지를 함께 쓴다.
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Stage
{
    public interface IComicData
    {
        int PanelCount { get; }
        Sprite GetPanelSprite(int index);
        string GetPanelLine(int index);
        float PanelPopTime { get; }
        float CaptionCharsPerSecond { get; }
        SoundData PanelSound { get; }
        SoundData ClickSound { get; }
    }
}
