using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria;
using Terraria.ID;

namespace YharimEX.Content.Projectiles
{
    public class YharimEXYharimEXAbomDeathrayMark : BaseDeathray
    {
        public bool DontS;

        public override string Texture => "YharimEX/Content/Projectiles/YharimEXAbomDeathray";

        public YharimEXYharimEXAbomDeathrayMark() : base(30f) { }

        public override void SetStaticDefaults()
        {
            // Use localization files for display name in 1.4.4
            // DisplayName.SetDefault("Deathray");
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            // Ensure a valid velocity
            if (Utils.HasNaNs(Projectile.velocity) || Projectile.velocity == Vector2.Zero)
                Projectile.velocity = -Vector2.UnitY;

            const float maxScale = 0.3f;

            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] >= maxTime)
            {
                Projectile.Kill();
                return;
            }

            // Scale follows a sine-in/out over lifetime
            Projectile.scale = (float)Math.Sin(Projectile.localAI[0] * Math.PI / maxTime) * 0.6f * maxScale;
            if (Projectile.scale > maxScale)
                Projectile.scale = maxScale;

            // Simple “scan” distance smoothing (constant here)
            const int samples = 3;
            float avgDistance = 3000f; // since all samples are 3000 in original
            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], avgDistance, 0.5f);

            Vector2 beamPos = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 14f);

            // Side dusts
            for (int i = 0; i < 2; i++)
            {
                float rot = Projectile.velocity.ToRotation() + (Main.rand.NextBool() ? -1f : 1f) * MathHelper.PiOver2;
                float spd = Main.rand.NextFloat(2f, 4f);
                Vector2 dustVel = new Vector2((float)Math.Cos(rot) * spd, (float)Math.Sin(rot) * spd);

                int idx = Dust.NewDust(beamPos, 0, 0, DustID.CopperCoin, dustVel.X, dustVel.Y);
                Main.dust[idx].noGravity = true;
                Main.dust[idx].scale = 1.7f;
            }

            // Random side puff
            if (Main.rand.NextBool(5))
            {
                Vector2 offset = Projectile.velocity.RotatedBy(MathHelper.PiOver2) *
                                 ((float)Main.rand.NextDouble() - 0.5f) * Projectile.width;

                int idx = Dust.NewDust(beamPos + offset - Vector2.One * 4f, 8, 8, DustID.CopperCoin, 0f, 0f, 100, default, 1.5f);
                Main.dust[idx].velocity *= 0.5f;
                Main.dust[idx].velocity.Y = -Math.Abs(Main.dust[idx].velocity.Y);
            }

            // Move back so the beam renders from origin forward (common deathray trick)
            Projectile.position -= Projectile.velocity;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override void Kill(int timeLeft)
        {
            // Spawn the main deathray, forwarding flags
            var proj = Projectile.NewProjectileDirect(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Projectile.velocity,
                ModContent.ProjectileType<YharimEXAbomDeathray>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                Projectile.ai[0],
                Projectile.ai[1]
            );

            if (proj.ModProjectile is YharimEXAbomDeathray ray)
                ray.DontSpawn = DontS;
        }
    }
}
