﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using YharimEX.Content.NPCs.Bosses;
using YharimEX.Core.Systems;
using YharimEX.Content.Projectiles.FargoProjectile;

namespace YharimEX.Content.Projectiles
{
    public class YharimEXSpearAim : ModProjectile
    {
        public override string Texture => "YharimEX/Assets/Projectiles/YharimEXSpear";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 0;
            Projectile.timeLeft = 60;
            CooldownSlot = 1;

                        if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                SetupFargoProjectile SetupFargoProjectile = Projectile.GetGlobalProjectile<SetupFargoProjectile>();
                SetupFargoProjectile.TimeFreezeImmune = true;
                SetupFargoProjectile.DeletiionImmuneRank = 2;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;

            float length = 200;
            float dummy = 0f;
            Vector2 offset = length / 2 * Projectile.scale * (Projectile.rotation - MathHelper.ToRadians(135f)).ToRotationVector2();
            Vector2 end = Projectile.Center - offset;
            Vector2 tip = Projectile.Center + offset;

            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), end, tip, 8f * Projectile.scale, ref dummy))
                return true;

            return false;
        }

        /* -1: direct, green, 3sec for rapid p2 toss
         * 0: direct, green, 1sec (?)
         * 1: direct, green, 0.5sec for rapid p2 toss
         * 2: predictice, blue, 1sec for p2 destroyer throw
         * 3: predictive, blue, 1.5sec for p1
         * 4: predictive, blue, 1.5sec with no more tracking after a bit for p2 slow dash finisher (maybe red?)
         * 5: predictive, NONE, 1sec for p2 slow dash
         */
        public ref float VisualRotation => ref Projectile.ai[2];
        public ref float MaxTime => ref Projectile.localAI[2];
        public override void AI()
        {
            //basically: ai1 > 1 means predictive and blue glow, otherwise direct aim and green glow

            NPC yharimEX = Main.npc[(int)Projectile.ai[0]];
            if (yharimEX.active && yharimEX.type == ModContent.NPCType<YharimEXBoss>())
            {
                Projectile.Center = yharimEX.Center;

                if (Projectile.localAI[0] == 0) //ensure it faces the right way on tick 1
                    Projectile.rotation = yharimEX.SafeDirectionTo(Main.player[yharimEX.target].Center).ToRotation();

                if (Projectile.ai[1] > 1)
                {
                    Projectile.rotation = yharimEX.SafeDirectionTo(Main.player[yharimEX.target].Center + Main.player[yharimEX.target].velocity * 30).ToRotation();
                }
                else
                {
                    if (!(Projectile.ai[1] == 4 && Projectile.timeLeft < System.Math.Abs(Projectile.localAI[1]) + 5))
                        Projectile.rotation = Projectile.rotation.AngleLerp(yharimEX.SafeDirectionTo(Main.player[yharimEX.target].Center).ToRotation(), 0.2f);
                }
            }
            else
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[0] == 0) //modifying timeleft for mp sync, localAI1 changed to adjust the rampup on the glow tell
            {
                Projectile.localAI[0] = 1;

                if (Projectile.ai[1] == -1) //extra long startup on p2 direct throw
                {
                    Projectile.timeLeft += 120;
                    Projectile.localAI[1] = -120;
                }
                else if (Projectile.ai[1] == 1) //p2 direct throw rapid fire
                {
                    Projectile.timeLeft -= 30;
                    Projectile.localAI[1] = 30;
                }
                else if (Projectile.ai[1] == 3) //p1 predictive throw
                {
                    Projectile.timeLeft += 30;
                    Projectile.localAI[1] = -30;
                }
                else if (Projectile.ai[1] == 4)
                {
                    Projectile.timeLeft += 20;
                    Projectile.localAI[1] = -20;
                }
                MaxTime = Projectile.timeLeft;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), target.Center + Main.rand.NextVector2Circular(100, 100), Vector2.Zero, ModContent.ProjectileType<YharimEXBombSmall>(), 0, 0f, Projectile.owner);
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                if (YharimEXWorldFlags.DeathMode & !YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                {
                    target.YharimPlayer().MaxLifeReduction += 100;
                }
                else if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                {
                    EternityDebuffs.ManageOnHitDebuffs(target);
                }
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin = rectangle.Size() / 2f;

            Color color26 = lightColor;
            color26 = Projectile.GetAlpha(color26);

            float rotationOffset = MathHelper.ToRadians(135f);

            // visual animation
            int direction = Projectile.rotation.ToRotationVector2().X.NonZeroSign();
            float timeFraction = Projectile.timeLeft / MaxTime;
            float maxRearback = MathHelper.PiOver2 * 0.35f;
            Vector2 maxOffset = Projectile.rotation.ToRotationVector2() * 30;
            float rotationAnimation;
            Vector2 positionOffset = Vector2.Zero;
            float windupFraction = 0.5f;
            float extensionFraction;
            //dont do it for predictive aim
            if (!YharimEXWorldFlags.MasochistModeReal && Projectile.ai[1] <= 1)
            {
                if (timeFraction > windupFraction) // rear back
                {
                    float rearbackFraction = (timeFraction - windupFraction) / (1 - windupFraction);
                    rotationAnimation = MathHelper.SmoothStep(-maxRearback, -maxRearback * 0.8f, rearbackFraction);
                    positionOffset = Vector2.SmoothStep(-maxOffset, -maxOffset * 0.8f, rearbackFraction);
                    extensionFraction = 0;
                }
                else
                {
                    float tossFraction = timeFraction / windupFraction;
                    tossFraction = MathF.Pow(tossFraction, 0.5f);
                    rotationAnimation = MathHelper.Lerp(0, -maxRearback, tossFraction);
                    positionOffset = Vector2.Lerp(Vector2.Zero, -maxOffset, tossFraction);
                    extensionFraction = MathHelper.Lerp(1, 0, tossFraction);
                }
                rotationOffset += direction * rotationAnimation;
                Vector2 extendMax = (Projectile.rotation + direction * rotationAnimation).ToRotationVector2() * origin.Length() * 0.3f;
                Vector2 extensionOffset = Vector2.Lerp(-extendMax, extendMax * 0.5f, extensionFraction);
                positionOffset += extensionOffset;

            }

            

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = color26;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i] + rotationOffset;
                Main.EntitySpriteDraw(texture2D13, value4 + positionOffset + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture2D13, Projectile.Center + positionOffset - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Projectile.GetAlpha(lightColor), Projectile.rotation + rotationOffset, origin, Projectile.scale, SpriteEffects.None, 0);

            if (Projectile.ai[1] != 5)
            {
                Texture2D glow = ModContent.Request<Texture2D>("YharimEX/Assets/Projectiles/YharimEXSpearAimGlow", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                float modifier = Projectile.timeLeft / (60f - Projectile.localAI[1]);
                Color glowColor = new Color(255, 191, 51, 210);
                if (Projectile.ai[1] > 1)
                    glowColor = new Color(255, 0, 0, 210);
                //if (Projectile.ai[1] == 4) glowColor = new Color(255, 0, 0, 210);
                glowColor *= 1f - modifier;
                float glowScale = Projectile.scale * 8f * modifier;
                Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glow.Bounds), glowColor, 0, glow.Bounds.Size() / 2, glowScale, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}