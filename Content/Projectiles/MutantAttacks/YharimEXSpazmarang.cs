using YharimEX.Core.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace YharimEX.Content.Projectiles.MutantAttacks
{
    public class YharimEXSpazmarang : YharimEXRetirang
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/TriactisTruePaladinianMageHammerofMightMelee";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Spazmarang");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Mod FargoSouls = YharimEXCrossmodSystem.FargowiltasSouls.Mod;
            target.AddBuff(BuffID.CursedInferno, 120);
            if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
                target.AddBuff(FargoSouls.Find<ModBuff>("MutantFangBuff").Type, 180);
        }
    }
}