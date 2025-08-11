using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.ID;

public class YharimEXGlobalProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public Color? PhantasmalBoltTintColor = null;

    public override Color? GetAlpha(Projectile projectile, Color lightColor)
    {
        if (PhantasmalBoltTintColor.HasValue)
            return PhantasmalBoltTintColor.Value * (1f - projectile.alpha / 255f);
        return null;
    }

    public override void AI(Projectile projectile)
    {
        if (PhantasmalBoltTintColor.HasValue)
        {

        }
    }
}

public class YharimEXSystem : ModSystem
{
    public override void PostUpdateDusts()
    {
        for (int i = 0; i < Main.maxDustToDraw; i++)
        {
            Dust dust = Main.dust[i];
            if (dust.active)
            {
                foreach (Projectile proj in Main.projectile)
                {
                    if (proj.active)
                    {
                        YharimEXGlobalProjectile YharimEXGlobalProjectile = proj.GetGlobalProjectile<YharimEXGlobalProjectile>();
                        if (YharimEXGlobalProjectile.PhantasmalBoltTintColor.HasValue && Vector2.Distance(dust.position, proj.Center) < 20f)
                        {
                            dust.color = YharimEXGlobalProjectile.PhantasmalBoltTintColor.Value;
                        }
                    }
                }
            }
        }
    }
}
