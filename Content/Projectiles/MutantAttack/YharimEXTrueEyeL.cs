using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
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
    public class YharimEXTrueEyeL : ModProjectile
    {
        public override string Texture => "CalamityMod/NPCs/SupremeCalamitas/SupremeCalamitasHooded";
        private float localAI0;
        private float localAI1;

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
            Projectile.hide = true;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
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

        private float localai1;

        public override void AI()
        {
            Player target = Main.player[(int)Projectile.ai[0]];
            Projectile.localAI[0]++;
            switch ((int)Projectile.ai[1])
            {
                case 0: //true eye movement code
                    Vector2 newVel = target.Center - Projectile.Center + new Vector2(0f, -300f);
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

                    if (Projectile.localAI[0] > 120f)
                    {
                        Projectile.localAI[0] = 0f;
                        Projectile.ai[1]++;
                        Projectile.netUpdate = true;
                    }
                    break;

                case 1: //slow down
                    Projectile.velocity *= 0.95f;
                    if (Projectile.velocity.Length() < 1f) //stop
                    {
                        Projectile.velocity = Vector2.Zero;
                        Projectile.localAI[0] = 0f;
                        Projectile.ai[1]++;
                        Projectile.netUpdate = true;
                    }
                    break;

                case 2: //firing laser
                    if (Projectile.localAI[0] == 1f)
                    {
                        const float PI = (float)Math.PI;
                        float rotationDirection = PI * 2f / 3f / 90f; //positive is CW, negative is CCW
                        if (Projectile.Center.X < target.Center.X)
                            rotationDirection *= -1;
                        localAI0 -= rotationDirection * 60f;
                        Vector2 speed = -Vector2.UnitX.RotatedBy(localAI0);
                        if (YharimEXGlobalUtilities.HostCheck)
                            Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center - Vector2.UnitY * 6f, speed, ModContent.ProjectileType<YharimEXTrueEyeDeathray>(),
                                Projectile.damage, 0f, Projectile.owner, rotationDirection, Projectile.whoAmI);
                        localai1 = rotationDirection;
                        Projectile.netUpdate = true;
                    }
                    else if (Projectile.localAI[0] > 90f)
                    {
                        Projectile.localAI[0] = 0f;
                        Projectile.ai[1]++;
                    }
                    else
                    {
                        localAI0 += localai1;
                    }
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

            int framesPerColumn = 21;
            int totalColumns = 2;
            int frameWidth = texture.Width / totalColumns;
            int frameHeight = texture.Height / framesPerColumn;
            int totalFrameIndex = Projectile.frame;
            int column = totalFrameIndex / framesPerColumn;
            int row = totalFrameIndex % framesPerColumn;

            Rectangle rectangle = new Rectangle(
                frameWidth * column,
                frameHeight * row,
                frameWidth,
                frameHeight
            );

            Vector2 origin2 = rectangle.Size() / 2f;
            Color color26 = Projectile.GetAlpha(
                Projectile.hide && Main.netMode == NetmodeID.MultiplayerClient
                    ? Lighting.GetColor((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16)
                    : lightColor
            );
            float scale = (Main.mouseTextColor / 200f - 0.35f) * 0.4f + 1f;
            scale *= Projectile.scale;
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = color26 * 0.75f;
                color27.A = 0;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), rectangle, color27, num165, origin2, scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), rectangle, color26, Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}