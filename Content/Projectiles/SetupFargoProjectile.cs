using System;
using FargowiltasSouls;
using Terraria;
using Terraria.ModLoader;

namespace YharimEX.Content.Projectiles
{
    [ExtendsFromMod(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    [JITWhenModsEnabled(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    public class SetupFargoProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool TimeFreezeImmune = false;
        public int DeletionImmuneRank = -1;
        public bool GrazeCheck = false;
        public bool GrazeCheck2 = false;
        public bool GrazeCheck3 = false;
        public bool GrazeCheck4 = false;
        public bool GrazeCheck5 = false;
        public float GrazeTargetPlayer;
        public float GrazeThreshold;
        public int safeRange = 0;
        public bool? canDamage = false;
        public bool canSplit = false;
        public int? GrazeCD;
        public bool noInteractionWithNPCImmunityFrames = false;

        public override void AI(Projectile entity)
        {
            if (TimeFreezeImmune) entity.FargoSouls().TimeFreezeImmune = true;
            if (DeletionImmuneRank != -1) entity.FargoSouls().DeletionImmuneRank = DeletionImmuneRank;
            if (GrazeCheck)
            {
                entity.FargoSouls().GrazeCheck =
                    projectile =>
                    {
                        return canDamage == true && Math.Abs((Main.LocalPlayer.Center - entity.Center).Length() - safeRange) < Player.defaultHeight + Main.LocalPlayer.FargoSouls().GrazeRadius;
                    };
            }
            if (GrazeCheck2)
            {
                entity.FargoSouls().GrazeCheck = projectile => { return false; };
            }
            if (GrazeCheck3)
            {
                entity.FargoSouls().GrazeCheck =
                    Projectile =>
                        {
                            float num6 = 0f;
                            if (canDamage != false && Collision.CheckAABBvLineCollision(Main.LocalPlayer.Hitbox.TopLeft(), Main.LocalPlayer.Hitbox.Size(), Projectile.Center,
                            Projectile.Center + Projectile.velocity * Projectile.localAI[1], 22f * Projectile.scale + Main.LocalPlayer.FargoSouls().GrazeRadius * 2f + Player.defaultHeight, ref num6))
                            {
                                return true;
                            }
                            return false;
                        };
            }
            if (GrazeCheck4)
            {
                entity.FargoSouls().GrazeCheck =
                projectile =>
                {
                    return canDamage == true && Math.Abs((Main.LocalPlayer.Center - entity.Center).Length() - safeRange) < Player.defaultHeight + Main.LocalPlayer.FargoSouls().GrazeRadius;
                };
            }
            if (GrazeCheck5)
            {
                entity.FargoSouls().GrazeCheck = projectile =>
                    {
                        return canDamage == true && GrazeTargetPlayer == Main.myPlayer && Math.Abs((Main.LocalPlayer.Center - entity.Center).Length() - GrazeThreshold) < entity.width / 2 * entity.scale + Player.defaultHeight + Main.LocalPlayer.FargoSouls().GrazeRadius;
                    };
            }
            if (canSplit)
                {
                    entity.FargoSouls().CanSplit = true;
                }
            if (noInteractionWithNPCImmunityFrames)
            {
                entity.FargoSouls().noInteractionWithNPCImmunityFrames = true;
                entity.FargoSouls().GrazeCD = GrazeCD.Value;
            }
            if (GrazeCD != null)
            {
                entity.FargoSouls().GrazeCD = GrazeCD.Value;
            }
        }
    }
}
