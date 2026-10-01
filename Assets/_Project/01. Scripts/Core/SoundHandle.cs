namespace ExplodeIt.Core
{
    // 재생 중인 효과음을 도중에 멈추기 위한 표. 소스는 돌려 쓰므로, 그 사이 다른 소리가 들어갔다면
    // 재생 번호가 달라 엉뚱한 소리를 끊지 않는다.
    public readonly struct SoundHandle
    {
        public static readonly SoundHandle None = new SoundHandle(-1, 0);

        public readonly int Voice;
        public readonly int PlayId;

        public SoundHandle(int voice, int playId)
        {
            Voice = voice;
            PlayId = playId;
        }
    }
}
