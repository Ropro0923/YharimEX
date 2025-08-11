using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FargowiltasSouls.Content.Bosses.AbomBoss;
using FargowiltasSouls.Content.Items.BossBags;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.GameContent;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace YharimEX.Content.Projectiles
{
    public class AbomScytheSplit : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_274";

        public override void SetStaticDefaults()
        {
            // Use localization file for display name in 1.4.4 ((ModProjectile)this).DisplayName.SetDefault("Abominationn Scythe");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 900;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            CooldownSlot = 1;
            Projectile.scale = 2f;
        }

        public override void AI()
        {
            Projectile.rotation += 1f;
            if ((Projectile.ai[0] -= 1f) <= -300f)
                Projectile.Kill();
        }

        public override void Kill(int timeLeft)
        {
            int dustMax = Projectile.ai[1] >= 0f ? 50 : 25;
            float speed = Projectile.ai[1] >= 0f ? 15f : 6f;

            for (int i = 0; i < dustMax; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 0, default, 3.5f);
                Main.dust[d].velocity *= speed;
                Main.dust[d].noGravity = true;
            }

            if (Projectile.ai[1] < 0f || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int p = Player.FindClosest(Projectile.Center, 0, 0);
            if (p != -1)
            {
                Vector2 vel = Projectile.ai[1] == 0f
                    ? Vector2.Normalize(Projectile.velocity)
                    : Projectile.DirectionTo(Main.player[p].Center);

                vel *= 30f;

                int max = Projectile.ai[1] == 0f ? 6 : 10;

                for (int j = 0; j < max; j++)
                {
                    Vector2 shot = vel.RotatedBy(MathHelper.TwoPi / max * j);
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        shot,
                        ModContent.ProjectileType<AbomSickle3>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        p,
                        0f
                    );
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            //target.AddBuff(ModContent.BuffType<AbomFang>(), 300);
            //target.AddBuff(ModContent.BuffType<Berserked>(), 120);

            target.AddBuff(BuffID.Bleeding, 600);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = tex.Height / Math.Max(1, Main.projFrames[Projectile.type]);
            Rectangle frame = new(0, frameHeight * Projectile.frame, tex.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;

            // trail
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                float t = (ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) /
                          (float)ProjectileID.Sets.TrailCacheLength[Projectile.type];

                Color c = Projectile.GetAlpha(lightColor) * t;

                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                float rot = Projectile.oldRot[i];

                Main.EntitySpriteDraw(tex, pos, frame, c, rot, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            // current
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Main.EntitySpriteDraw(tex, drawPos, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
