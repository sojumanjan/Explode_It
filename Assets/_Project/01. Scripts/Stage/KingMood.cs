namespace ExplodeIt.Stage
{
    // 왕의 표정. 대사마다 골라서 그 줄을 말할 때 왕 그림이 이 표정으로 바뀐다.
    public enum KingMood
    {
        Smug,
        // 삐침(king_pout). 처음엔 Annoyed(화남)이라 불렀다. 번호가 같아 저장된 대사는 그대로다.
        Pout,
        Laugh,
        Cry,
        Lazy,
        LetsGo,
        // 뒤에 추가한 표정. 이미 저장된 대사 데이터의 표정 번호가 밀리지 않게 항상 끝에 붙인다.
        Shock,
        Speechless,
        Rage
    }
}
