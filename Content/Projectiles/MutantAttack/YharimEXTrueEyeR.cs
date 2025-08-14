using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using YharimEX.Content.NPCs.Bosses;
using YharimEX.Content.Projectiles.FargoProjectile;
using YharimEX.Core.Globals;
using YharimEX.Core.Systems;

namespace YharimEX.Content.Projectiles
{
    public class YharimEXTrueEyeR : ModProjectile
    {
        public override string Texture => "CalamityMod/NPCs/SupremeCalamitas/SupremeCalamitasHooded";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 42;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 42;
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            CooldownSlot = 1;
            Projectile.penetrate = -1;
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                SetupFargoProjectile SetupFargoProjectile = Projectile.GetGlobalProjectile<SetupFargoProjectile>();
                SetupFargoProjectile.DeletiionImmuneRank = 1;
            }
        }

        public override bool? CanDamage()
        {
            return false;
        }

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
            Projectile.localAI[0]++;
            switch ((int)Projectile.ai[1])
            {
                case 0: //true eye movement code
                    Vector2 newVel = target.Center - Projectile.Center + new Vector2(300f * Projectile.localAI[1], -300f);
                    if (newVel != Vector2.Zero)
                    {
                        newVel.Normalize();
                        newVel *= 24f;
                        Projectile.velocity.X = (Projectile.velocity.X * 29 + newVel.X) / 30;
                        Projectile.velocity.Y = (Projectile.velocity.Y * 29 + newVel.Y) / 30;
                    }
                    if (Projectile.Distance(target.Center) < 150f)
                    {
                        if (Projectile.Center.X < target.Center.X)
                            Projectile.velocity.X -= 0.25f;
                        else
                            Projectile.velocity.X += 0.25f;

                        if (Projectile.Center.Y < target.Center.Y)
                            Projectile.velocity.Y -= 0.25f;
                        else
                            Projectile.velocity.Y += 0.25f;
                    }

                    if (Projectile.localAI[0] > 90f)
                    {
                        Projectile.localAI[0] = 0f;
                        Projectile.ai[1]++;
                        Projectile.netUpdate = true;
                    }

                    if (Projectile.rotation > 3.14159274101257)
                        Projectile.rotation = Projectile.rotation - 6.283185f;
                    Projectile.rotation = Projectile.rotation <= -0.005 || Projectile.rotation >= 0.005 ? Projectile.rotation * 0.96f : 0.0f;
                    break;

                case 1: //slow down
                    if (Projectile.localAI[0] == 1f) //spawn orb ring
                    {
                        const int max = 6;
                        const float distance = 100f;
                        const float rotation = 2f * (float)Math.PI / max;
                        for (int i = 0; i < max; i++)
                        {
                            Vector2 spawnPos = Projectile.Center - Vector2.UnitY * 6f + new Vector2(distance, 0f).RotatedBy(rotation * i);
                            if (YharimEXGlobalUtilities.HostCheck)
                                Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), spawnPos, Vector2.Zero, ModContent.ProjectileType<YharimEXTrueEyeSphere>(),
                                    Projectile.damage, 0f, Projectile.owner, Projectile.identity, i);
                        }
                    }
                    Projectile.velocity *= 0.95f;
                    if (Projectile.localAI[0] > 60f)
                    {
                        Projectile.velocity = Vector2.Zero;
                        Projectile.localAI[0] = 0f;
                        Projectile.ai[1]++;
                        Projectile.netUpdate = true;
                    }

                    if (Projectile.rotation > 3.14159274101257)
                        Projectile.rotation = Projectile.rotation - 6.283185f;
                    Projectile.rotation = Projectile.rotation <= -0.005 || Projectile.rotation >= 0.005 ? Projectile.rotation * 0.96f : 0.0f;
                    break;

                case 2: //ramming
                    if (Projectile.localAI[0] == 1f)
                    {
                        SoundEngine.PlaySound(SoundID.Zombie102, Projectile.Center);
                        Projectile.velocity = target.Center - Projectile.Center;
                        if (Projectile.velocity != Vector2.Zero)
                        {
                            Projectile.velocity.Normalize();
                            Projectile.velocity *= 24f;
                        }
                        Projectile.netUpdate = true;
                    }
                    else if (Projectile.localAI[0] > 10f)
                    {
                        Projectile.localAI[0] = 0f;
                        Projectile.ai[1]++;
                    }

                    float num3 = Projectile.velocity.ToRotation() + (float)Math.PI / 2;
                    if (Math.Abs(Projectile.rotation - num3) >= 3.14159274101257)
                        Projectile.rotation = num3 >= Projectile.rotation ? Projectile.rotation + 6.283185f : Projectile.rotation - 6.283185f;
                    float num4 = 12f;
                    Projectile.rotation = (Projectile.rotation * (num4 - 1f) + num3) / num4;
                    break;

                default:
                    for (int i = 0; i < 30; i++)
                    {
                        int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch, 0f, 0f, 0, default, 3f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 8f;
                    }
                    SoundEngine.PlaySound(SoundID.Zombie102, Projectile.Center);
                    Projectile.Kill();
                    break;
            }

            if (Projectile.rotation > 3.14159274101257)
                Projectile.rotation = Projectile.rotation - 6.283185f;
            Projectile.rotation = Projectile.rotation <= -0.005 || Projectile.rotation >= 0.005 ? Projectile.rotation * 0.96f : 0.0f;
            if (++Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 0;
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
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


        public override bool? CanCutTiles()
        {
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;

            int framesPerColumn = 21; // Number of frames stacked vertically in each column
            int totalColumns = 2;     // Change this to however many columns your spritesheet has
            int frameWidth = texture.Width / totalColumns;
            int frameHeight = texture.Height / framesPerColumn;

            // Flattened frame index from Projectile.frame
            int totalFrameIndex = Projectile.frame;

            // Figure out which column and row this frame is in
            int column = totalFrameIndex / framesPerColumn;
            int row = totalFrameIndex % framesPerColumn;

            Rectangle rectangle = new Rectangle(
                column * frameWidth,
                row * frameHeight,
                frameWidth,
                frameHeight
            );

            Vector2 origin2 = rectangle.Size() / 2f;
            Color color26 = Projectile.GetAlpha(lightColor);

            float scale = (Main.mouseTextColor / 200f - 0.35f) * 0.4f + 1f;
            scale *= Projectile.scale;

            // Draw the trail
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = color26 * 0.75f;
                color27.A = 0;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY),
                    rectangle, color27, num165, origin2, scale, SpriteEffects.None, 0);
            }

            // Draw the main projectile
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                rectangle, color26, Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

    }
}