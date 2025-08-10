using Terraria.ModLoader;

namespace YharimEX.Core
{
    public class YharimEXGlobalPlayer : ModPlayer
    {
        public void YharimBaseHitEffect(int Timer)
        {
            if (Timer > 0)
            {
                Player.statDefense -= 20;
                Player.endurance -= 0.20f;
                Player.GetDamage(DamageClass.Generic) -= 0.2f;
                Player.GetCritChance(DamageClass.Generic) -= 20;
                Timer--;
            }
            if (Timer < 0)
            {
                Timer = 0;
            }
        }
    }
}