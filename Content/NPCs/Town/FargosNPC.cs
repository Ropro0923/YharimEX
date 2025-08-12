using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fargowiltas.Common.Configs;
using Terraria.ModLoader;
using YharimEX.Core.Systems;

namespace YharimEX.Content.NPCs.Town
{
    [ExtendsFromMod(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    [JITWhenModsEnabled(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    public class FargosNPC
    {
        public static bool CanSetCatchable()
        {
            return FargoServerConfig.Instance.CatchNPCs;
        }
    }
}
