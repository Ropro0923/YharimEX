﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria;
using Terraria.ModLoader;
using YharimEX.Core.Systems;
using Microsoft.Xna.Framework;

namespace YharimEX.Content.Projectiles.DLCAttack
{
    public class YharimEXAureusSpawn : ModProjectile
    {
        public override string Texture => "CalamityMod/NPCs/AstrumAureus/AureusSpawn";
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.projFrames[Type] = Main.npcFrameCount[ModContent.NPCType<AureusSpawn>()];
        }
        public override void SetDefaults()
        {
            Projectile.width = 92;
            Projectile.height = 248 / 4;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.aiStyle = -1;
            Projectile.alpha = 255;
        }
        public override void AI()
        {
            #region Animation
            if (++Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= Main.projFrames[Type])
                {
                    Projectile.frame = 0;
                }
            }
            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 10;
            }
            else
            {
                Projectile.alpha = 0;
            }
            #endregion

            if (Projectile.ai[2] == 0)
            {
                Projectile.ai[2] = -Math.Sign(Projectile.velocity.X);
            }
            int playerIndex = (int)Projectile.ai[0];
            if (playerIndex.WithinBounds(Main.maxPlayers) && Projectile.ai[1] != 1)
            {
                Player player = Main.player[playerIndex];
                if (player != null && player.active && !player.dead)
                {
                    float difY = player.Center.Y - Projectile.Center.Y;

                    if (Math.Abs(difY) < 20)
                    {
                        Projectile.ai[1] = 1;
                    }

                    float maxSpeed = 10;
                    float modifier = 0.25f;
                    Projectile.velocity.Y += Math.Sign(difY) * modifier;
                    Utils.Clamp(Projectile.velocity.Y, -maxSpeed, maxSpeed);
                }
            }
            else
            {
                Projectile.velocity.Y *= 0.85f;
            }
            Projectile.velocity.X += Projectile.ai[2] * 0.4f;
            Utils.Clamp(Projectile.velocity.X, -15, 15);

            Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (YharimEXWorldFlags.EternityMode && YharimEXCrossmodSystem.FargowiltasSouls.Loaded) target.AddBuff(YharimEXCrossmodSystem.FargowiltasSouls.Mod.Find<ModBuff>("MutantFangBuff").Type, 180);
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 60 * 3);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = (SpriteEffects)0;
            if (Projectile.spriteDirection == 1)
            {
                spriteEffects = (SpriteEffects)1;
            }

            Color backglowColor = Color.Lerp(Color.Cyan, Color.Orange, (float)Math.Sin((double)Main.GlobalTimeWrappedHourly) / 2f + 0.5f);
            backglowColor.A = 0;
            Projectile.DrawBackglow(backglowColor, 2f + 8f * ((float)Math.Sin((double)(Main.GlobalTimeWrappedHourly * ((float)Math.PI * 2f))) + 1f));

            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Color drawColor = Color.White;

            int num156 = texture.Height / Main.projFrames[Type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle frame = new(0, y3, texture.Width, num156);

            Vector2 origin = frame.Size() / 2;
            Main.EntitySpriteDraw(texture, pos, frame, Projectile.GetAlpha(drawColor), Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);
            texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/AstrumAureus/AureusSpawnGlow", (AssetRequestMode)2).Value;
            Color glowColor = Color.Lerp(Color.White, Color.Orange, 0.5f) * Projectile.Opacity;
            Main.EntitySpriteDraw(texture, pos, frame, glowColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);
            return false;
        }
    }
}
