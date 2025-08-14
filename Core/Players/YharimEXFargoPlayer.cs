using Terraria.ModLoader;
using FargowiltasSouls;
using YharimEX.Core.Systems;
using Terraria;

namespace YharimEX.Core.Players
{
    [ExtendsFromMod(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    [JITWhenModsEnabled(YharimEXCrossmodSystem.FargowiltasSouls.Name)]

    public class YharimEXFargoPlayer : ModPlayer
    {
    //    public bool YharimEXBetsyDashing;
        public override void ResetEffects()
        {
        //    YharimEXBetsyDashing = Main.player[Player.whoAmI].FargoSouls().BetsyDashing;
        }
    }
}