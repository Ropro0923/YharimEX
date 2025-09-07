using Terraria;
using Terraria.ModLoader;
using FargowiltasSouls.Core.ModPlayers;
using YharimEX.Core.Systems;

namespace YharimEX.Core.Players
{
    [ExtendsFromMod(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    [JITWhenModsEnabled(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    public class YharimEXFargoPlayer : ModPlayer
    {
        public bool BetsyDashing = false;
        public override void ResetEffects()
        {
            var FargoSouls = Player.GetModPlayer<FargoSoulsPlayer>();
            BetsyDashing = FargoSouls.BetsyDashing;
        }
    }
}
