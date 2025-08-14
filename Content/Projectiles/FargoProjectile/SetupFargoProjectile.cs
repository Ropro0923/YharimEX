using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using FargowiltasSouls;
using Terraria;
using Terraria.ModLoader;
using YharimEX.Core.Systems;

namespace YharimEX.Content.Projectiles.FargoProjectile
{
    [ExtendsFromMod(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    [JITWhenModsEnabled(YharimEXCrossmodSystem.FargowiltasSouls.Name)]
    public class SetupFargoProjectile : GlobalProjectile
    {
        public bool TimeFreezeImmune = false;
        public int DeletionImmuneRank = -1;
        public bool GrazeCheck = false;
        public int safeRange = 0;
        public bool? canDamage = false;
        public bool canSplit = false;
        public int GrazeCD;
        public bool noInteractionWithNPCImmunityFrames = false;
        public override bool InstancePerEntity => true;
        public override void SetDefaults(Projectile entity)
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
            if (canSplit) entity.FargoSouls().CanSplit = true;
            if (noInteractionWithNPCImmunityFrames) entity.FargoSouls().noInteractionWithNPCImmunityFrames = true;
        }

        public static void SetGrazeCD(Projectile projectile)
        {
            if (projectile.FargoSouls().GrazeCD > 10)
                projectile.FargoSouls().GrazeCD = 10;
        }
    }
}
