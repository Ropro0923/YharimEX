using System;
using FargowiltasSouls;
using Terraria;
using Terraria.ModLoader;
using YharimEX.Core.Systems;
using FargowiltasSouls.Core.ModPlayers;

namespace YharimEX.Core.Globals
{
    [ExtendsFromMod(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    [JITWhenModsEnabled(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    public class SetupFargoPlayer : ModPlayer
    {
        public bool BetsyDashing = false;
        public override void ResetEffects()
        {
            var FargoSouls = Player.GetModPlayer<FargoSoulsPlayer>();
            BetsyDashing = FargoSouls.BetsyDashing;
        }
    }
}
