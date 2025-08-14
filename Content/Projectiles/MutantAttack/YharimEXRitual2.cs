﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using YharimEX.Content.NPCs.Bosses;
using YharimEX.Content.Projectiles.FargoProjectile;
using YharimEX.Core.Globals;
using YharimEX.Core.Systems;

namespace YharimEX.Content.Projectiles
{
    public class YharimEXRitual2 : ModProjectile
    {
        public override string Texture => "YharimEX/Assets/Projectiles/YharimEXSphere";

        private const float PI = (float)Math.PI;
        private const float rotationPerTick = PI / 50f;
        private const float threshold = 700;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Mutant Seal");
            base.SetStaticDefaults();
            Main.projFrames[Projectile.type] = 2;
            Terraria.ID.ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.scale *= 2f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                SetupFargoProjectile SetupFargoProjectile = Projectile.GetGlobalProjectile<SetupFargoProjectile>();
                SetupFargoProjectile.TimeFreezeImmune = true;
            }
        }

        public override void AI()
        {
            NPC npc = YharimEXGlobalUtilities.NPCExists(Projectile.ai[1], ModContent.NPCType<YharimEXBoss>());
            if (npc != null && npc.ai[0] == 10)
            {
                Projectile.alpha -= 17;
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;
                Projectile.Center = npc.Center;
            }
            else
            {
                if (npc != null)
                    Projectile.Center = npc.Center;

                Projectile.velocity = Vector2.Zero;
                Projectile.alpha += 9;
                if (Projectile.alpha > 255)
                {
                    Projectile.Kill();
                    return;
                }
            }

            Projectile.timeLeft = 2;
            Projectile.scale = 1f - Projectile.alpha / 255f;
            Projectile.ai[0] -= rotationPerTick;
            if (Projectile.ai[0] < PI)
            {
                Projectile.ai[0] += 2f * PI;
                Projectile.netUpdate = true;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame > 1)
                    Projectile.frame = 0;
            }
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = Projectile.GetAlpha(lightColor);
            Texture2D glow = ModContent.Request<Texture2D>("YharimEX/Assets/Projectiles/YharimEXSphereGlow", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            int rect1 = glow.Height;
            int rect2 = 0;
            Rectangle glowrectangle = new(0, rect2, glow.Width, rect1);
            Vector2 gloworigin2 = glowrectangle.Size() / 2f;
            Color glowcolor = Color.Lerp(Color.Red, Color.Transparent, 0.8f);

            for (int x = 0; x < 7; x++)
            {
                Vector2 drawOffset = new Vector2(threshold * Projectile.scale / 2f, 0f).RotatedBy(Projectile.ai[0]);
                drawOffset = drawOffset.RotatedBy(2f * PI / 7f * x);
                const int max = 4;
                for (int i = 0; i < max; i++)
                {
                    Color color27 = color26;
                    color27 *= (float)(max - i) / max;
                    Vector2 value4 = Projectile.Center + drawOffset.RotatedBy(rotationPerTick * i);
                    float num165 = Projectile.rotation;
                    Main.EntitySpriteDraw(texture2D13, value4 - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin2, Projectile.scale, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(glow, value4 - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle), glowcolor * ((float)(max - i) / max),
                        Projectile.rotation, gloworigin2, Projectile.scale * 1.4f, SpriteEffects.None, 0);
                }
                Main.EntitySpriteDraw(texture2D13, Projectile.Center + drawOffset - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26, Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(glow, Projectile.Center + drawOffset - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(glowrectangle), glowcolor,
                    Projectile.rotation, gloworigin2, Projectile.scale * 1.3f, SpriteEffects.None, 0);
            }
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White * Projectile.Opacity;
        }
    }
}