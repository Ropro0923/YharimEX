using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Core.Systems;
using YharimEX.Core;
using FargowiltasSouls.Core.ModPlayers;

namespace YharimEX.Content.Projectiles
{
	public class YharimEXBigFlare : ModProjectile
	{
		public override string Texture => "CalamityMod/Projectiles/Boss/BigFlare";
		public override void SetStaticDefaults()
		{
			Main.projFrames[Projectile.type] = 4;
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
			ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.hostile = true;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 600;
			Projectile.alpha = 100;
			CooldownSlot = 1;
		}

		public override bool CanHitPlayer(Player target)
		{
			return target.hurtCooldowns[1] == 0;
		}

		public override void AI()
		{
			((Entity)((ModProjectile)this).Projectile).velocity = Utils.RotatedBy(((Entity)((ModProjectile)this).Projectile).velocity, (double)((ModProjectile)this).Projectile.ai[1] / (Math.PI * 2.0 * (double)((ModProjectile)this).Projectile.ai[0] * (double)(((ModProjectile)this).Projectile.localAI[0] += 1f)), default(Vector2));
			int cap = Main.rand.Next(3);
			for (int index1 = 0; index1 < cap; index1++)
			{
				Vector2 vector2_1 = ((Entity)((ModProjectile)this).Projectile).velocity;
				vector2_1 = Utils.SafeNormalize(vector2_1, Vector2.UnitX);
				vector2_1.X *= ((Entity)((ModProjectile)this).Projectile).width;
				vector2_1.Y *= ((Entity)((ModProjectile)this).Projectile).height;
				vector2_1 /= 2f;
				vector2_1 = Utils.RotatedBy(vector2_1, (double)(index1 - 2) * Math.PI / 6.0, default(Vector2));
				vector2_1 += ((Entity)((ModProjectile)this).Projectile).Center;
				Vector2 vector2_2 = Utils.ToRotationVector2(Utils.NextFloat(Main.rand) * (float)Math.PI - (float)Math.PI / 2f);
				vector2_2 *= (float)Main.rand.Next(3, 8);
				int index2 = Dust.NewDust(vector2_1 + vector2_2, 0, 0, 172, vector2_2.X * 2f, vector2_2.Y * 2f, 100, default(Color), 1.4f);
				Main.dust[index2].noGravity = true;
				Main.dust[index2].noLight = true;
				Dust obj = Main.dust[index2];
				obj.velocity /= 4f;
				Dust obj2 = Main.dust[index2];
				obj2.velocity -= ((Entity)((ModProjectile)this).Projectile).velocity;
			}
			Projectile projectile = ((ModProjectile)this).Projectile;
			projectile.rotation += 0.2f * ((((Entity)((ModProjectile)this).Projectile).velocity.X > 0f) ? 1f : (-1f));
			Projectile projectile2 = ((ModProjectile)this).Projectile;
			projectile2.frame++;
			if (((ModProjectile)this).Projectile.frame > 2)
			{
				((ModProjectile)this).Projectile.frame = 0;
			}
		}

		public override void OnHitPlayer(Player player, Player.HurtInfo hurtInfo)
		{

			if (WorldSavingSystem.EternityMode)
			{
				player.GetModPlayer<FargoSoulsPlayer>().MaxLifeReduction += 100;
				player.AddBuff(ModContent.BuffType<OceanicMaulBuff>(), 5400, true, false);
				player.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180, true, false);
			}
			player.AddBuff(ModContent.BuffType<DefenselessBuff>(), Main.rand.Next(600, 900), true, false);
			player.AddBuff(196, Main.rand.Next(300, 600), true, false);
		}

		public override void OnKill(int timeLeft)
		{
			int num1 = 36;
			for (int index1 = 0; index1 < num1; index1++)
			{
				Vector2 val = Utils.RotatedBy(Vector2.Normalize(((Entity)((ModProjectile)this).Projectile).velocity) * new Vector2((float)((Entity)((ModProjectile)this).Projectile).width / 2f, (float)((Entity)((ModProjectile)this).Projectile).height) * 0.75f, (double)(index1 - (num1 / 2 - 1)) * 6.28318548202515 / (double)num1, default(Vector2)) + ((Entity)((ModProjectile)this).Projectile).Center;
				Vector2 vector2_2 = val - ((Entity)((ModProjectile)this).Projectile).Center;
				int index2 = Dust.NewDust(val + vector2_2, 0, 0, DustID.DungeonWater, vector2_2.X * 2f, vector2_2.Y * 2f, 100, default(Color), 1.4f);
				Main.dust[index2].noGravity = true;
				Main.dust[index2].noLight = true;
				Main.dust[index2].velocity = vector2_2;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture2D13 = TextureAssets.Projectile[((ModProjectile)this).Projectile.type].Value;
			int num156 = TextureAssets.Projectile[((ModProjectile)this).Projectile.type].Value.Height / Main.projFrames[((ModProjectile)this).Projectile.type];
			int y3 = num156 * ((ModProjectile)this).Projectile.frame;
			Rectangle rectangle = new Rectangle(0, y3, texture2D13.Width, num156);
			Vector2 origin2 = Utils.Size(rectangle) / 2f;
			Color color26 = lightColor;
			color26 = ((ModProjectile)this).Projectile.GetAlpha(color26);
			for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[((ModProjectile)this).Projectile.type]; i += 2)
			{
				Color color27 = color26;
				color27 *= (float)(ProjectileID.Sets.TrailCacheLength[((ModProjectile)this).Projectile.type] - i) / (float)ProjectileID.Sets.TrailCacheLength[((ModProjectile)this).Projectile.type];
				Vector2 value4 = ((ModProjectile)this).Projectile.oldPos[i];
				float num165 = ((ModProjectile)this).Projectile.oldRot[i];
				Main.EntitySpriteDraw(texture2D13, value4 + ((Entity)((ModProjectile)this).Projectile).Size / 2f - Main.screenPosition + new Vector2(0f, ((ModProjectile)this).Projectile.gfxOffY), (Rectangle?)rectangle, color27, num165, origin2, ((ModProjectile)this).Projectile.scale, (SpriteEffects)0, 0);
			}
			Main.EntitySpriteDraw(texture2D13, ((Entity)((ModProjectile)this).Projectile).Center - Main.screenPosition + new Vector2(0f, ((ModProjectile)this).Projectile.gfxOffY), (Rectangle?)rectangle, ((ModProjectile)this).Projectile.GetAlpha(lightColor), ((ModProjectile)this).Projectile.rotation, origin2, ((ModProjectile)this).Projectile.scale, (SpriteEffects)0, 0);
			return false;
		}

		public override Color? GetAlpha(Color lightColor)
		{
			return new Color(100, 100, 250, 200);
		}
	}
}