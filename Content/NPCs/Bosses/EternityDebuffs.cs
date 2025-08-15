using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls;
using Terraria;
using Terraria.ModLoader;
using YharimEX.Core.Systems;

namespace YharimEX.Content.NPCs.Bosses
{
    [ExtendsFromMod(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    [JITWhenModsEnabled(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    public class EternityDebuffs
    {
        public static void ManageOnHitDebuffs(Player target, int type = 0)
        {
            if (type == 1)
            {
                if (YharimEXWorldFlags.DeathMode || YharimEXWorldFlags.EternityMode)
                {
                    target.FargoSouls().MaxLifeReduction += 100;
                }
                if (YharimEXWorldFlags.EternityMode)
                {
                    //target.AddBuff(ModContent.BuffType<DefenselessBuff>(), 300);
                    target.AddBuff(ModContent.BuffType<OceanicMaulBuff>(), 5400);
                    target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
                }
                target.AddBuff(ModContent.BuffType<MutantNibbleBuff>(), 900);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 900);
            }
            else
            {
                if (YharimEXWorldFlags.DeathMode || YharimEXWorldFlags.EternityMode)
                {
                    target.FargoSouls().MaxLifeReduction += 100;
                }
                if (YharimEXWorldFlags.EternityMode)
                {
                    //target.AddBuff(ModContent.BuffType<DefenselessBuff>(), 300);
                    target.AddBuff(ModContent.BuffType<OceanicMaulBuff>(), 5400);
                    target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
                }
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 600);
            }
        }
        public static void ApplyHypothermia(Player target)
        {
            target.AddBuff(ModContent.BuffType<HypothermiaBuff>(), 900);
        }
    }
}