/*

If you don't want it to be supreme calamitas, you can just summon MoonLordFreeEye

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Core.Systems;

namespace YharimEX.Content.Projectiles
{
    public class YharimEXSupremeCalamitasS : ModProjectile
    {
        private float localAI0;
        private float localAI1;
        public override string Texture => "Terraria/Images/Projectile_650"; //i really don't like the just having floating supcals so i'd rather keep the moonlord eyes.

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = 32;
            Projectile.height = 42;
            Projectile.timeLeft *= 5;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanDamage() => false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AI()
        {
            Player target = Main.player[(int)Projectile.ai[0]];
            Projectile.localAI[0] += 1f;

            switch ((int)Projectile.ai[1])
            {
                case 0:
                    {
                        Vector2 newVel = target.Center - Projectile.Center + new Vector2(-200f * Projectile.localAI[1], -200f);
                        if (newVel != Vector2.Zero)
                        {
                            newVel = Utils.SafeNormalize(newVel, Vector2.Zero) * 24f;
                            Projectile.velocity.X = (Projectile.velocity.X * 29f + newVel.X) / 30f;
                            Projectile.velocity.Y = (Projectile.velocity.Y * 29f + newVel.Y) / 30f;
                        }

                        if (Projectile.Distance(target.Center) < 150f)
                        {
                            Projectile.velocity.X += (Projectile.Center.X < target.Center.X) ? -0.25f : 0.25f;
                            Projectile.velocity.Y += (Projectile.Center.Y < target.Center.Y) ? -0.25f : 0.25f;
                        }

                        if (Projectile.localAI[0] > 60f)
                        {
                            Projectile.localAI[0] = 0f;
                            Projectile.ai[1] += 1f;
                            Projectile.netUpdate = true;
                        }
                        break;
                    }

                case 1:
                    {
                        Projectile.velocity *= 0.9f;
                        if (Projectile.velocity.LengthSquared() < 1f)
                        {
                            Projectile.velocity = Vector2.Zero;
                            Projectile.localAI[0] = 0f;
                            Projectile.ai[1] += 1f;
                            Projectile.netUpdate = true;
                        }
                        break;
                    }

                case 2:
                    if (Projectile.localAI[0] == 7f)
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath6, Projectile.Center);
                        ShootBolts(target);
                    }
                    else if (Projectile.localAI[0] == 14f)
                    {
                        ShootBolts(target);
                    }
                    else if (Projectile.localAI[0] > 21f)
                    {
                        Projectile.localAI[0] = 0f;
                        Projectile.ai[1] += 1f;
                    }
                    break;

                default:
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemRuby, 0f, 0f, 0, default, 3f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].noLight = true;
                            Main.dust[d].velocity *= 8f;
                        }
                        SoundEngine.PlaySound(SoundID.Zombie102, Projectile.Center);
                        Projectile.Kill();
                        break;
                    }
            }

            if (Projectile.rotation > MathHelper.Pi)
                Projectile.rotation -= MathHelper.TwoPi;

            Projectile.rotation = (Projectile.rotation <= -0.005f || Projectile.rotation >= 0.005f)
                ? Projectile.rotation * 0.96f
                : 0f;

            if (++Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 0;
            }

            if (Projectile.ai[1] != 2f)
                UpdatePupil();
        }

        private void ShootBolts(Player target)
        {
            Vector2 spawn = Projectile.Center - Vector2.UnitY * 6f;
            Vector2 vel = target.Center + target.velocity * 15f - spawn;

            if (vel != Vector2.Zero)
            {
                vel = Utils.SafeNormalize(vel, Vector2.Zero) * 8f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, vel, 462, Projectile.damage, 0f, Projectile.owner);
                    Main.projectile[p].GetGlobalProjectile<YharimEXGlobalProjectile>().PhantasmalBoltTintColor = new Color(255, 50, 50);

                }
            }
        }

        private void UpdatePupil()
        {
            float f1 = (float)(localAI0 % MathHelper.TwoPi - MathHelper.Pi);
            float num13 = (float)Math.IEEERemainder(localAI1, 1.0);
            if (num13 < 0f)
                num13 += 1f;

            float num14 = (float)Math.Floor(localAI1);
            float max = 0.999f;
            int num15 = 0;
            float amount = 0.1f;
            float num16 = Projectile.AngleTo(Main.player[(int)Projectile.ai[0]].Center);
            num15 = 2;

            float num18 = MathHelper.Clamp(num13 + 0.05f, 0f, max);
            float num19 = num14 + Math.Sign(-12f - num14);
            Vector2 rotationVector2 = num16.ToRotationVector2();

            localAI0 = Vector2.Lerp(f1.ToRotationVector2(), rotationVector2, amount).ToRotation() + num15 * MathHelper.TwoPi + MathHelper.Pi;
            localAI1 = num19 + num18;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 360, true, false);
            if (WorldSavingSystem.EternityMode)
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180, true, false);
        }

        public override bool? CanCutTiles() => false;

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        // ...existing code...
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int y = frameHeight * Projectile.frame;
            Rectangle frame = new Rectangle(0, y, texture.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;

            Color baseColor = Projectile.GetAlpha(lightColor);
            float scale = ((int)Main.mouseTextColor / 200f - 0.35f) * 0.4f + 1f;
            scale *= Projectile.scale;

            // Draw trail
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color trailColor = baseColor * 0.75f;
                trailColor.A = 0;
                trailColor *= (ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[Projectile.type];

                Vector2 oldPos = Projectile.oldPos[i] + Projectile.Size / 2f;
                float oldRot = Projectile.oldRot[i];

                Main.EntitySpriteDraw(
                    texture,
                    oldPos - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                    frame,
                    trailColor,
                    oldRot,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0
                );
            }

            // Draw main sprite
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                frame,
                baseColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            // Draw the pupil (TrueEyePupil)
            Texture2D pupilTexture = ModContent.Request<Texture2D>("YharimEX/Content/Projectiles/YharimEXSparklingLove", AssetRequestMode.ImmediateLoad).Value;            Vector2 pupilOffset = Utils.RotatedBy(new Vector2(localAI1 / 2f, 0f), localAI0) + Utils.RotatedBy(new Vector2(0f, -6f), Projectile.rotation);
            Vector2 pupilOrigin = pupilTexture.Size() / 2f;
            Main.EntitySpriteDraw(
                pupilTexture,
                Projectile.Center + pupilOffset - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                null,
                Color.White,
                Projectile.rotation,
                pupilOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}

*/