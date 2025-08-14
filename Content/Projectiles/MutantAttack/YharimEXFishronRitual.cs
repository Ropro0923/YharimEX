﻿using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using YharimEX.Content.NPCs.Bosses;
using YharimEX.Core.Globals;
using YharimEX.Core.Systems;
using YharimEX.Content.Projectiles.FargoProjectile;

namespace YharimEX.Content.Projectiles
{
    public class YharimEXFishronRitual : ModProjectile
    {
        public override string Texture => "YharimEX/Assets/Projectiles/YharimEXFishronRitual";

        private const int safeRange = 150;

        public override void SetDefaults()
        {
            Projectile.width = 320;
            Projectile.height = 320;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            CooldownSlot = -1;

            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                SetupFargoProjectile SetupFargoProjectile = Projectile.GetGlobalProjectile<SetupFargoProjectile>();
                SetupFargoProjectile.DeletionImmuneRank = 2;
                SetupFargoProjectile.TimeFreezeImmune = true;
                SetupFargoProjectile.GrazeCheck = true;
            }
        }

        public override bool? CanDamage()
        {
            return Projectile.alpha == 0f && Main.player[Projectile.owner].ownedProjectileCounts[ModContent.ProjectileType<YharimEXBomb>()] > 0;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if ((projHitbox.Center.ToVector2() - targetHitbox.Center.ToVector2()).Length() < safeRange)
                return false;

            int clampedX = projHitbox.Center.X - targetHitbox.Center.X;
            int clampedY = projHitbox.Center.Y - targetHitbox.Center.Y;

            if (Math.Abs(clampedX) > targetHitbox.Width / 2)
                clampedX = targetHitbox.Width / 2 * Math.Sign(clampedX);
            if (Math.Abs(clampedY) > targetHitbox.Height / 2)
                clampedY = targetHitbox.Height / 2 * Math.Sign(clampedY);

            int dX = projHitbox.Center.X - targetHitbox.Center.X - clampedX;
            int dY = projHitbox.Center.Y - targetHitbox.Center.Y - clampedY;

            return Math.Sqrt(dX * dX + dY * dY) <= 1200;
        }

        public override void AI()
        {
            NPC npc = YharimEXGlobalUtilities.NPCExists(Projectile.ai[0], ModContent.NPCType<YharimEXBoss>());
            if (npc != null && npc.ai[0] == 34)
            {
                Projectile.alpha -= 7;
                Projectile.timeLeft = 300;
                Projectile.Center = npc.Center;
                Projectile.position.Y -= 100;
            }
            else
            {
                Projectile.alpha += 17;
            }

            if (Projectile.alpha < 0)
                Projectile.alpha = 0;
            if (Projectile.alpha > 255)
            {
                Projectile.alpha = 255;
                Projectile.Kill();
                return;
            }
            Projectile.scale = 1f - Projectile.alpha / 255f;
            Projectile.rotation += (float)Math.PI / 70f;
            Lighting.AddLight(Projectile.Center, 0.4f, 0.9f, 1.1f);

            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                SetupFargoProjectile.SetGrazeCD(Projectile);
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
        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White;
        }
    }
}