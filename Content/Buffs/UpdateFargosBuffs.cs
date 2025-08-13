using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FargowiltasSouls;
using Terraria;
using Terraria.ModLoader;
using YharimEX.Core.Systems;

namespace YharimEX.Content.Buffs
{
    [ExtendsFromMod(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    [JITWhenModsEnabled(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    public partial class UpdateFargosBuffs
    {
        public static void UpdateFargosEffects(Player player, string type, bool boolData = false, float floatData = 0f)
        {
            switch (type)
            {
                case "noSupersonic":
                    player.FargoSouls().noSupersonic = boolData;
                    return;
                case "GrazeRadius":
                    player.FargoSouls().GrazeRadius *= floatData;
                    return;
                case "MutantPresence":
                    player.FargoSouls().MutantPresence = boolData;
                    return;
                default:
                    return;
            }
        }
    }
}
