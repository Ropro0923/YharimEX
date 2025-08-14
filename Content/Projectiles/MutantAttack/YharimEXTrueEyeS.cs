using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using YharimEX.Content.NPCs.Bosses;
using YharimEX.Core.Globals;
using YharimEX.Core.Systems;
using YharimEX.Content.Projectiles.FargoProjectile;

namespace YharimEX.Content.Projectiles
{
    public class YharimEXTrueEyeS : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_650"; //i like the moon lord eyes better ngl
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
                SetupFargoProjectile.DeletionImmuneRank = 1;
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
                    Vector2 newVel = target.Center - Projectile.Center + new Vector2(-200f * Projectile.localAI[1], -200f);
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

                    if (Projectile.localAI[0] > 60f)
                    {
                        Projectile.localAI[0] = 0f;
                        Projectile.ai[1]++;
                        Projectile.netUpdate = true;
                    }
                    break;

                case 1: //slow down
                    Projectile.velocity *= 0.9f;
                    if (Projectile.velocity.Length() < 1f) //stop, FIRE LASER
                    {
                        Projectile.velocity = Vector2.Zero;
                        Projectile.localAI[0] = 0f;
                        Projectile.ai[1]++;
                        Projectile.netUpdate = true;
                    }
                    break;

                case 2: //shoot
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
                        Projectile.ai[1]++;
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

        private void ShootBolts(Player target)
        {
            Vector2 spawn = Projectile.Center - Vector2.UnitY * 6f;
            Vector2 vel = target.Center + target.velocity * 15f - spawn;
            if (vel != Vector2.Zero)
            {
                vel.Normalize();
                vel *= 8f;
                if (YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), spawn, vel, ProjectileID.PhantasmalBolt, Projectile.damage, 0f, Projectile.owner);
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
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

            int framesPerColumn = 21; // frames stacked vertically in each column
            int totalColumns = 2;     // number of columns in spritesheet
            int frameWidth = texture.Width / totalColumns;
            int frameHeight = texture.Height / framesPerColumn;

            // Flattened frame index
            int totalFrameIndex = Projectile.frame;

            // Determine column and row
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

            // Draw trail
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = color26 * 0.75f;
                color27.A = 0;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture,
                    value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY),
                    rectangle, color27, num165, origin2, scale, SpriteEffects.None, 0);
            }

            // Draw projectile
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                rectangle, color26, Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}