using System;
using System.Collections.Generic;
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
        public int DeletiionImmuneRank = -1;
        public override bool InstancePerEntity => true;
        public override void SetDefaults(Projectile entity)
        {
            if (TimeFreezeImmune) entity.FargoSouls().TimeFreezeImmune = true;
            if (DeletiionImmuneRank != -1) entity.FargoSouls().DeletionImmuneRank = DeletiionImmuneRank;
        }
    }
}
