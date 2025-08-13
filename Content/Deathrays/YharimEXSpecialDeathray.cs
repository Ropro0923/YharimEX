﻿using System;
using Terraria;

namespace YharimEX.Content.Deathrays
{
    public abstract class YharimEXSpecialDeathray : BaseDeathray
    {

        public override string Texture => "YharimEX/Assets/Deathrays/YharimEXSpecialDeathray";
        public YharimEXSpecialDeathray(int maxTime) : base(maxTime, sheeting: TextureSheeting.Horizontal) { }
        public YharimEXSpecialDeathray(int maxTime, float hitboxModifier) : base(maxTime, hitboxModifier: hitboxModifier, sheeting: TextureSheeting.Horizontal) { }

        bool spawned;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.projFrames[Projectile.type] = 16;
        }

        public override void AI()
        {
            if (!spawned)
            {
                spawned = true;
                Projectile.frame = (int)Math.Abs(Main.GameUpdateCount % Main.projFrames[Projectile.type]);
            }

            Projectile.frameCounter += 1;
            if (++Projectile.frameCounter > 3)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 0;
            }

            if (Main.rand.NextBool(10))
                Projectile.spriteDirection *= -1;
        }
    }
}
