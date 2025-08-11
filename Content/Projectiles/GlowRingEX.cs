using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace YharimEX.Content.Projectiles
{
    public class GlowRingEX : ModProjectile
    {
        private Color ringColor = new(255, 255, 255, 0);

        public override void SetStaticDefaults()
        {
            // Use localization files for name in 1.4.4.((ModProjectile)this).DisplayName.SetDefault("Glow Ring");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.alpha = 0;

            var g = Projectile.GetGlobalProjectile<FargoSoulsGlobalProjectile>();
            g.TimeFreezeImmune = true;
            g.DeletionImmuneRank = 2;
        }

        public override void AI()
        {
            // Follow owner NPC if present
            NPC npc = FargoSoulsUtil.NPCExists(Projectile.ai[0]);
            if (npc != null)
                Projectile.Center = npc.Center;

            float baseScale = 12f;
            int maxTime = 30;
            bool customScaleAlpha = false;

            switch ((int)Projectile.ai[1])
            {
                case -23:
                    {
                        customScaleAlpha = true;
                        maxTime = 90;
                        float t = Projectile.localAI[0] / maxTime;
                        ringColor = new Color(51, 255, 191) * t;
                        Projectile.alpha = (int)(255f * (1f - t));
                        Projectile.scale = 27f * (1f - t);
                        break;
                    }

                case -21:
                    baseScale = 4f;
                    maxTime = 60;
                    break;

                case -20:
                    {
                        customScaleAlpha = true;
                        maxTime = 200;
                        float t = Projectile.localAI[0] / maxTime;
                        ringColor = new Color(51, 255, 191) * t;
                        Projectile.alpha = (int)(255f * (1f - t));
                        Projectile.scale = 18f * (1f - t);
                        break;
                    }

                case -19:
                    ringColor = Color.Yellow with { A = 0 };
                    baseScale = 18f;
                    break;

                case -18:
                    baseScale = 36f;
                    maxTime = 120;
                    break;

                case -17:
                    baseScale = 6f;
                    goto case -16;

                case -16:
                    ringColor = new Color(255, 51, 153, 0);
                    break;

                case -15:
                    baseScale = 18f;
                    goto case -16;

                case -14:
                    baseScale = 24f;
                    goto case -16;

                case -13:
                    ringColor = new Color(93, 255, 241, 0);
                    baseScale = 6f;
                    maxTime = 15;
                    break;

                case -12:
                    ringColor = new Color(0, 0, 255, 0);
                    maxTime = 45;
                    break;

                case -11:
                    ringColor = new Color(0, 255, 0, 0);
                    maxTime = 45;
                    break;

                case -10:
                    ringColor = new Color(0, 255, 255, 0);
                    maxTime = 45;
                    break;

                case -9:
                    ringColor = new Color(255, 255, 0, 0);
                    maxTime = 45;
                    break;

                case -8:
                    ringColor = new Color(255, 127, 40, 0);
                    maxTime = 45;
                    break;

                case -7:
                    ringColor = new Color(255, 0, 0, 0);
                    maxTime = 45;
                    break;

                case -6:
                    ringColor = new Color(255, 255, 0, 0);
                    baseScale = 18f;
                    break;

                case -5:
                    ringColor = new Color(200, 0, 255, 0);
                    baseScale = 18f;
                    break;

                case -4:
                    ringColor = new Color(255, 255, 0, 0);
                    baseScale = 18f;
                    maxTime = 60;
                    break;

                case -3:
                    ringColor = new Color(255, 100, 0, 0);
                    baseScale = 18f;
                    maxTime = 60;
                    break;

                case -2:
                    ringColor = new Color(51, 255, 191, 0);
                    baseScale = 18f;
                    break;

                case -1:
                    ringColor = new Color(200, 0, 200, 0);
                    maxTime = 60;
                    break;

                case 4:
                    ringColor = new Color(51, 255, 191, 0);
                    maxTime = 45;
                    break;

                case 222:
                    ringColor = new Color(255, 255, 100, 0);
                    maxTime = 45;
                    break;

                case 114:
                    ringColor = new Color(93, 255, 241, 0);
                    baseScale = 12f;
                    maxTime = 30;
                    break;

                case 125:
                    ringColor = new Color(255, 0, 0, 0);
                    baseScale = 24f;
                    maxTime = 60;
                    break;

                case 128:
                case 129:
                case 130:
                case 131:
                    ringColor = new Color(51, 255, 191, 0);
                    baseScale = 12f;
                    maxTime = 30;
                    break;

                case 657:
                    {
                        ringColor = Color.HotPink with { A = 200 };
                        baseScale = 6f;
                        maxTime = 60;

                        if (Projectile.localAI[0] > maxTime * 0.25f && NPC.AnyNPCs(ModContent.NPCType<GelatinSubject>()))
                            Projectile.localAI[0] = maxTime * 0.25f;

                        if (npc != null)
                            Projectile.Center = npc.Bottom + (npc.height / 2f) * -Vector2.UnitY.RotatedBy(npc.rotation);

                        break;
                    }

                case 439:
                    ringColor = new Color(255, 127, 40, 0);
                    break;

                case 396:
                case 397:
                case 398:
                    ringColor = new Color(51, 255, 191, 0);
                    baseScale = 12f;
                    maxTime = 60;
                    break;

                case 668:
                    {
                        ringColor = Color.LightSkyBlue with { A = 0 };
                        baseScale = 9f;
                        maxTime = 30;

                        if (npc != null)
                            Projectile.Center = npc.direction < 0 ? npc.TopLeft : npc.TopRight;

                        break;
                    }

                default:
                    Main.NewText("Error in YharimEX GlowRing.cs", 255, 255, 255);
                    break;
            }

            // Lifetime & scaling/alpha
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > maxTime)
            {
                Projectile.Kill();
                return;
            }

            if (!customScaleAlpha)
            {
                float t = Projectile.localAI[0] / maxTime;
                Projectile.scale = baseScale * (float)System.Math.Sin(System.Math.PI * 0.5 * t);
                Projectile.alpha = (int)(255f * t);
            }

            Projectile.alpha = Utils.Clamp(Projectile.alpha, 0, 255);
        }

        public override Color? GetAlpha(Color lightColor) => ringColor * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            bool specialShader = Projectile.ai[1] == 657f;

            if (specialShader)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                                       DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                GameShaders.Misc["HallowBoss"].Apply(null);
            }

            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = tex.Height / Math.Max(1, Main.projFrames[Projectile.type]);
            Rectangle frame = new(0, frameHeight * Projectile.frame, tex.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                frame,
                Projectile.GetAlpha(lightColor),
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            if (specialShader)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                                       DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);
            }

            return false;
        }
    }
}
