using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using YharimEX.Core.Systems;

namespace YharimEX.Content.Buffs
{
    public class TyrantPresenceBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            Terraria.ID.BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.YharimPlayer().noDodge = true;

            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                UpdateFargosBuffs.UpdateFargosEffects(player, "noSupersonic", true);
                UpdateFargosBuffs.UpdateFargosEffects(player, "GrazeRadious", floatData: 0.5f);
                UpdateFargosBuffs.UpdateFargosEffects(player, "MutantPresence", true);
            }
            else
            {
                player.YharimPlayer().YharimPresence = true; //we don't want to have both mutant and yharim... that would be cooked.
            }

            player.moonLeech = true;
        }
    }
}
