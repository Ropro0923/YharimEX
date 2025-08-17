using Terraria;
using Terraria.ModLoader;
using YharimEX.Core.Players;

namespace YharimEX.Core.Globals
{
    public class YharimEXGlobalItem : GlobalItem
    {
        public override bool CanUseItem(Item item, Player player)
        {
            YharimEXPlayer YharimEXPlayer = player.GetModPlayer<YharimEXPlayer>();

            if (YharimEXPlayer.NoUsingItems > 0)
                return false;
            return true;
        }
    }
}