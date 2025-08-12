using Terraria.ModLoader;

namespace YharimEX.Core.Globals
{
    public class YharimEXGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public static int yharimEXBoss = -1;
    }
}