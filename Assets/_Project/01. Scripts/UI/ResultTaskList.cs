using System;
using ExplodeIt.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplodeIt.UI
{
    // 결과 화면의 "할 일": 무기·스킬·아티팩트 칸마다 다음에 노릴 해금 업적을 하나씩 보여 준다.
    // 아이콘은 그 업적으로 얻을 물건을 메인 메뉴 책의 잠긴 칸과 같은 실루엣으로 보여 줘, 무엇이 열리는지 궁금하게 만든다.
    // 칸마다 깨기 쉬운 순서로 업적을 늘어놓고, 아직 못 깬 첫 업적을 띄운다. 스킬을 다 열었으면 스킬 칸은 스킬 강화로 넘어간다.
    // 한 칸의 업적을 모두 깼으면 "모든 무기 해금"처럼 다 열었다고 알린다.
    public class ResultTaskList : MonoBehaviour
    {
        [Serializable]
        private class Row
        {
            public Image Icon;
            public TMP_Text Title;
            public TMP_Text Detail;
            public TMP_Text Progress;
        }

        [Serializable]
        private class Category
        {
            [Tooltip("칸 제목 (예: 무기)")]
            public string Label;
            [Tooltip("이 분류의 업적을 다 깼을 때 문구")]
            public string DoneText;
            [Tooltip("깨기 쉬운 순서로 늘어놓은 해금 업적. 앞에서부터 아직 못 깬 것을 띄운다")]
            public AchievementData[] Order;
        }

        [SerializeField] private Row _weaponRow;
        [SerializeField] private Row _skillRow;
        [SerializeField] private Row _artifactRow;

        [SerializeField] private Category _weapons;
        [SerializeField] private Category _skills;
        [Tooltip("스킬 업적을 다 깬 뒤 스킬 칸에 이어서 보여 줄 스킬 강화 업적")]
        [SerializeField] private Category _upgrades;
        [SerializeField] private Category _artifacts;

        private void OnEnable()
        {
            Show(_weaponRow, _weapons);
            Show(_skillRow, FindNext(_skills) != null ? _skills : _upgrades);
            Show(_artifactRow, _artifacts);
        }

        private static void Show(Row row, Category category)
        {
            AchievementData next = FindNext(category);
            Sprite icon = next != null ? RewardIcon(next) : null;
            row.Icon.sprite = icon;
            row.Icon.enabled = icon != null;
            row.Icon.color = ItemCard.LockedTint;
            row.Icon.preserveAspect = true;

            if (next == null)
            {
                row.Title.text = category.DoneText;
                row.Detail.text = "";
                row.Progress.SetText("");
                return;
            }

            row.Title.text = category.Label;
            row.Detail.text = next.Description;
            int progress = AchievementTracker.GetProgress(next);
            if (progress >= 0)
            {
                row.Progress.SetText("{0} / {1}", Mathf.Min(progress, next.Target), next.Target);
            }
            else
            {
                row.Progress.SetText("");
            }
        }

        // 스킬 강화 업적은 그 스킬을 아직 못 열었으면 깰 수 없으므로 건너뛴다.
        private static AchievementData FindNext(Category category)
        {
            AchievementData[] order = category.Order;
            for (int i = 0; i < order.Length; i++)
            {
                AchievementData achievement = order[i];
                if (achievement == null || achievement.IsCompleted)
                {
                    continue;
                }

                if (achievement.Ability != null && !achievement.Ability.IsUnlocked)
                {
                    continue;
                }

                return achievement;
            }

            return null;
        }

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

            return achievement.RewardArtifact != null ? achievement.RewardArtifact.Icon : null;
        }
    }
}
