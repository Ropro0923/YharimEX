using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace YharimEX.Content.Projectiles
{
    public class YharimEXDeviEnergyHeart : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            //((ModProjectile)this).DisplayName.SetDefault("Energy Heart");
        }
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            CooldownSlot = 1;                 // still valid in 1.4.4
            Projectile.alpha = 150;
            Projectile.timeLeft = 90;
            Projectile.GetGlobalProjectile<FargoSoulsGlobalProjectile>().DeletionImmuneRank = 1;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(SoundID.Item44, Projectile.Center);
            }

            if (Projectile.alpha >= 60)
            {
                Projectile.alpha -= 10;
                if (Projectile.alpha < 60)
                    Projectile.alpha = 60;
            }

            Projectile.rotation = Projectile.ai[0];
            Projectile.scale += 0.01f;

            float speed = Projectile.velocity.Length();
            speed += Projectile.ai[1];

            if (speed < 0f) speed = 0f;

            if (Projectile.velocity != Vector2.Zero)
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * speed;
            // If velocity is zero, keep it zero to avoid NaNs.
        }

        public override void Kill(int timeLeft)
        {
            FargoSoulsUtil.HeartDust(Projectile.Center, Projectile.rotation + MathHelper.PiOver2);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int j = 0; j < 4; j++)
                {
                    Vector2 vel = Vector2.UnitX.RotatedBy(Projectile.rotation + MathHelper.PiOver2 * j);
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        vel,
                        ModContent.ProjectileType<DeviDeathray>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner
                    );
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            //target.AddBuff(ModContent.BuffType<Lovestruck>(), 240);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
