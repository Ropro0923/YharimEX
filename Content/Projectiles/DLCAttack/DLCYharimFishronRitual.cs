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

            Projectile.FargoSouls().GrazeCheck =
                projectile =>
                {
                    return CanDamage() == true && Math.Abs((Main.LocalPlayer.Center - Projectile.Center).Length() - safeRange) < Player.defaultHeight + Main.LocalPlayer.FargoSouls().GrazeRadius;
                };

            Projectile.FargoSouls().TimeFreezeImmune = true;
            Projectile.FargoSouls().DeletionImmuneRank = 2;
        }

        public override bool? CanDamage()
        {
            return Projectile.alpha == 0f && FargoSoulsUtil.NPCExists(Projectile.ai[0], ModContent.NPCType<MutantBoss>()).GetGlobalNPC<MutantDLC>().DLCAttackChoice == MutantDLC.DLCAttack.AresNuke;
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
            NPC npc = FargoSoulsUtil.NPCExists(Projectile.ai[0], ModContent.NPCType<MutantBoss>());
            MutantDLC mutantDLC = npc.GetGlobalNPC<MutantDLC>();
            if (npc != null && (mutantDLC.DLCAttackChoice == MutantDLC.DLCAttack.AresNuke || mutantDLC.DLCAttackChoice == MutantDLC.DLCAttack.PrepareAresNuke))
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

            if (Projectile.FargoSouls().GrazeCD > 10)
                Projectile.FargoSouls().GrazeCD = 10;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (YharimEXWorldFlags.DeathMode & !YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                target.YharimPlayer().MaxLifeReduction += 100;
            }
            else if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                EternityDebuffs.ManageOnHItDebuffs(target, 1);
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White;
        }

    }
}
