// YHARIM EX
using YharimEX.Content.BossBars;
using YharimEX.Content.NPCs.Town;
using YharimEX.Core.Systems;
using YharimEX.Core.Globals;
using YharimEX.Content.Items;
using YharimEX.Core.Players;


// MOD DEPENDENCIES
using CalamityMod;
using Luminance.Core.Graphics;

// TERRARIA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using YharimEX.Content.Buffs;
using YharimEX.Assets.Sounds.Attacks;
using YharimEX.Assets.ExtraTextures;
using YharimEX.Content.Projectiles.MutantAttacks;
using YharimEX.Content.Projectiles;

namespace YharimEX.Content.NPCs.Bosses
{
    public partial class YharimEXBoss : ModNPC
    {
        bool Phase3Transition()
        {
            bool retval = true;

            NPC.localAI[3] = 3;
            YharimEXPlayer modPlayer = player.GetModPlayer<YharimEXPlayer>();

            EModeSpecialEffects();

            //NPC.damage = 0;
            if (NPC.buffType[0] != 0)
                NPC.DelBuff(0);

            if (NPC.ai[1] == 0) //entering final phase, give healing
            {
                NPC.life = NPC.lifeMax;

                DramaticTransition(true);
            }

            if (NPC.ai[1] < 60 && !Main.dedServ && Main.LocalPlayer.active)
                YharimEXGlobalUtilities.ScreenshakeRumble(6);

            if (NPC.ai[1] == 360)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            }

