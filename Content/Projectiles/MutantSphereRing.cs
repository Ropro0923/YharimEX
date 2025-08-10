using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace YharimEX.Content.Projectiles
{
	public class MutantSphereRing : ModProjectile
	{
		protected bool DieOutsideArena;
		private int ritualID = -1;

		private float originalSpeed;

		private bool spawned;

		public override string Texture => "YharimEX/Content/Projectiles/MutantSphere";

		public override void SetStaticDefaults()
		{
			Main.projFrames[Projectile.type] = 2;
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
			ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.hostile = true;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 480;
			Projectile.alpha = 200;
			CooldownSlot = 1;
			if (Projectile.type == ModContent.ProjectileType<MutantSphereRing>())
			{
				DieOutsideArena = true;
				Projectile.GetGlobalProjectile<FargoSoulsGlobalProjectile>(true).TimeFreezeImmune = FargoSoulsWorld.MasochistModeReal && FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.mutantBoss, ModContent.NPCType<global::FargowiltasSouls.NPCs.MutantBoss.MutantBoss>()) && Main.npc[EModeGlobalNPC.mutantBoss].ai[0] == -5f;
			}
		}

		public override bool CanHitPlayer(Player target)
		{
			if (target.hurtCooldowns[1] != 0)
			{
				return FargoSoulsWorld.MasochistModeReal;
			}
			return true;
		}

		public override void AI()
		{
			if (!spawned)
			{
				spawned = true;
				originalSpeed = ((Vector2)(ref ((Entity)((ModProjectile)this).Projectile).velocity)).Length();
			}
			((Entity)((ModProjectile)this).Projectile).velocity = originalSpeed * Utils.RotatedBy(Vector2.Normalize(((Entity)((ModProjectile)this).Projectile).velocity), (double)((ModProjectile)this).Projectile.ai[1] / (Math.PI * 2.0 * (double)((ModProjectile)this).Projectile.ai[0] * (double)(((ModProjectile)this).Projectile.localAI[0] += 1f)), default(Vector2));
			if (((ModProjectile)this).Projectile.alpha > 0)
			{
				Projectile projectile = ((ModProjectile)this).Projectile;
				projectile.alpha -= 20;
				if (((ModProjectile)this).Projectile.alpha < 0)
				{
					((ModProjectile)this).Projectile.alpha = 0;
				}
			}
			((ModProjectile)this).Projectile.scale = 1f - (float)((ModProjectile)this).Projectile.alpha / 255f;
			Projectile projectile2 = ((ModProjectile)this).Projectile;
			if (++projectile2.frameCounter >= 6)
			{
				((ModProjectile)this).Projectile.frameCounter = 0;
				Projectile projectile3 = ((ModProjectile)this).Projectile;
				if (++projectile3.frame > 1)
				{
					((ModProjectile)this).Projectile.frame = 0;
				}
			}
			if (DieOutsideArena)
			{
				if (ritualID == -1)
				{
					ritualID = -2;
					for (int i = 0; i < 1000; i++)
					{
						if (((Entity)Main.projectile[i]).active && Main.projectile[i].type == ModContent.ProjectileType<MutantRitual>())
						{
							ritualID = i;
							break;
						}
					}
				}
				Projectile ritual = FargoSoulsUtil.ProjectileExists(ritualID, ModContent.ProjectileType<MutantRitual>());
				if (ritual != null && ((Entity)((ModProjectile)this).Projectile).Distance(((Entity)ritual).Center) > 1200f)
				{
					((ModProjectile)this).Projectile.timeLeft = 0;
				}
			}
			if (((Entity)Main.LocalPlayer).active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost && FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.mutantBoss, ModContent.NPCType<global::FargowiltasSouls.NPCs.MutantBoss.MutantBoss>()) && FargoSoulsWorld.MasochistModeReal && Main.npc[EModeGlobalNPC.mutantBoss].ai[0] == -5f && ((ModProjectile)this).Projectile.Colliding(((Entity)((ModProjectile)this).Projectile).Hitbox, Main.LocalPlayer.GetModPlayer<FargoSoulsPlayer>().GetPrecisionHurtbox()))
			{
				if (!Main.LocalPlayer.HasBuff(ModContent.BuffType<TimeFrozen>()))
				{
					SoundStyle val = new SoundStyle("FargowiltasSouls/Sounds/ZaWarudo", (SoundType)0);
					SoundEngine.PlaySound(ref val, (Vector2?)((Entity)Main.LocalPlayer).Center);
				}
				Main.LocalPlayer.AddBuff(ModContent.BuffType<TimeFrozen>(), 300, true, false);
			}
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.mutantBoss, ModContent.NPCType<global::FargowiltasSouls.NPCs.MutantBoss.MutantBoss>()) && FargoSoulsWorld.EternityMode)
			{
				target.GetModPlayer<FargoSoulsPlayer>().MaxLifeReduction += 100;
				target.AddBuff(ModContent.BuffType<OceanicMaul>(), 5400, true, false);
				target.AddBuff(ModContent.BuffType<MutantFang>(), 180, true, false);
			}
			target.AddBuff(ModContent.BuffType<CurseoftheMoon>(), 360, true, false);
		}

		public override void Kill(int timeleft)
		{
			if (Utils.NextBool(Main.rand, Main.player[((ModProjectile)this).Projectile.owner].ownedProjectileCounts[((ModProjectile)this).Projectile.type] / 10 + 1))
			{
				SoundEngine.PlaySound(ref SoundID.NPCDeath6, (Vector2?)((Entity)((ModProjectile)this).Projectile).Center);
			}
			((Entity)((ModProjectile)this).Projectile).position = ((Entity)((ModProjectile)this).Projectile).Center;
			((Entity)((ModProjectile)this).Projectile).width = (((Entity)((ModProjectile)this).Projectile).height = 208);
			((Entity)((ModProjectile)this).Projectile).Center = ((Entity)((ModProjectile)this).Projectile).position;
			for (int index1 = 0; index1 < 2; index1++)
			{
				int index2 = Dust.NewDust(((Entity)((ModProjectile)this).Projectile).position, ((Entity)((ModProjectile)this).Projectile).width, ((Entity)((ModProjectile)this).Projectile).height, 31, 0f, 0f, 100, default(Color), 1.5f);
				Main.dust[index2].position = Utils.RotatedBy(new Vector2((float)(((Entity)((ModProjectile)this).Projectile).width / 2), 0f), 6.28318548202515 * Main.rand.NextDouble(), default(Vector2)) * (float)Main.rand.NextDouble() + ((Entity)((ModProjectile)this).Projectile).Center;
			}
			for (int i = 0; i < 4; i++)
			{
				int index3 = Dust.NewDust(((Entity)((ModProjectile)this).Projectile).position, ((Entity)((ModProjectile)this).Projectile).width, ((Entity)((ModProjectile)this).Projectile).height, 60, 0f, 0f, 0, default(Color), 2.5f);
				Main.dust[index3].position = Utils.RotatedBy(new Vector2((float)(((Entity)((ModProjectile)this).Projectile).width / 2), 0f), 6.28318548202515 * Main.rand.NextDouble(), default(Vector2)) * (float)Main.rand.NextDouble() + ((Entity)((ModProjectile)this).Projectile).Center;
				Main.dust[index3].noGravity = true;
				Dust obj = Main.dust[index3];
				obj.velocity *= 1f;
				int index4 = Dust.NewDust(((Entity)((ModProjectile)this).Projectile).position, ((Entity)((ModProjectile)this).Projectile).width, ((Entity)((ModProjectile)this).Projectile).height, 60, 0f, 0f, 100, default(Color), 1.5f);
				Main.dust[index4].position = Utils.RotatedBy(new Vector2((float)(((Entity)((ModProjectile)this).Projectile).width / 2), 0f), 6.28318548202515 * Main.rand.NextDouble(), default(Vector2)) * (float)Main.rand.NextDouble() + ((Entity)((ModProjectile)this).Projectile).Center;
				Dust obj2 = Main.dust[index4];
				obj2.velocity *= 1f;
				Main.dust[index4].noGravity = true;
			}
		}

		public override Color? GetAlpha(Color lightColor)
		{
			return Color.White * ((ModProjectile)this).Projectile.Opacity;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D glow = ((Mod)FargowiltasSouls.Instance).Assets.Request<Texture2D>("Projectiles/MutantBoss/MutantSphereGlow", (AssetRequestMode)1).Value;
			int rect1 = glow.Height;
			int rect2 = 0;
			Rectangle glowrectangle = default(Rectangle);
			((Rectangle)(ref glowrectangle))._002Ector(0, rect2, glow.Width, rect1);
			Vector2 gloworigin2 = Utils.Size(glowrectangle) / 2f;
			Color glowcolor = Color.Lerp(new Color(255, 37, 45, 0), Color.Transparent, 0.9f);
			for (int i = 0; i < Sets.TrailCacheLength[((ModProjectile)this).Projectile.type]; i++)
			{
				Color color27 = glowcolor;
				color27 *= (float)(Sets.TrailCacheLength[((ModProjectile)this).Projectile.type] - i) / (float)Sets.TrailCacheLength[((ModProjectile)this).Projectile.type];
				float scale = ((ModProjectile)this).Projectile.scale * (float)(Sets.TrailCacheLength[((ModProjectile)this).Projectile.type] - i) / (float)Sets.TrailCacheLength[((ModProjectile)this).Projectile.type];
				Vector2 value4 = ((ModProjectile)this).Projectile.oldPos[i] - Vector2.Normalize(((Entity)((ModProjectile)this).Projectile).velocity) * (float)i * 6f;
				Main.EntitySpriteDraw(glow, value4 + ((Entity)((ModProjectile)this).Projectile).Size / 2f - Main.screenPosition + new Vector2(0f, ((ModProjectile)this).Projectile.gfxOffY), (Rectangle?)glowrectangle, color27, Utils.ToRotation(((Entity)((ModProjectile)this).Projectile).velocity) + (float)Math.PI / 2f, gloworigin2, scale * 1.5f, (SpriteEffects)0, 0);
			}
			glowcolor = Color.Lerp(new Color(255, 255, 255, 0), Color.Transparent, 0.85f);
			Main.EntitySpriteDraw(glow, ((Entity)((ModProjectile)this).Projectile).position + ((Entity)((ModProjectile)this).Projectile).Size / 2f - Main.screenPosition + new Vector2(0f, ((ModProjectile)this).Projectile.gfxOffY), (Rectangle?)glowrectangle, glowcolor, Utils.ToRotation(((Entity)((ModProjectile)this).Projectile).velocity) + (float)Math.PI / 2f, gloworigin2, ((ModProjectile)this).Projectile.scale * 1.5f, (SpriteEffects)0, 0);
			return false;
		}

		public override void PostDraw(Color lightColor)
		{
			Texture2D texture2D13 = TextureAssets.Projectile[((ModProjectile)this).Projectile.type].Value;
			int num156 = TextureAssets.Projectile[((ModProjectile)this).Projectile.type].Value.Height / Main.projFrames[((ModProjectile)this).Projectile.type];
			int y3 = num156 * ((ModProjectile)this).Projectile.frame;
			Rectangle rectangle = default(Rectangle);
			((Rectangle)(ref rectangle))._002Ector(0, y3, texture2D13.Width, num156);
			Vector2 origin2 = Utils.Size(rectangle) / 2f;
			Main.EntitySpriteDraw(texture2D13, ((Entity)((ModProjectile)this).Projectile).Center - Main.screenPosition + new Vector2(0f, ((ModProjectile)this).Projectile.gfxOffY), (Rectangle?)rectangle, ((ModProjectile)this).Projectile.GetAlpha(lightColor), ((ModProjectile)this).Projectile.rotation, origin2, ((ModProjectile)this).Projectile.scale, (SpriteEffects)0, 0);
		}
	}
}