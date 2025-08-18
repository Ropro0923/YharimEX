using Terraria;
using Terraria.ModLoader;
using YharimEX.Content.NPCs.Bosses;
using YharimEX.Core.Globals;

namespace YharimEX.Content.Projectiles.MutantAttacks
{
    public class YharimEXMoonBlast : YharimEXSunBlast
    {
        public override string Texture => "Terraria/Images/Projectile_645";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Mod FargoSouls = YharimEXCrossmodSystem.FargowiltasSouls.Mod;
            target.AddBuff(FargoSouls.Find<ModBuff>("CurseoftheMoonBuff").Type, 360);
            if (YharimEXGlobalUtilities.BossIsAlive(ref YharimEXGlobalNPC.yharimEXBoss, ModContent.NPCType<YharimEXBoss>()))
            {
                target.YharimPlayer().MaxLifeReduction += 100;
                target.AddBuff(FargoSouls.Find<ModBuff>("OceanicMaulBuff").Type, 5400);
                target.AddBuff(FargoSouls.Find<ModBuff>("MutantFangBuff").Type, 180);
            }
        }
    }
}

