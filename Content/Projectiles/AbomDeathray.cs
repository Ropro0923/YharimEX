using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FargowiltasSouls.Content.Bosses.AbomBoss;
using FargowiltasSouls.Content.Items.BossBags;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Core.ModPlayers;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using YharimEX.Core;

namespace YharimEX.Content.Projectiles
{
    public class AbomDeathray : BaseDeathray
    {
        private Vector2 spawnPos;
        public bool DontSpawn;

        public override string Texture => "YharimEX/Content/Projectiles/AbomDeathray";

        public AbomDeathray() : base(120f) { }

        public override void SetStaticDefaults()
        {
            //DisplayName.SetDefault("Abominable Deathray");
        }

        public override void AI()
        {
            if (!Main.dedServ && Main.LocalPlayer.active)
                YharimEXUtil.ScreenshakeRumble(2);

            // Ensure a valid velocity
            if (Utils.HasNaNs(Projectile.velocity) || Projectile.velocity == Vector2.Zero)
                Projectile.velocity = -Vector2.UnitY;

            if (Projectile.localAI[0] == 0f)
            {
                if (!Main.dedServ)
                {
                    SoundStyle sound = new("YharimEX/Assets/Sounds/Zombie_104", SoundType.Sound)
                    {
                        Volume = 0.5f
                    };
                    SoundEngine.PlaySound(sound, Projectile.Center);
                }
                spawnPos = Projectile.Center;
            }
            else
            {
                Projectile.Center = spawnPos + Main.rand.NextVector2Circular(5f, 5f);
            }

            const float maxScale = 5f;
            Projectile.localAI[0]++;

            if (Projectile.localAI[0] >= maxTime)
            {
                Projectile.Kill();
                return;
            }

            Projectile.scale = (float)Math.Sin(Projectile.localAI[0] * Math.PI / maxTime) * maxScale * 6f;
            if (Projectile.scale > maxScale)
                Projectile.scale = maxScale;

            if (Projectile.localAI[0] > maxTime / 2f && Projectile.scale < maxScale && Projectile.ai[0] > 0f && !DontSpawn)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = Main.rand.Next(120); i < 3000; i += 500)
                    {
                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            Projectile.Center + Projectile.velocity * i,
                            Vector2.Zero,
                            ModContent.ProjectileType<AbomScytheSplit>(),
                            Projectile.damage,
                            Projectile.knockBack,
                            Projectile.owner,
                            Projectile.ai[0],
                            -1f
                        );
                    }
                }
                Projectile.ai[0] = 0f;
            }

            // Fake laser scan
            const int samples = 3;
            float[] distances = new float[samples];
            for (int i = 0; i < samples; i++)
                distances[i] = 3000f;

            float avgDistance = 0f;
            for (int i = 0; i < samples; i++)
                avgDistance += distances[i];
            avgDistance /= samples;

            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], avgDistance, 0.5f);

            Vector2 beamPos = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 14f);

            // Side dusts
            for (int i = 0; i < 2; i++)
            {
                float rot = Projectile.velocity.ToRotation() + ((Main.rand.NextBool()) ? -1f : 1f) * MathHelper.PiOver2;
                float speed = Main.rand.NextFloat(2f, 4f);
                Vector2 dustVel = new Vector2((float)Math.Cos(rot) * speed, (float)Math.Sin(rot) * speed);

                int dustIndex = Dust.NewDust(beamPos, 0, 0, DustID.CopperCoin, dustVel.X, dustVel.Y);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].scale = 1.7f;
            }

            // Random side puff
            if (Main.rand.NextBool(5))
            {
                Vector2 offset = Projectile.velocity.RotatedBy(MathHelper.PiOver2) *
                                 ((float)Main.rand.NextDouble() - 0.5f) * Projectile.width;

                int dustIndex = Dust.NewDust(beamPos + offset - Vector2.One * 4f, 8, 8, DustID.CopperCoin);
                Main.dust[dustIndex].velocity *= 0.5f;
                Main.dust[dustIndex].velocity.Y = -Math.Abs(Main.dust[dustIndex].velocity.Y);
                Main.dust[dustIndex].scale = 1.5f;
            }

            Projectile.position -= Projectile.velocity;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            //target.AddBuff(ModContent.BuffType<AbomFang>(), 300);
            target.AddBuff(BuffID.Bleeding, 180);
            target.AddBuff(BuffID.Weak, 600);
            target.AddBuff(BuffID.BrokenArmor, 600);
        }
    }
}
