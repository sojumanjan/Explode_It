using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Stage
{
    // 첫 판 조작 튜토리얼. 퀘스트처럼 체크박스가 달린 과제를 하나씩 해내야 다음으로 넘어간다.
    // 이동 → 폭탄 → 대시 → 스킬. 스킬(블랙홀)이 터지면 화면 아래 게이지만 밝게 남기고 설명을 띄우며 첫 웨이브를 연다.
    [CreateAssetMenu(fileName = "TutorialData", menuName = "Explode It/Story/Tutorial Data")]
    public class TutorialData : ScriptableObject
    {
        [Header("과제 문구 (고블린 머리 위, 체크박스 옆)")]
        [Tooltip("이동 과제. 정한 거리만큼 움직이면 체크된다")]
        [SerializeField, TextArea] private string _moveTask = "<color=#FFD54A>WASD</color>로 이동하기";

        [Tooltip("폭탄 과제. 폭탄을 한 번 던지면 체크된다")]
        [SerializeField, TextArea] private string _throwTask = "<color=#FFD54A>좌클릭</color>으로 폭탄 던지기";

        [Tooltip("대시 과제. 한 번 대시하면 체크된다")]
        [SerializeField, TextArea] private string _dashTask = "<color=#FFD54A>Space</color>로 대시하기";

        [Tooltip("대시 과제를 해낸 뒤 체크된 채로 바뀌는 설명")]
        [SerializeField, TextArea] private string _dashDoneText = "대시하는 동안은 <color=#FFD54A>무적</color>입니다!";

        [Tooltip("스킬 과제. 우클릭으로 블랙홀을 던지면 체크된다. 이 과제가 뜨기 전에는 스킬을 쓸 수 없다")]
        [SerializeField, TextArea] private string _abilityTask = "<color=#FFD54A>우클릭</color>으로 강력한 스킬 사용하기";

        [Tooltip("스킬 과제를 해낸 뒤 체크된 채로 바뀌는 설명. 블랙홀이 터져 사라질 때까지 떠 있다")]
        [SerializeField, TextArea] private string _abilityDoneText = "<color=#FFD54A>블랙홀</color>은 적들을 가운데로 끌어모아 한 방에 터뜨립니다!";

        [Tooltip("블랙홀이 터진 뒤 화면 아래 게이지를 밝게 강조하며 뜨는 설명. 이 설명이 걷히면 첫 웨이브가 시작된다")]
        [SerializeField, TextArea] private string _gaugeText = "게이지 한 칸이 가득 차면 스킬을 사용할 수 있습니다.\n적을 처치해 게이지를 쌓아 스킬을 사용하세요!";

        [Header("진행")]
        [Tooltip("시작하고 첫 과제가 뜨기까지 (초)")]
        [SerializeField, Min(0f)] private float _startDelay = 0.4f;

        [Tooltip("이동 과제를 해내려면 움직여야 하는 거리 (유닛, 누적)")]
        [SerializeField, Min(0.1f)] private float _moveDistance = 2.5f;

        [Tooltip("체크된 뒤 다음 과제로 넘어가기까지 (초)")]
        [SerializeField, Min(0f)] private float _taskCompleteHold = 0.9f;

        [Tooltip("대시 과제는 무적 설명을 읽을 시간을 더 준다. 체크된 뒤 다음 과제까지 (초)")]
        [SerializeField, Min(0f)] private float _dashDoneHold = 2f;

        [Tooltip("블랙홀이 터지고 게이지 강조 설명이 뜨기까지 (초)")]
        [SerializeField, Min(0f)] private float _gaugeFocusDelay = 0.6f;

        [Tooltip("게이지 강조 설명이 떠 있는 시간 (초). 걷히고 나서 첫 웨이브가 시작된다")]
        [SerializeField, Min(0.5f)] private float _gaugeFocusTime = 4f;

        public string MoveTask => L10n.Data(_moveTask);
        public string ThrowTask => L10n.Data(_throwTask);
        public string DashTask => L10n.Data(_dashTask);
        public string DashDoneText => L10n.Data(_dashDoneText);
        public string AbilityTask => L10n.Data(_abilityTask);
        public string AbilityDoneText => _abilityDoneText;
        public string GaugeText => L10n.Data(_gaugeText);
        public float StartDelay => _startDelay;
        public float MoveDistance => _moveDistance;
        public float TaskCompleteHold => _taskCompleteHold;
        public float DashDoneHold => _dashDoneHold;
        public float GaugeFocusDelay => _gaugeFocusDelay;
        public float GaugeFocusTime => _gaugeFocusTime;
    }
}
