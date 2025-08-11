using FargowiltasSouls.Content.Bosses.DeviBoss;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using YharimEX.Core;

namespace YharimEX.Content.Projectiles
{
    public class YharimEXSparklingLove : ModProjectile
    {
        public int scaleCounter;

        public override void SetStaticDefaults()
        {
            // In 1.4.4, prefer localization files for display names.
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;
            Projectile.alpha = 250;
            Projectile.aiStyle = -1;
            Projectile.penetrate = -1;
            Projectile.GetGlobalProjectile<FargoSoulsGlobalProjectile>().DeletionImmuneRank = 2;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            NPC npc = Main.npc[(int)Projectile.ai[0]];
            Player plr = Main.LocalPlayer;

            if (npc == null || !npc.active)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.localAI[1] = Projectile.DirectionFrom(npc.Center).ToRotation();

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXGlowRingEX>(), 0, 0f, Main.myPlayer, -1f, -17f);
            }

            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 4;
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;
            }

            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 31f)
            {
                Projectile.localAI[0] = 1f;

                if (++scaleCounter < 3)
                {
                    Vector2 center = Projectile.Center;
                    Projectile.width *= 2;
                    Projectile.height *= 2;
                    Projectile.scale *= 2f;
                    Projectile.Center = center;

                    MakeDust();

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXGlowRingEX>(), 0, 0f, Main.myPlayer, -1f, -16 + scaleCounter);

                    SoundEngine.PlaySound(SoundID.Item92, Projectile.Center);
                }
            }

            // Orbit/offset around npc
            float angle = npc.ai[3] + Projectile.localAI[1];
            Vector2 offset = new Vector2(Projectile.ai[1], 0f).RotatedBy(angle);
            Projectile.Center = npc.Center + offset * Projectile.scale;

            if (Projectile.timeLeft == 8)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath6, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item92, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXGlowRingEX>(), 0, 0f, Main.myPlayer, -1f, -14f);

                if (!Main.dedServ && Main.LocalPlayer.active)
                    YharimEXUtil.ScreenshakeRumble(30);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float baseRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                    int max = 8;

                    void Burst(Vector2 center, float radius, bool halfStep)
                    {
                        for (int i = 0; i < max; i++)
                        {
                            float step = MathHelper.TwoPi / max * (i + (halfStep ? 0.5f : 0f)) + baseRotation;
                            Vector2 target = radius * Vector2.UnitX.RotatedBy(step);
                            Vector2 speed = 2f * target / 90f;
                            float accel = -speed.Length() / 90f;
                            float rot = speed.ToRotation();

                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, speed, ModContent.ProjectileType<YharimEXDeviEnergyHeart>(), (int)(Projectile.damage * 0.75), 0f, Main.myPlayer, rot + MathHelper.PiOver2, accel);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero, ModContent.ProjectileType<GlowLine>(), Projectile.damage, 0f, Main.myPlayer, 2f, rot);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, speed, ModContent.ProjectileType<GlowLine>(), Projectile.damage, 0f, Main.myPlayer, 2f, rot + MathHelper.PiOver2);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, speed, ModContent.ProjectileType<GlowLine>(), Projectile.damage, 0f, Main.myPlayer, 2f, rot - MathHelper.PiOver2);
                        }
                    }

                    // Three rings (center + two rotated toward player vector)
                    Burst(Projectile.Center, 600f, false);
                    Burst(Projectile.Center + (plr.Center - Projectile.Center).RotatedBy(2.09433341f), 600f, false);
                    Burst(Projectile.Center + (plr.Center - Projectile.Center).RotatedBy(4.18866682f), 600f, false);
                    Burst(Projectile.Center, 300f, true);
                    Burst(Projectile.Center + (plr.Center - Projectile.Center).RotatedBy(2.09433341f), 300f, true);
                    Burst(Projectile.Center + (plr.Center - Projectile.Center).RotatedBy(4.18866682f), 300f, true);
                }
            }

            Projectile.spriteDirection = npc.direction;
            Projectile.direction = Projectile.spriteDirection;

            Projectile.rotation = npc.ai[3] + Projectile.localAI[1] + MathHelper.PiOver2 + MathHelper.PiOver4;
            if (Projectile.spriteDirection >= 0)
                Projectile.rotation -= MathHelper.PiOver2;
        }

        public override void Kill(int timeLeft) => MakeDust();

        private void MakeDust()
        {
            // Build a clamped start vector along the rotated width axis
            Vector2 start = (Projectile.width * Vector2.UnitX).RotatedBy(Projectile.rotation - MathHelper.PiOver4);
            start.X = MathHelper.Clamp(start.X, -Projectile.width / 2f, Projectile.width / 2f);
            start.Y = MathHelper.Clamp(start.Y, -Projectile.height / 2f, Projectile.height / 2f);

            int length = (int)start.Length();
            start.Normalize();

            float scaleMod = scaleCounter / 3f + 0.5f;

            for (int j = -length; j <= length; j += 80)
            {
                Vector2 dustPoint = Projectile.Center + start * j;
                dustPoint -= new Vector2(23f);

                for (int i = 0; i < 15; i++)
                {
                    int idx1 = Dust.NewDust(dustPoint, 46, 46, DustID.GemAmethyst, 0f, 0f, 0, default, scaleMod * 2.5f);
                    Main.dust[idx1].noGravity = true;
                    Main.dust[idx1].velocity *= 16f * scaleMod;

                    int idx2 = Dust.NewDust(dustPoint, 46, 46, DustID.GemAmethyst, 0f, 0f, 0, default, scaleMod);
                    Main.dust[idx2].noGravity = true;
                    Main.dust[idx2].velocity *= 8f * scaleMod;
                }

                for (int i = 0; i < 5; i++)
                {
                    int d = Dust.NewDust(dustPoint, 46, 46, DustID.GemAmethyst, 0f, 0f, 0, default, Main.rand.NextFloat(1f, 2f) * scaleMod);
                    Main.dust[d].velocity *= Main.rand.NextFloat(1f, 4f) * scaleMod;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = tex.Height / Math.Max(1, Main.projFrames[Projectile.type]);
            Rectangle frame = new(0, frameHeight * Projectile.frame, tex.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;
            var effects = Projectile.spriteDirection <= 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Base + glow
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Main.EntitySpriteDraw(tex, drawPos, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);

            Texture2D glow = ModContent.Request<Texture2D>("YharimEX/Content/Projectiles/YharimEXSparklingLove_glow", AssetRequestMode.ImmediateLoad).Value;
            Main.EntitySpriteDraw(glow, drawPos, frame, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, effects, 0);

            return false;
        }
    }
}