            if (++NPC.ai[1] > 480)
            {
                retval = false; //dont drain life during this time, ensure it stays synced

                if (!AliveCheck(player))
                    return retval;
                Vector2 targetPos = player.Center;
                targetPos.Y -= 300;
                Movement(targetPos, 1f, true, false);
                if (NPC.Distance(targetPos) < 50 || NPC.ai[1] > 720)
                {
                    NPC.netUpdate = true;
                    NPC.velocity = Vector2.Zero;
                    NPC.localAI[0] = 0;
                    AttackChoice--;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = NPC.DirectionFrom(player.Center).ToRotation();
                    NPC.ai[3] = (float)Math.PI / 20f;
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    if (player.Center.X < NPC.Center.X)
                        NPC.ai[3] *= -1;
                    EdgyBossText(GFBQuote(26));
                }
            }
            else
            {
                NPC.velocity *= 0.9f;

                //make you stop attacking
                if (Main.LocalPlayer.active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost && NPC.Distance(Main.LocalPlayer.Center) < 3000)
                {
                    Main.LocalPlayer.controlUseItem = false;
                    Main.LocalPlayer.controlUseTile = false;
                    modPlayer.NoUsingItems = 2;
                }

                if (--NPC.localAI[0] < 0)
                {
                    NPC.localAI[0] = Main.rand.Next(15);
                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        Vector2 spawnPos = NPC.position + new Vector2(Main.rand.Next(NPC.width), Main.rand.Next(NPC.height));
                        int type = ModContent.ProjectileType<YharimEXBombSmall>();
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer);
                    }
                }
            }

            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.SolarFlare, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }

            return retval;
        }

        void VoidRaysP3()
        {
            if (--NPC.ai[1] < 0)
            {
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    float speed = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.localAI[0] <= 40 ? 4f : 2f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed * Vector2.UnitX.RotatedBy(NPC.ai[2]), ModContent.ProjectileType<YharimEXMark1>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                }
                NPC.ai[1] = 1;
                NPC.ai[2] += NPC.ai[3];

                if (NPC.localAI[0] < 30)
                {
                    EModeSpecialEffects();
                    TryMasoP3Theme();
                }

                if (NPC.localAI[0]++ == 40 || NPC.localAI[0] == 80 || NPC.localAI[0] == 120)
                {
                    NPC.netUpdate = true;
                    NPC.ai[2] -= NPC.ai[3] / ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 2);
                }
                else if (NPC.localAI[0] >= ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 160 : 120))
                {
                    NPC.netUpdate = true;
                    AttackChoice--;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                    NPC.localAI[0] = 0;
                    EdgyBossText(GFBQuote(27));
                }
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.SolarFlare, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }

            NPC.velocity = Vector2.Zero;
        }

        void OkuuSpheresP3()
        {
            if (NPC.ai[2] == 0)
            {
                if (!AliveCheck(player))
                    return;
                NPC.ai[2] = Main.rand.NextBool() ? -1 : 1;
                NPC.ai[3] = Main.rand.NextFloat((float)Math.PI * 2);
            }

            int endTime = 360 + 120;
            if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                endTime += 360;

            if (++NPC.ai[1] > 10 && NPC.ai[3] > 60 && NPC.ai[3] < endTime - 120)
            {
                NPC.ai[1] = 0;
                float rotation = MathHelper.ToRadians(45) * (NPC.ai[3] - 60) / 240 * NPC.ai[2];
                int max = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 11 : 10;
                float speed = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 11f : 10f;
                SpawnSphereRing(max, speed, YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), -0.75f, rotation);
                SpawnSphereRing(max, speed, YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0.75f, rotation);
            }

            if (NPC.ai[3] < 30)
            {
                EModeSpecialEffects();
                TryMasoP3Theme();
            }
            if (NPC.ai[3] == (int)(endTime / 2))
            {
                EdgyBossText(GFBQuote(28));
            }
            if (++NPC.ai[3] > endTime)
            {
                NPC.netUpdate = true;
                AttackChoice--;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                EdgyBossText(GFBQuote(29));
                //NPC.TargetClosest();
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.SolarFlare, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }

            NPC.velocity = Vector2.Zero;
        }

        void BoundaryBulletHellP3()
        {
            if (NPC.localAI[0] == 0)
            {
                if (!AliveCheck(player))
                    return;
                NPC.localAI[0] = Math.Sign(NPC.Center.X - player.Center.X);
            }

            if (++NPC.ai[1] > 3)
            {
                SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
                NPC.ai[1] = 0;
                NPC.ai[2] += (float)Math.PI / 5 / 420 * NPC.ai[3] * NPC.localAI[0] * ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 2f : 1);
                if (NPC.ai[2] > (float)Math.PI)
                    NPC.ai[2] -= (float)Math.PI * 2;
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    int max = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 10 : 8;
                    for (int i = 0; i < max; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0f, -6f).RotatedBy(NPC.ai[2] + MathHelper.TwoPi / max * i),
                            ModContent.ProjectileType<YharimEXEye>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                    }
                }
            }

            if (NPC.ai[3] < 30)
            {
                EModeSpecialEffects();
                TryMasoP3Theme();
            }

            int endTime = 360;
            if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                endTime += 360;
            if (NPC.ai[3] == (int)endTime / 2)
            {
                EdgyBossText(GFBQuote(30));
            }
            if (++NPC.ai[3] > endTime)
            {
                //NPC.TargetClosest();
                AttackChoice--;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                NPC.localAI[0] = 0;
                NPC.netUpdate = true;
            }

            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.SolarFlare, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }

            NPC.velocity = Vector2.Zero;
        }

        void FinalSpark()
        {
            void SpinLaser(bool useMasoSpeed)
            {
                float newRotation = NPC.SafeDirectionTo(Main.player[NPC.target].Center).ToRotation();
                float difference = MathHelper.WrapAngle(newRotation - NPC.ai[3]);
                float rotationDirection = 2f * (float)Math.PI * 1f / 6f / 60f;
                rotationDirection *= useMasoSpeed ? 1.1f : 1f;
                float change = Math.Min(rotationDirection, Math.Abs(difference)) * Math.Sign(difference);
                if (useMasoSpeed)
                {
                    change *= 1.1f;
                    float angleLerp = NPC.ai[3].AngleLerp(newRotation, 0.015f) - NPC.ai[3];
                    if (Math.Abs(MathHelper.WrapAngle(angleLerp)) > Math.Abs(MathHelper.WrapAngle(change)))
                        change = angleLerp;
                }
                NPC.ai[3] += change;

                EdgyBossText(GFBQuote(31));
            }

            /*
            //if targets are all dead, will despawn much more aggressively to reduce respawn cheese
            if (NPC.localAI[2] > 30)
            {
                NPC.localAI[2] += 1; //after 30 ticks of no target, despawn can't be stopped
                if (NPC.localAI[2] > 120)
                    AliveCheck(player, true);
                return;
            }
            */
            if (!AliveCheck(player))
                return;

            if (--NPC.localAI[0] < 0) //just visual explosions
            {
                NPC.localAI[0] = Main.rand.Next(30);
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Vector2 spawnPos = NPC.position + new Vector2(Main.rand.Next(NPC.width), Main.rand.Next(NPC.height));
                    int type = ModContent.ProjectileType<YharimEXBombSmall>();
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer);
                }
            }

            bool harderRings = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.ai[2] >= 420 - 90;
            int ringTime = harderRings ? 100 : 120;
            if (++NPC.ai[1] > ringTime)
            {
                NPC.ai[1] = 0;

                EModeSpecialEffects();
                TryMasoP3Theme();

                if (YharimEXGlobalUtilities.HostCheck)
                {
                    int max = /*harderRings ? 11 :*/ 10;
                    int damage = YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage);
                    SpawnSphereRing(max, 6f, damage, 0.5f);
                    SpawnSphereRing(max, 6f, damage, -.5f);
                }
            }

            if (NPC.ai[2] == 0)
            {
                if (!(YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                    NPC.localAI[1] = 1;
            }
            else if (NPC.ai[2] == 420 - 90) //dramatic telegraph
            {
                if (NPC.localAI[1] == 0) //maso do ordinary spark
                {
                    NPC.localAI[1] = 1;
                    NPC.ai[2] -= 600 + 180;

                    //bias in one direction
                    NPC.ai[3] -= MathHelper.ToRadians(20);

                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitX.RotatedBy(NPC.ai[3]),
                            ModContent.ProjectileType<YharimEXGiantDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 0.5f), 0f, Main.myPlayer, 0, NPC.whoAmI);
                    }

                    NPC.netUpdate = true;
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        const int max = 8;
                        for (int i = 0; i < max; i++)
                        {
                            float offset = i - 0.5f;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, (NPC.ai[3] + MathHelper.TwoPi / max * offset).ToRotationVector2(), ModContent.ProjectileType<YharimEXGlowLine>(), 0, 0f, Main.myPlayer, 13f, NPC.whoAmI);
                        }
                    }
                }
            }

            if (NPC.ai[2] < 420)
            {
                //disable it while doing maso's first ray
                if (NPC.localAI[1] == 0 || NPC.ai[2] > 420 - 90)
                    NPC.ai[3] = NPC.DirectionFrom(player.Center).ToRotation(); //hold it here for glow line effect
            }
            else
            {
                if (!Main.dedServ)
                {
                    ManagedScreenFilter filter = ShaderManager.GetFilter("YharimEX.FinalSpark");
                    filter.Activate();
                    if (Main.WaveQuality == 0)
                        Main.WaveQuality = 1;
                }

                if (NPC.ai[1] % 3 == 0 && YharimEXGlobalUtilities.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, 24f * Vector2.UnitX.RotatedBy(NPC.ai[3]), ModContent.ProjectileType<YharimEXEyeWavy>(), 0, 0f, Main.myPlayer,
                      Main.rand.NextFloat(0.5f, 1.25f) * (Main.rand.NextBool() ? -1 : 1), Main.rand.Next(10, 60));
                }
            }

            int endTime = 1020;
            if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                endTime += 180;
            if (++NPC.ai[2] > endTime && NPC.life <= 1)
            {
                NPC.netUpdate = true;
                AttackChoice--;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                YharimEXGlobalUtilities.ClearAllProjectiles(2, NPC.whoAmI);
            }
            else if (NPC.ai[2] == 420)
            {
                NPC.netUpdate = true;

                //bias it in one direction
                NPC.ai[3] += MathHelper.ToRadians(20) * ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 1 : -1);

                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitX.RotatedBy(NPC.ai[3]),
                        ModContent.ProjectileType<YharimEXGiantDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 0.5f), 0f, Main.myPlayer, 0, NPC.whoAmI);
                }
            }
            else if (NPC.ai[2] < 300 && NPC.localAI[1] != 0) //charging up dust
            {
                float num1 = 0.99f;
                if (NPC.ai[2] >= 60)
                    num1 = 0.79f;
                if (NPC.ai[2] >= 120)
                    num1 = 0.58f;
                if (NPC.ai[2] >= 180)
                    num1 = 0.43f;
                if (NPC.ai[2] >= 240)
                    num1 = 0.33f;
                for (int i = 0; i < 9; ++i)
                {
                    if (Main.rand.NextFloat() >= num1)
                    {
                        float f = Main.rand.NextFloat() * 6.283185f;
                        float num2 = Main.rand.NextFloat();
                        Dust dust = Dust.NewDustPerfect(NPC.Center + f.ToRotationVector2() * (110 + 600 * num2), 229, (f - 3.141593f).ToRotationVector2() * (14 + 8 * num2), 0, default, 1f);
                        dust.scale = 0.9f;
                        dust.fadeIn = 1.15f + num2 * 0.3f;
                        //dust.color = new Color(1f, 1f, 1f, num1) * (1f - num1);
                        dust.noGravity = true;
                        //dust.noLight = true;
                    }
                }
            }

            SpinLaser((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.ai[2] >= 420);

            if (AliveCheck(player))
                NPC.localAI[2] = 0;
            else
                NPC.localAI[2]++;

            NPC.velocity = Vector2.Zero; //prevents mutant from moving despite calling AliveCheck()
        }

        void DyingDramaticPause()
        {
            if (!AliveCheck(player))
                return;
            Mod FargoSouls = YharimEXCrossmodSystem.FargowiltasSouls.Mod;
            NPC.ai[3] -= (float)Math.PI / 6f / 60f;
            NPC.velocity = Vector2.Zero;
            //in maso, if player got timestopped at very end of final spark, fucking kill them
            bool killPlayer = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && Main.player[NPC.target].HasBuff(FargosSouls.Find<ModBuff>("TimeFrozenBuff").Type);
            if (killPlayer)
            {
                if (++NPC.ai[2] > 15)
                {
                    NPC.ai[2] -= 15;
                    int realDefDamage = NPC.defDamage;
                    NPC.defDamage *= 10;
                    SpawnSpearTossDirectP2Attack();
                    NPC.defDamage = realDefDamage;
                }
            }
            else if (++NPC.ai[1] > 120)
            {
                NPC.netUpdate = true;
                AttackChoice--;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = (float)-Math.PI / 2;
                NPC.netUpdate = true;
                if (YharimEXGlobalUtilities.HostCheck) //shoot death anim mega ray
                {
                    int damage = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 0.5f) : 0;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitY * -1,
                        ModContent.ProjectileType<YharimEXGiantDeathray2>(),
                        damage, 0f, Main.myPlayer, 1, NPC.whoAmI);
                }
                EdgyBossText(GFBQuote(32));
            }
            if (--NPC.localAI[0] < 0)
            {
                NPC.localAI[0] = Main.rand.Next(15);
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Vector2 spawnPos = NPC.position + new Vector2(Main.rand.Next(NPC.width), Main.rand.Next(NPC.height));
                    int type = ModContent.ProjectileType<YharimEXBomb>();
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer);
                }
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.SolarFlare, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }
        }

        void DyingAnimationAndHandling()
        {
            /*if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
            {
                if (!AliveCheck(player))
                    return;
                i'm not THAT fucked up
            }*/
            NPC.velocity = Vector2.Zero;
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.SolarFlare, 0f, 0f, 0, default, 2.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 12f;
            }
            if (--NPC.localAI[0] < 0)
            {
                NPC.localAI[0] = Main.rand.Next(5);
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Vector2 spawnPos = NPC.Center + Main.rand.NextVector2Circular(240, 240);
                    int type = ModContent.ProjectileType<YharimEXBomb>();
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer);
                }
            }
            if (++NPC.ai[1] % 3 == 0 && YharimEXGlobalUtilities.HostCheck)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, 24f * Vector2.UnitX.RotatedBy(NPC.ai[3]), ModContent.ProjectileType<YharimEXEyeWavy>(), 0, 0f, Main.myPlayer,
                    Main.rand.NextFloat(0.75f, 1.5f) * (Main.rand.NextBool() ? -1 : 1), Main.rand.Next(10, 90));
            }
            if (++NPC.alpha > 255)
            {
                NPC.alpha = 255;
                NPC.life = 0;
                NPC.dontTakeDamage = false;
                NPC.checkDead();
                if (YharimEXGlobalUtilities.HostCheck && ModContent.TryFind("YharimEX", "TheGodseeker", out ModNPC modNPC) && !NPC.AnyNPCs(modNPC.Type))
                {
                    int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, modNPC.Type);
                    if (n != Main.maxNPCs)
                    {
                        Main.npc[n].homeless = true;
                        if (TownNPCName != default)
                            Main.npc[n].GivenName = TownNPCName;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                    }
                }
                EdgyBossText(GFBQuote(33));
            }
        }
    }
}