using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using YharimEX.Content.NPCs.Bosses;
using YharimEX.Content.Projectiles.FargoProjectile;
using YharimEX.Core.Globals;
using YharimEX.Core.Systems;

namespace YharimEX.Content.Projectiles.MutantAttack
{
    public class YharimEXGlowLine : ModProjectile
    {
        public override string Texture => "YharimEX/Assets/Projectiles/YharimEXGlowLine";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.alpha = 255;

            Projectile.hide = true;
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                SetupFargoProjectile SetupFargoProjectile = Projectile.GetGlobalProjectile<SetupFargoProjectile>();
                SetupFargoProjectile.DeletionImmuneRank = 2;
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        public Color color = Color.White;

        public override bool? CanDamage()
        {
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(counter);
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            counter = reader.ReadInt32();
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }

        private int counter;
        private int drawLayers = 1;

        public override void AI()
        {
            int maxTime = 60;
            float alphaModifier = 3;

            switch ((int)Projectile.ai[0])
            {
                case 0: //abom flaming scythe telegraph, sticks to abom and follows his line of sight to player w/ offset
                    {
                        color = Color.Orange;
                        maxTime = 30;
                        alphaModifier = 10;

                        NPC abom = YharimEXGlobalUtilities.NPCExists(Projectile.localAI[1], ModContent.NPCType<YharimEXBoss>());
                        if (abom != null)
                        {
                            Projectile.Center = abom.Center;
                            Projectile.rotation = abom.SafeDirectionTo(Main.player[abom.target].Center).ToRotation() + Projectile.ai[1];
                        }
                    }
                    break;

                case 1: //mutant final spark tell
                    {
                        color = Color.Orange;
                        maxTime = 90;
                        alphaModifier = counter > maxTime / 2 ? 6 : 3;
                        Projectile.scale = 4f;

                        NPC mutant = YharimEXGlobalUtilities.NPCExists(Projectile.ai[1], ModContent.NPCType<YharimEXBoss>());
                        if (mutant != null)
                        {
                            float targetRot = MathHelper.WrapAngle(mutant.ai[3]);
                            Projectile.velocity = Projectile.velocity.ToRotation().AngleLerp(targetRot, 0.12f * (float)Math.Pow((float)counter / maxTime, 3f)).ToRotationVector2();
                        }

                        Projectile.position -= Projectile.velocity;
                        Projectile.rotation = Projectile.velocity.ToRotation();
                    }
                    break;

                default:
                    Main.NewText("glow line: you shouldnt be seeing this text, show terry");
                    break;
            }

            if (++counter > maxTime)
            {
                Projectile.Kill();
                return;
            }

            if (alphaModifier >= 0)
            {
                Projectile.alpha = 255 - (int)(255 * Math.Sin(Math.PI / maxTime * counter) * alphaModifier);
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;
            }

            color.A = 0;
        }

        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
            {
                return true;
            }
            float num6 = 0f;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * 3000f, 16f * Projectile.scale, ref num6))
            {
                return true;
            }
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return color * Projectile.Opacity * (Main.mouseTextColor / 255f) * 0.9f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //Main.spriteBatch.End(); Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = texture2D13.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            const int length = 3000;
            Vector2 offset = Projectile.rotation.ToRotationVector2() * length / 2f;
            Vector2 position = Projectile.Center - Main.screenLastPosition + new Vector2(0f, Projectile.gfxOffY) + offset;
            const float resolutionCompensation = 128f / 24f; //i made the image higher res, this compensates to keep original display size
            Rectangle destination = new((int)position.X, (int)position.Y, length, (int)(rectangle.Height * Projectile.scale / resolutionCompensation));

            Color drawColor = Projectile.GetAlpha(lightColor);

            for (int j = 0; j < drawLayers; j++)
                Main.EntitySpriteDraw(new DrawData(texture2D13, destination, new Rectangle?(rectangle), drawColor, Projectile.rotation, origin2, SpriteEffects.None, 0));

            //Main.spriteBatch.End(); Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);
            return false;
        }
    }
}