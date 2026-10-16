using System.Collections.Generic;
using ExplodeIt.Bombs;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 메뉴에서 장착한 무기·아티팩트를 판이 시작될 때 실제로 적용한다.
    // 잠겼거나 목록에 없는 무기를 가리키면 카탈로그가 기본 무기를 돌려주므로, 해금한 무기만 판에 들어온다.
    // 장착한 아티팩트는 다른 컴포넌트가 Awake에서 값을 읽기 전에 판 보정값(RunModifiers)에 채워 둔다. 그래서 가장 먼저 실행한다.
    [DefaultExecutionOrder(-200)]
    public class RunLoadoutApplier : MonoBehaviour
    {
        [SerializeField] private UnlockCatalog _catalog;
        [SerializeField] private BombLauncher _launcher;

        private void Awake()
        {
            RunModifiers.Reset();
            IReadOnlyList<string> ids = RunSetup.ArtifactIds;
            for (int i = 0; i < ids.Count; i++)
            {
                ArtifactData artifact = _catalog.FindUsableArtifact(ids[i]);
                if (artifact != null)
                {
                    Apply(artifact);
                }
            }
        }

        // 같은 종류를 두 칸에 낄 수는 없지만(칸끼리 맞바꿈), 값은 더해 두어 겹쳐도 자연스럽게 커지게 한다.
        private static void Apply(ArtifactData artifact)
        {
            float value1 = artifact.Value1;
            float value2 = artifact.Value2;
            switch (artifact.Effect)
            {
                case ArtifactEffect.MoveSpeedWhenBombsFull:
                    RunModifiers.MoveSpeedBonusWhenBombsFull += value1;
                    break;
                case ArtifactEffect.ExtraDodge:
                    RunModifiers.ExtraDodges += Mathf.RoundToInt(value1);
                    break;
                case ArtifactEffect.ExtraHealth:
                    RunModifiers.ExtraHealth += Mathf.RoundToInt(value1);
                    RunModifiers.SpawnIntervalMultiplier *= value2 > 0f ? value2 : 1f;
                    break;
                case ArtifactEffect.KillsToChargeReduction:
                    RunModifiers.KillsToChargeReduction += Mathf.RoundToInt(value1);
                    break;
                case ArtifactEffect.ExtraAbilityCharges:
                    RunModifiers.ExtraAbilityCharges += Mathf.RoundToInt(value1);
                    break;
                case ArtifactEffect.StillCharge:
                    RunModifiers.StillChargeDelay = value1;
                    RunModifiers.StillChargeInterval = value2;
                    break;
                case ArtifactEffect.DodgeKill:
                    RunModifiers.DodgeKillRadius = value1;
                    RunModifiers.DodgeCooldownMultiplier *= value2 > 0f ? value2 : 1f;
                    break;
                case ArtifactEffect.AbilityRefund:
                    RunModifiers.AbilityRefundMinKills = Mathf.RoundToInt(value1);
                    RunModifiers.AbilityRefundAmount += Mathf.RoundToInt(value2);
                    break;
                case ArtifactEffect.ThrowWhileDodging:
                    RunModifiers.CanThrowWhileDodging = true;
                    break;
                case ArtifactEffect.SpeedAfterAbility:
                    RunModifiers.MoveSpeedBonusAfterAbility += value1;
                    RunModifiers.MoveSpeedBonusAfterAbilityDuration = value2;
                    break;
            }
        }

        // 발사기가 Awake에서 수치를 준비한 뒤에 바꿔 끼우도록 Start에서 한다.
        private void Start()
        {
            WeaponData weapon = _catalog.FindUsableWeapon(RunSetup.WeaponId);
            if (weapon != null && weapon != _launcher.Weapon)
            {
                _launcher.Equip(weapon);
            }
        }
    }
}
