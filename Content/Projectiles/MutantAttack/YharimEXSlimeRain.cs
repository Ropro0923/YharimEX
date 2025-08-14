using FargowiltasSouls.Content.Buffs.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using YharimEX.Content.NPCs.Bosses;
using YharimEX.Core.Globals;
using YharimEX.Core.Systems;
using FargowiltasSouls;

namespace YharimEX.Content.Projectiles
{
    public class YharimEXSlimeRain : ModProjectile
    {
        public override string Texture => "YharimEX/Assets/Projectiles/YharimEXSlimeRain";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 28;
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            CooldownSlot = 1;
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                Projectile.FargoSouls().DeletionImmuneRank = 2;
            }
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            NPC npc = YharimEXGlobalUtilities.NPCExists(Projectile.ai[0], ModContent.NPCType<YharimEXBoss>());
            if (npc != null && (npc.ai[0] == 36 || npc.ai[0] == 48))
            {
                Projectile.timeLeft = 2;
                Projectile.Center = npc.Center;
                Projectile.position.X += Projectile.width / 2 * npc.spriteDirection;
                Projectile.spriteDirection = npc.spriteDirection;
                Projectile.rotation = (float)Math.PI / 4 * Projectile.spriteDirection;
            }
            else
            {
                Projectile.Kill();
                return;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Slimed, 300);
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                if (YharimEXWorldFlags.EternityMode)
                    target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = lightColor;
            color26 = Projectile.GetAlpha(color26);

            SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = color26;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture2D13, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin2, Projectile.scale, effects, 0);
            }

            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Projectile.GetAlpha(lightColor), Projectile.rotation, origin2, Projectile.scale, effects, 0);
            return false;
        }
    }
}