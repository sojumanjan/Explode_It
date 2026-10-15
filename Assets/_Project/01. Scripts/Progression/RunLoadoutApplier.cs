using ExplodeIt.Bombs;
using ExplodeIt.Core;
using UnityEngine;

namespace ExplodeIt.Progression
{
    // 메뉴에서 장착한 무기를 판이 시작될 때 실제로 낀다.
    // 잠겼거나 목록에 없는 무기를 가리키면 카탈로그가 기본 무기를 돌려주므로, 해금한 무기만 판에 들어온다.
    // 아티팩트는 효과를 구현할 때 여기서 함께 적용한다.
    public class RunLoadoutApplier : MonoBehaviour
    {
        [SerializeField] private UnlockCatalog _catalog;
        [SerializeField] private BombLauncher _launcher;

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
