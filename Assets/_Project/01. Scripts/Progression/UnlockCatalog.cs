using ExplodeIt.Bombs;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 고를 수 있는 무기·아티팩트와 업적 전체 목록. 메뉴와 게임 씬은 이 목록 하나만 보고 ID로 찾는다.
    // 목록 순서가 곧 메뉴·도감에 보이는 순서다.
    [CreateAssetMenu(fileName = "UnlockCatalog", menuName = "Explode It/Progression/Unlock Catalog")]
    public class UnlockCatalog : ScriptableObject
    {
        [Tooltip("고를 수 있는 무기. 처음부터 열린 무기 중 맨 앞의 것이 기본 무기다")]
        [SerializeField] private WeaponData[] _weapons = new WeaponData[0];

        [Tooltip("고를 수 있는 아티팩트")]
        [SerializeField] private ArtifactData[] _artifacts = new ArtifactData[0];

        [Tooltip("도감에 붙는 업적")]
        [SerializeField] private AchievementData[] _achievements = new AchievementData[0];

        [Tooltip("판 시작 전 고를 수 있는 아티팩트 칸 수")]
        [SerializeField, Min(0)] private int _artifactSlots = 2;

        public WeaponData[] Weapons => _weapons;
        public ArtifactData[] Artifacts => _artifacts;
        public AchievementData[] Achievements => _achievements;
        public int ArtifactSlots => _artifactSlots;

        public WeaponData DefaultWeapon
        {
            get
            {
                for (int i = 0; i < _weapons.Length; i++)
                {
                    if (_weapons[i].IsUnlocked)
                    {
                        return _weapons[i];
                    }
                }

                return _weapons.Length > 0 ? _weapons[0] : null;
            }
        }

        public int UnlockedWeaponCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _weapons.Length; i++)
                {
                    if (_weapons[i].IsUnlocked)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int UnlockedArtifactCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _artifacts.Length; i++)
                {
                    if (_artifacts[i].IsUnlocked)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int CompletedAchievementCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _achievements.Length; i++)
                {
                    if (_achievements[i].IsCompleted)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        // 저장된 ID가 지워졌거나 잠긴 무기를 가리키면 기본 무기를 돌려준다.
        public WeaponData FindUsableWeapon(string id)
        {
            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i].Id == id && _weapons[i].IsUnlocked)
                {
                    return _weapons[i];
                }
            }

            return DefaultWeapon;
        }

        // 잠겼거나 없는 아티팩트면 null. 판 시작 때 그 칸은 비운다.
        public ArtifactData FindUsableArtifact(string id)
        {
            for (int i = 0; i < _artifacts.Length; i++)
            {
                if (_artifacts[i].Id == id && _artifacts[i].IsUnlocked)
                {
                    return _artifacts[i];
                }
            }

            return null;
        }

        // 잠긴 카드에 해금 조건을 보여 주려고, 이 보상을 주는 업적을 찾는다. 없으면 null.
        public AchievementData FindAchievementRewarding(Object reward)
        {
            for (int i = 0; i < _achievements.Length; i++)
            {
                AchievementData achievement = _achievements[i];
                if (achievement.RewardWeapon == reward || achievement.RewardArtifact == reward)
                {
                    return achievement;
                }
            }

            return null;
        }
    }
}
