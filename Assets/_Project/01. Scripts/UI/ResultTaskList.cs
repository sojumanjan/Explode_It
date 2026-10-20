using System;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 결과 화면의 "할 일": 아직 못 깬 업적을 몇 개 골라 다음에 노릴 목표로 보여 준다.
    // 보상(무기·스킬·강화·아티팩트)이 걸린 업적을 먼저, 도감 순서대로 고른다. 숨김 업적은 내용이 드러나므로 뺀다.
    public class ResultTaskList : MonoBehaviour
    {
        [Serializable]
        private class Row
        {
            public GameObject Root;
            public Image Icon;
            public TMP_Text Title;
            public TMP_Text Detail;
            public TMP_Text Progress;
        }

        [SerializeField] private UnlockCatalog _catalog;
        [SerializeField] private Row[] _rows;
        [Tooltip("보여 줄 할 일이 하나도 없을 때 켜는 문구")]
        [SerializeField] private GameObject _emptyText;

        private void OnEnable()
        {
            int filled = Fill(0, true);
            filled = Fill(filled, false);
            for (int i = filled; i < _rows.Length; i++)
            {
                _rows[i].Root.SetActive(false);
            }

            _emptyText.SetActive(filled == 0);
        }

        // 조건(보상 유무)에 맞는 업적으로 빈 줄을 채우고, 채운 줄 수를 돌려준다.
        private int Fill(int filled, bool rewardedOnly)
        {
            AchievementData[] achievements = _catalog.Achievements;
            for (int i = 0; i < achievements.Length && filled < _rows.Length; i++)
            {
                AchievementData achievement = achievements[i];
                if (achievement == null || achievement.IsCompleted || achievement.IsHidden
                    || HasReward(achievement) != rewardedOnly)
                {
                    continue;
                }

                Show(_rows[filled], achievement);
                filled++;
            }

            return filled;
        }

        private static void Show(Row row, AchievementData achievement)
        {
            row.Root.SetActive(true);
            Sprite icon = RewardIcon(achievement);
            row.Icon.sprite = icon;
            row.Icon.enabled = icon != null;
            row.Icon.preserveAspect = true;
            row.Title.text = achievement.DisplayName;
            row.Detail.text = achievement.Description;

            int progress = AchievementTracker.GetProgress(achievement);
            if (progress >= 0)
            {
                row.Progress.SetText("{0} / {1}", Mathf.Min(progress, achievement.Target), achievement.Target);
            }
            else
            {
                row.Progress.SetText("");
            }
        }

        private static bool HasReward(AchievementData achievement)
        {
            return achievement.RewardWeapon != null || achievement.RewardAbility != null
                || achievement.RewardUpgrade != null || achievement.RewardArtifact != null;
        }

        // 보상이 있으면 보상 그림, 없으면 도감 스티커 실루엣.
        private static Sprite RewardIcon(AchievementData achievement)
        {
            if (achievement.RewardWeapon != null)
            {
                return achievement.RewardWeapon.Icon;
            }

            if (achievement.RewardAbility != null)
            {
                return achievement.RewardAbility.Icon;
            }

            if (achievement.RewardUpgrade != null)
            {
                return achievement.RewardUpgrade.Icon;
            }

            if (achievement.RewardArtifact != null)
            {
                return achievement.RewardArtifact.Icon;
            }

            return achievement.StickerSilhouette != null ? achievement.StickerSilhouette : achievement.Sticker;
        }
    }
}
