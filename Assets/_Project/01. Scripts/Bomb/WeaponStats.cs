using UnityEngine;

namespace ExplodeIt.Bombs
{
    // 실행 중 SO 에셋을 직접 바꾸면 에디터에서 값이 에셋에 남는다.
    // 개발자 패널, 상점, 아티팩트는 원본이 아닌 이 복사본을 수정한다.
    public class WeaponStats
    {
        public int Charges { get; set; }
        public float ThrowInterval { get; set; }
        public float RechargeTime { get; set; }
        public int BurstCount { get; set; }
        public float BurstInterval { get; set; }
        public float MaxThrowRange { get; set; }
        public float FuseDelay { get; set; }
        public float ExplosionRadius { get; set; }
        public float FlightDuration { get; set; }
        public float ArcHeight { get; set; }
        // 그림 값이라 개발자 패널에서 바꾸지 않지만, 폭탄이 수치와 함께 한 번에 받아 가도록 여기 둔다.
        public Sprite BombSprite { get; set; }
        public float BombSize { get; set; }

        public WeaponStats(WeaponData data)
        {
            CopyFrom(data);
        }

        // 무기와 상관없는 폭탄(스킬 폭탄)의 수치를 직접 채울 때 쓴다.
        public WeaponStats()
        {
        }

        // 개발자 패널에서 바꾼 값을 원본 데이터 값으로 되돌릴 때도 쓴다.
        public void CopyFrom(WeaponData data)
        {
            Charges = data.Charges;
            ThrowInterval = data.ThrowInterval;
            RechargeTime = data.RechargeTime;
            BurstCount = data.BurstCount;
            BurstInterval = data.BurstInterval;
            MaxThrowRange = data.MaxThrowRange;
            FuseDelay = data.FuseDelay;
            ExplosionRadius = data.ExplosionRadius;
            FlightDuration = data.FlightDuration;
            ArcHeight = data.ArcHeight;
            BombSprite = data.BombSprite;
            BombSize = data.BombSize;
        }
    }
}
