using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Core.Systems;
using YharimEX.Core;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls;

namespace YharimEX.Content.Projectiles
{

    public class YharimEXSkyFlare : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Boss/SkyFlare";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.hostile = true;
            Projectile.timeLeft = 360;
            CooldownSlot = 1;
        }

        public override bool CanHitPlayer(Player target) => target.hurtCooldowns[1] == 0;

        public override void AI()
        {
            int byIdentity = FargoSoulsUtil.GetProjectileByIdentity(Projectile.owner, (int)Projectile.ai[0], ModContent.ProjectileType<YharimEXSkyFlare>());
            if (byIdentity != -1 && Projectile.timeLeft > 295)
            {
                if (Main.projectile[byIdentity].ai[1] == 0f)
                {
                    Projectile.ai[0] = -1f;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                }
                else
                {
                    Projectile.velocity = Main.projectile[byIdentity].velocity;
                }
            }

            if (Projectile.alpha > 200)
                Projectile.alpha = 200;

            Projectile.alpha -= 5;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            Projectile.scale = 1f - Projectile.alpha / 255f;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame > 1)
                    Projectile.frame = 0;
            }
        }

        public override void OnHitPlayer(Player player, Player.HurtInfo hurtInfo)
        {
            if (WorldSavingSystem.EternityMode)
            {
                player.GetModPlayer<FargoSoulsPlayer>().MaxLifeReduction += 100;
                player.AddBuff(ModContent.BuffType<OceanicMaulBuff>(), 5400, true, false);
            }
            player.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 360, true, false);
            player.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180, true, false);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 255) * (1f - Projectile.alpha / 255f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glow = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Projectiles/GlowRing", AssetRequestMode.ImmediateLoad).Value;

            int rectHeight = glow.Height;
            int rectY = 0;
            Rectangle glowrectangle = new Rectangle(0, rectY, glow.Width, rectHeight);
            Vector2 gloworigin2 = glowrectangle.Size() / 2f;

            Color glowcolor = Color.Lerp(new Color(255, 56, 55, 0), Color.Transparent, 0.85f);
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = glowcolor;
                color27 *= (ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[Projectile.type];
                float scale = Projectile.scale * (ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[Projectile.type];

                Vector2 oldPos = Projectile.oldPos[i];
                Main.EntitySpriteDraw(
                    glow,
                    oldPos + Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                    glowrectangle,
                    color27,
                    Projectile.velocity.ToRotation() + MathHelper.PiOver2,
                    gloworigin2,
                    scale * 1.5f,
                    SpriteEffects.None,
                    0
                );
            }

            glowcolor = Color.Lerp(new Color(255, 255, 255, 0), Color.Transparent, 0.8f);
            Main.EntitySpriteDraw(
                glow,
                Projectile.position + Projectile.Size / 2f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                glowrectangle,
                glowcolor,
                Projectile.velocity.ToRotation() + MathHelper.PiOver2,
                gloworigin2,
                Projectile.scale * 1.5f,
                SpriteEffects.None,
                0
            );

            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int y = frameHeight * Projectile.frame;
            Rectangle rect = new Rectangle(0, y, texture.Width, frameHeight);
            Vector2 origin = rect.Size() / 2f;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                rect,
                Projectile.GetAlpha(lightColor),
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath6, Projectile.Center);

            // Expand hitbox around center for dust effect
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 208;
            Projectile.position.X -= Projectile.width / 2f;
            Projectile.position.Y -= Projectile.height / 2f;

            for (int i = 0; i < 3; i++)
            {
                int idx = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.5f);
                Main.dust[idx].position = new Vector2(Projectile.width / 2f, 0f)
                    .RotatedBy(MathHelper.TwoPi * Main.rand.NextDouble())
                    * (float)Main.rand.NextDouble()
                    + Projectile.Center;
            }

            for (int i = 0; i < 30; i++)
            {
                int d1 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.YellowTorch, 0f, 0f, 0, default, 2.5f);
                Main.dust[d1].position = new Vector2(Projectile.width / 2f, 0f)
                    .RotatedBy(MathHelper.TwoPi * Main.rand.NextDouble())
                    * (float)Main.rand.NextDouble()
                    + Projectile.Center;
                Main.dust[d1].noGravity = true;

                Main.dust[d1].velocity *= 1f;

                int d2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.YellowTorch, 0f, 0f, 100, default, 1.5f);
                Main.dust[d2].position = new Vector2(Projectile.width / 2f, 0f)
                    .RotatedBy(MathHelper.TwoPi * Main.rand.NextDouble())
                    * (float)Main.rand.NextDouble()
                    + Projectile.Center;
                Main.dust[d2].velocity *= 1f;
                Main.dust[d2].noGravity = true;
            }
        }
    }
}