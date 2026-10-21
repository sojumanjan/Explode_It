using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 무한 모드 판 도중 다음 저주의 해금 조건을 본다. 저주는 목록 순서대로 하나씩만 열린다.
    // 조건: 바로 다음 차례 저주가 요구하는 수만큼 저주를 걸고, 이번 판 처치 수가 정해진 수를 넘는다(저주로 죽은 적 포함).
    // 한 판에 최대 하나만 연다. 열면 바로 저장하고 알림을 띄운다(판 도중에 죽어도 해금은 남는다).
    public class CurseUnlockTracker : MonoBehaviour
    {
        [SerializeField] private UnlockCatalog _catalog;

        private CurseData _next;
        private int _kills;
        private bool _isActive;

        private void OnEnable()
        {
            GameEvents.StageStarted += OnStageStarted;
            GameEvents.EnemyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.StageStarted -= OnStageStarted;
            GameEvents.EnemyKilled -= OnEnemyKilled;
        }

        // 판이 시작될 때 이번 판에 노릴 저주를 하나 정해 둔다. 걸어 둔 저주 수가 모자라면 이번 판엔 열 것이 없다.
        private void OnStageStarted(bool isStoryMode)
        {
            _kills = 0;
            _next = isStoryMode ? null : _catalog.NextLockedCurse;
            _isActive = _next != null && RunCurses.Count >= _next.RequiredCurses;
        }

        private void OnEnemyKilled(Vector2 position)
        {
            if (!_isActive)
            {
                return;
            }

            _kills++;
            if (_kills < _next.UnlockKills)
            {
                return;
            }

            _isActive = false;
            if (SaveSystem.Data.UnlockCurse(_next.Id))
            {
                SaveSystem.Save();
                ProgressionEvents.RaiseCurseUnlocked(_next);
            }
        }
    }
}
