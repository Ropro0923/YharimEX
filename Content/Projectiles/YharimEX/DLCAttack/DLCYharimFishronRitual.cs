using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using YharimEX.Content.NPCs.Bosses;
using YharimEX.Core.Systems;
using YharimEX.Core.Globals;
/*
namespace YharimEX.Content.Projectiles.DLCAttack
{
    public class DLCYharimFishronRitual : ModProjectile
    {
        public override string Texture => "YharimEX/Assets/Projectiles/YharimEXFishronRitual";

        private const int safeRange = 150;

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Projectile.width = 320;
            Projectile.height = 320;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.alpha = 250;
            Projectile.penetrate = -1;
            CooldownSlot = -1;

            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                SetupFargoProjectile setupFargoProjectile = Projectile.GetGlobalProjectile<SetupFargoProjectile>();
                setupFargoProjectile.GrazeCheck = true;
                setupFargoProjectile.safeRange = safeRange;
                setupFargoProjectile.canDamage = CanDamage();
                setupFargoProjectile.TimeFreezeImmune = true;
                setupFargoProjectile.DeletionImmuneRank = 2;
            }
        }

        public override bool? CanDamage()
        {
            return Projectile.alpha == 0f && YharimEXGlobalUtilities.NPCExists(Projectile.ai[0], ModContent.NPCType<YharimEXBoss>()).GetGlobalNPC<CalDLCEmodeAttacks>().DLCAttackChoice == CalDLCEmodeAttacks.DLCAttack.AresNuke;
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
            CalDLCEmodeAttacks mutantDLC = npc.GetGlobalNPC<CalDLCEmodeAttacks>();
            if (npc != null && (mutantDLC.DLCAttackChoice == CalDLCEmodeAttacks.DLCAttack.AresNuke || mutantDLC.DLCAttackChoice == CalDLCEmodeAttacks.DLCAttack.PrepareAresNuke))
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

            SetupFargoProjectile setupFargoProjectile = Projectile.GetGlobalProjectile<SetupFargoProjectile>();

            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                if (setupFargoProjectile.GrazeCD > 10)
                {
                    setupFargoProjectile.GrazeCD = 10;
                }
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
                EternityDebuffs.ManageOnHitDebuffs(target, 1);
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White;
        }

    }
}
*/