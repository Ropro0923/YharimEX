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
        void Phase2Transition()
        {
            YharimEXPlayer modPlayer = player.GetModPlayer<YharimEXPlayer>();
            NPC.velocity *= 0.9f;
            NPC.dontTakeDamage = true;

            if (NPC.buffType[0] != 0)
                NPC.DelBuff(0);

            EModeSpecialEffects();

            if (NPC.ai[2] == 0)
            {
                if (NPC.ai[1] < 60 && !Main.dedServ && Main.LocalPlayer.active)
                    YharimEXGlobalUtilities.ScreenshakeRumble(6);
            }
            else
            {
                NPC.velocity = Vector2.Zero;
            }

            if (NPC.ai[1] < 240)
            {
                //make you stop attacking
                if (Main.LocalPlayer.active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost && NPC.Distance(Main.LocalPlayer.Center) < 3000)
                {
                    Main.LocalPlayer.controlUseItem = false;
                    Main.LocalPlayer.controlUseTile = false;

                    modPlayer.NoUsingItems = 2;
                }
            }

            if (NPC.ai[1] == 0)
            {
                YharimEXGlobalUtilities.ClearAllProjectiles(2, NPC.whoAmI);

                if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
                {
                    DramaticTransition(false, NPC.ai[2] == 0);

                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        ritualProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXRitual>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0f, NPC.whoAmI);

                        if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXRitual2>(), 0, 0f, Main.myPlayer, 0f, NPC.whoAmI);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXRitual3>(), 0, 0f, Main.myPlayer, 0f, NPC.whoAmI);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXRitual4>(), 0, 0f, Main.myPlayer, 0f, NPC.whoAmI);
                        }
                    }
                }
            }
            else if (NPC.ai[1] == 150)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                if (YharimEXGlobalUtilities.HostCheck)
                {
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), 0, 0f, Main.myPlayer, 5);
                    //Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, -22);
                }

                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) && YharimEXWorldFlags.SkipYharimEXP1 <= 10)
                {
                    YharimEXWorldFlags.SkipYharimEXP1++;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.WorldData);
                }

                for (int i = 0; i < 50; i++)
                {
                    int d = Dust.NewDust(Main.LocalPlayer.position, Main.LocalPlayer.width, Main.LocalPlayer.height, DustID.SolarFlare, 0f, 0f, 0, default, 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 9f;
                }
                EdgyBossText(GFBQuote(1));
            }
            else if (NPC.ai[1] > 150)
            {
                NPC.localAI[3] = 3;
            }

            if (++NPC.ai[1] > 270)
            {
                if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
                {
                    NPC.life = NPC.lifeMax;
                    AttackChoice = Main.rand.Next(new int[] { 11, 13, 16, 19, 20, 21, 24, 26, 29, 35, 37, 39, 42/*, 47*//*, 49*/ }); //force a random choice
                }
                else
                {
                    AttackChoice++;
                }
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                //NPC.TargetClosest();
                NPC.netUpdate = true;

                attackHistory.Enqueue(AttackChoice);
            }
        }

        void ApproachForNextAttackP2()
        {
            if (!AliveCheck(player))
                return;
            Vector2 targetPos = player.Center + player.SafeDirectionTo(NPC.Center) * 300;
            if (NPC.Distance(targetPos) > 50 && ++NPC.ai[2] < 180)
            {
                Movement(targetPos, 0.8f);
            }
            else
            {
                NPC.netUpdate = true;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.ai[2] = player.SafeDirectionTo(NPC.Center).ToRotation();
                NPC.ai[3] = (float)Math.PI / 10f;
                NPC.localAI[0] = 0;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (player.Center.X < NPC.Center.X)
                    NPC.ai[3] *= -1;
            }
        }

        void VoidRaysP2()
        {
            NPC.velocity = Vector2.Zero;
            if (--NPC.ai[1] < 0)
            {
                if (YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(2, 0).RotatedBy(NPC.ai[2]), ModContent.ProjectileType<YharimEXMark1>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                NPC.ai[1] = 3;
                NPC.ai[2] += NPC.ai[3];

                if (NPC.localAI[0]++ == 20 || NPC.localAI[0] == 40)
                {
                    NPC.netUpdate = true;
                    NPC.ai[2] -= NPC.ai[3] / ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 2);

                    if (NPC.localAI[0] == 21 && endTimeVariance > 0.33f //sometimes skip to end
                    || NPC.localAI[0] == 41 && endTimeVariance < -0.33f)
                        NPC.localAI[0] = 60;

                    EdgyBossText(GFBQuote(6));
                }
                else if (NPC.localAI[0] >= 60)
                {
                    ChooseNextAttack(13, 19, 21, 24, 31, 39, 41, 42/*, 49*/);
                }
            }
        }

        void PrepareSpearDashPredictiveP2()
        {
            if (NPC.ai[3] == 0)
            {
                if (!AliveCheck(player))
                    return;
                NPC.ai[3] = 1;
                //NPC.velocity = NPC.DirectionFrom(player.Center) * NPC.velocity.Length();
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearSpin>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 180); // + 60);
                    TelegraphSound = SoundEngine.PlaySound(YharimEXSoundRegistry.YharimEXPredictive with { Volume = 8f }, NPC.Center);
                }

                EdgyBossText(GFBQuote(9));
            }

            if (++NPC.ai[1] > 180)
            {
                if (!AliveCheck(player))
                    return;
                NPC.netUpdate = true;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.ai[3] = 0;
                //NPC.TargetClosest();
            }

            Vector2 targetPos = player.Center;
            targetPos.Y += 400f * Math.Sign(NPC.Center.Y - player.Center.Y); //can be above or below
            Movement(targetPos, 0.7f, false);
            if (NPC.Distance(player.Center) < 200)
                Movement(NPC.Center + NPC.DirectionFrom(player.Center), 1.4f);
        }

        void SpearDashPredictiveP2()
        {
            if (NPC.localAI[1] == 0) //max number of attacks
            {
                if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
                    NPC.localAI[1] = Main.rand.Next((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 5, 9);
                else
                    NPC.localAI[1] = 5;
            }

            if (NPC.ai[1] == 0) //telegraph
            {
                if (!AliveCheck(player))
                    return;

                if (NPC.ai[2] == NPC.localAI[1] - 1)
                {
                    if (NPC.Distance(player.Center) > 450) //get closer for last dash
                    {
                        Movement(player.Center, 0.6f);
                        return;
                    }

                    NPC.velocity *= 0.75f; //try not to bump into player
                }

                if (NPC.ai[2] < NPC.localAI[1])
                {
                    if (YharimEXGlobalUtilities.HostCheck)
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center + player.velocity * 30f), ModContent.ProjectileType<YharimEXDeathrayAim>(), 0, 0f, Main.myPlayer, 55, NPC.whoAmI);

                    if (NPC.ai[2] == NPC.localAI[1] - 1)
                    {
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                        if (YharimEXGlobalUtilities.HostCheck)
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearAim>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 4);
                    }
                }
            }

            NPC.velocity *= 0.9f;

            if (NPC.ai[1] < 55) //track player up until just before dash
            {
                NPC.localAI[0] = NPC.SafeDirectionTo(player.Center + player.velocity * 30f).ToRotation();
            }

            int endTime = 60;
            if (NPC.ai[2] == NPC.localAI[1] - 1)
                endTime = 80;
            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && (NPC.ai[2] == 0 || NPC.ai[2] >= NPC.localAI[1]))
                endTime = 0;
            if (++NPC.ai[1] > endTime)
            {
                NPC.netUpdate = true;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.ai[3] = 0;
                if (++NPC.ai[2] > NPC.localAI[1])
                {
                    ChooseNextAttack(16, 19, 20, 26, 29, 31, 33, 39, 42, 44, 45);
                }
                else
                {
                    NPC.velocity = NPC.localAI[0].ToRotationVector2() * 45f;
                    float spearAi = 0f;
                    if (NPC.ai[2] == NPC.localAI[1])
                        spearAi = -2f;

                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearDash>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, spearAi);
                    }

                    EdgyBossText(GFBQuote(10));
                }
                NPC.localAI[0] = 0;
            }
        }

        void WhileDashingP2()
        {
            NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);
            if (++NPC.ai[1] > 30)
            {
                if (!AliveCheck(player))
                    return;
                NPC.netUpdate = true;
                AttackChoice--;
                NPC.ai[1] = 0;

                //quickly bounce back towards player
                if (AttackChoice == 14 && NPC.ai[2] == NPC.localAI[1] - 1 && NPC.Distance(player.Center) > 450)
                    NPC.velocity = NPC.SafeDirectionTo(player.Center) * 16f;
            }
        }

        void BoundaryBulletHellP2()
        {
            NPC.velocity = Vector2.Zero;
            if (NPC.localAI[0] == 0)
            {
                NPC.localAI[0] = Math.Sign(NPC.Center.X - player.Center.X);
                //if (YharimEXWorldFlags.MasochistMode) NPC.ai[2] = NPC.SafeDirectionTo(player.Center).ToRotation(); //starting rotation offset to avoid hitting at close range
                if (YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXGlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, -2);

                EdgyBossText(GFBQuote(11));

                if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                    NPC.ai[2] = Main.rand.NextFloat(MathHelper.Pi);
            }
            if (NPC.ai[3] > 60 && ++NPC.ai[1] > 2)
            {
                SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
                NPC.ai[1] = 0;
                NPC.ai[2] += (float)Math.PI / 8 / 480 * NPC.ai[3] * NPC.localAI[0];
                if (NPC.ai[2] > (float)Math.PI)
                    NPC.ai[2] -= (float)Math.PI * 2;
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    int max = 4;
                    if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
                        max += 1;
                    if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                        max += 1;
                    for (int i = 0; i < max; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0f, -6f).RotatedBy(NPC.ai[2] + Math.PI * 2 / max * i),
                            ModContent.ProjectileType<YharimEXEye>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                    }
                }
            }

            int endTime = 360 + 60 + (int)(300 * endTimeVariance);
            if (++NPC.ai[3] > endTime)
            {
                ChooseNextAttack(11, 13, 19, 20, 21, 24, (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 31 : 26, 33, 41, 44);
            }
        }

        void PillarDunk()
        {
            if (!AliveCheck(player))
                return;

            int pillarAttackDelay = 60;

            if (Main.zenithWorld && NPC.ai[1] > 180)
                player.confused = true;

            if (NPC.ai[2] == 0 && NPC.ai[3] == 0) //target one corner of arena
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (YharimEXGlobalUtilities.HostCheck) //spawn cultists
                {
                    void Clone(float ai1, float ai2, float ai3) => YharimEXGlobalUtilities.NewNPCEasy(NPC.GetSource_FromAI(), NPC.Center, ModContent.NPCType<YharimEXIllusion>(), NPC.whoAmI, NPC.whoAmI, ai1, ai2, ai3);
                    Clone(-1, 1, pillarAttackDelay * 4);
                    Clone(1, -1, pillarAttackDelay * 2);
                    Clone(1, 1, pillarAttackDelay * 3);
                    if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                    {
                        Clone(1, 1, pillarAttackDelay * 6);
                        if (Main.getGoodWorld)
                        {
                            Clone(-1, 1, pillarAttackDelay * 7);
                            Clone(1, -1, pillarAttackDelay * 8);
                        }
                    }

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Center, new Vector2(0, -4), ModContent.ProjectileType<YharimEXBrainofConfusion>(), 0, 0, Main.myPlayer);
                }

                EdgyBossText(GFBQuote(12));

                NPC.netUpdate = true;
                NPC.ai[2] = NPC.Center.X;
                NPC.ai[3] = NPC.Center.Y;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<YharimEXRitual>() && Main.projectile[i].ai[1] == NPC.whoAmI)
                    {
                        NPC.ai[2] = Main.projectile[i].Center.X;
                        NPC.ai[3] = Main.projectile[i].Center.Y;
                        break;
                    }
                }

                Vector2 offset = 1000f * Vector2.UnitX.RotatedBy(MathHelper.ToRadians(45));
                if (Main.rand.NextBool()) //always go to a side player isn't in but pick a way to do it randomly
                {
                    if (player.Center.X > NPC.ai[2])
                        offset.X *= -1;
                    if (Main.rand.NextBool())
                        offset.Y *= -1;
                }
                else
                {
                    if (Main.rand.NextBool())
                        offset.X *= -1;
                    if (player.Center.Y > NPC.ai[3])
                        offset.Y *= -1;
                }

                NPC.localAI[1] = NPC.ai[2]; //for illusions
                NPC.localAI[2] = NPC.ai[3];

                NPC.ai[2] = offset.Length();
                NPC.ai[3] = offset.ToRotation();
            }

            Vector2 targetPos = player.Center;
            targetPos.X += NPC.Center.X < player.Center.X ? -700 : 700;
            targetPos.Y += NPC.ai[1] < 240 ? 400 : 150;
            if (NPC.Distance(targetPos) > 50)
                Movement(targetPos, 1f);

            int endTime = 240 + pillarAttackDelay * 4 + 60;
            if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
            {
                endTime += pillarAttackDelay * 2;
                if (Main.getGoodWorld)
                    endTime += 210;
            }

            NPC.localAI[0] = endTime - NPC.ai[1]; //for pillars to know remaining duration
            NPC.localAI[0] += 60f + 60f * (1f - NPC.ai[1] / endTime); //staggered despawn

            if (++NPC.ai[1] > endTime)
            {
                ChooseNextAttack(11, 13, 20, 21, 26, 33, 41, 44/*, 49*/);
            }
            else if (NPC.ai[1] == pillarAttackDelay)
            {
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitY * -5,
                        ModContent.ProjectileType<YharimEXPillar>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0, Main.myPlayer, 3, NPC.whoAmI);
                }
            }
            else if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.ai[1] == pillarAttackDelay * 5)
            {
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitY * -5,
                        ModContent.ProjectileType<YharimEXPillar>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0, Main.myPlayer, 1, NPC.whoAmI);
                }
            }
        }

        void EOCStarSickles()
        {
            if (!AliveCheck(player))
                return;

            if (NPC.ai[1] == 0)
            {
                float ai1 = 0;

                if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) //begin attack much faster
                {
                    ai1 = 30;
                    NPC.ai[1] = 30;
                }

                if (YharimEXGlobalUtilities.HostCheck)
                {
                    int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.UnitY, ModContent.ProjectileType<YharimEXEyeOfCthulhu>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, ai1);
                    if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && p != Main.maxProjectiles)
                        Main.projectile[p].timeLeft -= 30;
                }


            }

            if (NPC.ai[1] < 120) //stop tracking when eoc begins attacking, this locks arena in place
            {
                NPC.ai[2] = player.Center.X;
                NPC.ai[3] = player.Center.Y;
            }

            if (NPC.ai[1] == 120)
            {
                EdgyBossText(GFBQuote(13));
            }

            /*if (NPC.Distance(player.Center) < 200)
            {
                Movement(NPC.Center + 200 * NPC.DirectionFrom(player.Center), 0.9f);
            }
            else
            {*/
            Vector2 targetPos = new(NPC.ai[2], NPC.ai[3]);
            targetPos += NPC.DirectionFrom(targetPos).RotatedBy(MathHelper.ToRadians(-5)) * 450f;
            if (NPC.Distance(targetPos) > 50)
                Movement(targetPos, 0.25f);
            //}

            if (++NPC.ai[1] > 450)
            {
                ChooseNextAttack(11, 13, 16, 21, 26, 29, 31, 33, 35, 37, 41, 44, 45/*, 47*//*, 49*/);
            }

            /*if (Math.Abs(targetPos.X - player.Center.X) < 150) //avoid crossing up player
            {
                targetPos.X = player.Center.X + 150 * Math.Sign(targetPos.X - player.Center.X);
                Movement(targetPos, 0.3f);
            }
            if (NPC.Distance(targetPos) > 50)
            {
                Movement(targetPos, 0.5f);
            }

            if (--NPC.ai[1] < 0)
            {
                NPC.ai[1] = 60;
                if (++NPC.ai[2] > (YharimEXWorldFlags.MasochistMode ? 3 : 1))
                {
                    //float[] options = { 13, 19, 21, 24, 26, 31, 33, 40 }; AttackChoice = options[Main.rand.Next(options.Length)];
                    AttackChoice++;
                    NPC.ai[2] = 0;
                    NPC.TargetClosest();
                }
                else
                {
                    if (YharimEXGlobalUtilities.HostCheck)
                        for (int i = 0; i < 8; i++)
                            Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.UnitX.RotatedBy(Math.PI / 4 * i) * 10f, ModContent.ProjectileType<YharimEXScythe1>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer, NPC.whoAmI);
                    SoundEngine.PlaySound(SoundID.ForceRoarPitched, NPC.Center);
                }
                NPC.netUpdate = true;
                break;
            }*/
        }

        void PrepareSpearDashDirectP2()
        {
            if (NPC.ai[3] == 0)
            {
                if (!AliveCheck(player))
                    return;
                NPC.ai[3] = 1;
                //NPC.velocity = NPC.DirectionFrom(player.Center) * NPC.velocity.Length();
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearSpin>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 180);// + (YharimEXWorldFlags.MasochistMode ? 10 : 20));
                    TelegraphSound = SoundEngine.PlaySound(YharimEXSoundRegistry.YharimEXUnpredictive with { Volume = 2f }, NPC.Center);
                }

                EdgyBossText(GFBQuote(14));
            }

            if (++NPC.ai[1] > 180)
            {
                if (!AliveCheck(player))
                    return;
                NPC.netUpdate = true;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.ai[3] = 0;
                //NPC.TargetClosest();
            }

            Vector2 targetPos = player.Center;
            targetPos.Y += 450f * Math.Sign(NPC.Center.Y - player.Center.Y); //can be above or below
            Movement(targetPos, 0.7f, false);
            if (NPC.Distance(player.Center) < 200)
                Movement(NPC.Center + NPC.DirectionFrom(player.Center), 1.4f);
        }

        void SpearDashDirectP2()
        {
            NPC.velocity *= 0.9f;

            if (NPC.localAI[1] == 0) //max number of attacks
            {
                if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
                    NPC.localAI[1] = Main.rand.Next((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 5, 9);
                else
                    NPC.localAI[1] = 5;
            }

            if (++NPC.ai[1] > ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) ? 5 : 20))
            {
                NPC.netUpdate = true;
                AttackChoice++;
                NPC.ai[1] = 0;
                if (++NPC.ai[2] > NPC.localAI[1])
                {
                    if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                        ChooseNextAttack(11, 13, 16, 19, 20, 31, 33, 35, 39, 42, 44/*, 47*/);
                    else
                        ChooseNextAttack(11, 16, 26, 29, 31, 35, 37, 39, 42, 44/*, 47*/);
                }
                else
                {
                    NPC.velocity = NPC.SafeDirectionTo(player.Center) * ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 60f : 45f);
                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearDash>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);
                    }
                }

                EdgyBossText(GFBQuote(15));
            }
        }

        void SpawnDestroyersForPredictiveThrow()
        {
            if (!AliveCheck(player))
                return;

            if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
            {
                Vector2 targetPos = player.Center + NPC.DirectionFrom(player.Center) * 500;
                if (Math.Abs(targetPos.X - player.Center.X) < 150) //avoid crossing up player
                {
                    targetPos.X = player.Center.X + 150 * Math.Sign(targetPos.X - player.Center.X);
                    Movement(targetPos, 0.3f);
                }
                if (NPC.Distance(targetPos) > 50)
                {
                    Movement(targetPos, 0.9f);
                }
            }
            else
            {
                Vector2 targetPos = player.Center;
                targetPos.X += 500 * (NPC.Center.X < targetPos.X ? -1 : 1);
                if (NPC.Distance(targetPos) > 50)
                {
                    Movement(targetPos, 0.4f);
                }
            }

            if (NPC.localAI[1] == 0) //max number of attacks
            {
                if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
                    NPC.localAI[1] = Main.rand.Next((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 5, 9);
                else
                    NPC.localAI[1] = 5;

                NPC.localAI[2] = Main.rand.Next(2);

                EdgyBossText(GFBQuote(16));
            }

            if (++NPC.ai[1] > 60)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 30;
                int cap = 3;
                if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
                {
                    cap += 2;
                }
                if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                {
                    cap += 2;
                    NPC.ai[1] += 15; //faster
                }

                if (++NPC.ai[2] > cap)
                {
                    //NPC.TargetClosest();
                    AttackChoice++;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath13, NPC.Center);
                    if (YharimEXGlobalUtilities.HostCheck) //spawn worm
                    {
                        Vector2 vel = NPC.DirectionFrom(player.Center).RotatedByRandom(MathHelper.ToRadians(120)) * 10f;
                        float ai1 = 0.8f + 0.4f * NPC.ai[2] / 5f;
                        if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                            ai1 += 0.4f;
                        float appearance = 0;
                        int current = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<YharimEXDestroyerHead>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, ai1, appearance);
                        //timeleft: remaining duration of this case + duration of next case + extra delay after + successive death
                        Main.projectile[current].timeLeft = 30 * (cap - (int)NPC.ai[2]) + 60 * (int)NPC.localAI[1] + 30 + (int)NPC.ai[2] * 6;
                        int max = Main.rand.Next(8, 19);
                        for (int i = 0; i < max; i++)
                            current = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<YharimEXDestroyerBody>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, Main.projectile[current].identity, 0f, appearance);
                        int previous = current;
                        current = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<YharimEXDestroyerTail>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, Main.projectile[current].identity, 0f, appearance);
                        Main.projectile[previous].localAI[1] = Main.projectile[current].identity;
                        Main.projectile[previous].netUpdate = true;
                    }
                }
            }
        }

        void SpearTossPredictiveP2()
        {
            if (!AliveCheck(player))
                return;

            Vector2 targetPos = player.Center;
            targetPos.X += 500 * (NPC.Center.X < targetPos.X ? -1 : 1);
            if (NPC.Distance(targetPos) > 25)
                Movement(targetPos, 0.8f);

            if (++NPC.ai[1] > 60)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 0;
                bool shouldAttack = true;
                if (++NPC.ai[2] > NPC.localAI[1])
                {
                    shouldAttack = false;
                    if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                        ChooseNextAttack(11, 19, 20, 29, 31, 33, 35, 37, 39, 42, 44, 45/*, 47*/);
                    else
                        ChooseNextAttack(11, 19, 20, 26, 26, 26, 29, 31, 33, 35, 37, 39, 42, 44/*, 47*/);
                }

                if ((shouldAttack || (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)) && YharimEXGlobalUtilities.HostCheck)
                {
                    Vector2 vel = NPC.SafeDirectionTo(player.Center + player.velocity * 30f) * 30f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(vel), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.Normalize(vel), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<YharimEXSpearThrown>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, 1f);
                }
            }
            else if (NPC.ai[1] == 1 && (NPC.ai[2] < NPC.localAI[1] || (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)) && YharimEXGlobalUtilities.HostCheck)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center + player.velocity * 30f), ModContent.ProjectileType<YharimEXDeathrayAim>(), 0, 0f, Main.myPlayer, 60f, NPC.whoAmI);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearAim>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 2);
            }
        }

        void PrepareMechRayFan()
        {
            if (NPC.ai[1] == 0)
            {
                if (!AliveCheck(player))
                    return;

                if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                    NPC.ai[1] = 31; //skip the pause, skip the telegraph
            }

            if (NPC.ai[1] == 30)
            {
                SoundEngine.PlaySound(SoundID.ForceRoarPitched, NPC.Center); //eoc roar
                if (YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXGlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, NPCID.Retinazer);

                EdgyBossText(GFBQuote(17));
            }

            Vector2 targetPos;
            if (NPC.ai[1] < 30)
            {
                targetPos = player.Center + NPC.DirectionFrom(player.Center).RotatedBy(MathHelper.ToRadians(15)) * 500f;
                if (NPC.Distance(targetPos) > 50)
                    Movement(targetPos, 0.3f);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    int d = Dust.NewDust(NPC.Center, 0, 0, DustID.Torch, 0f, 0f, 0, default, 3f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 12f;
                }

                targetPos = player.Center;
                targetPos.X += 600 * (NPC.Center.X < targetPos.X ? -1 : 1);
                Movement(targetPos, 1.2f, false);
            }

            if (++NPC.ai[1] > 150 || (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.Distance(targetPos) < 64)
            {
                NPC.netUpdate = true;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                //NPC.TargetClosest();
            }
        }

        void MechRayFan()
        {
            NPC.velocity = Vector2.Zero;

            if (NPC.ai[2] == 0)
            {
                NPC.ai[2] = Main.rand.NextBool() ? -1 : 1; //randomly aim either up or down
            }

            if (NPC.ai[3] == 0 && YharimEXGlobalUtilities.HostCheck)
            {
                int max = 7;
                for (int i = 0; i <= max; i++)
                {
                    Vector2 dir = Vector2.UnitX.RotatedBy(NPC.ai[2] * i * MathHelper.Pi / max) * 6; //rotate initial velocity of telegraphs by 180 degrees depending on velocity of lasers
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + dir, Vector2.Zero, ModContent.ProjectileType<YharimEXGlowything>(), 0, 0f, Main.myPlayer, dir.ToRotation(), NPC.whoAmI, 0f);
                }
            }

            int endTime = 60 + 180 + 150;

            if (NPC.ai[3] > ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 45 : 60) && NPC.ai[3] < 60 + 180 && ++NPC.ai[1] > 10)
            {
                NPC.ai[1] = 0;
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    float rotation = MathHelper.ToRadians(245) * NPC.ai[2] / 80f;
                    int timeBeforeAttackEnds = endTime - (int)NPC.ai[3];

                    void SpawnRay(Vector2 pos, float angleInDegrees, float turnRotation)
                    {
                        int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, MathHelper.ToRadians(angleInDegrees).ToRotationVector2(),
                            ModContent.ProjectileType<YharimEXDeathray3>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0, Main.myPlayer, turnRotation, NPC.whoAmI);
                        if (p != Main.maxProjectiles && Main.projectile[p].timeLeft > timeBeforeAttackEnds)
                            Main.projectile[p].timeLeft = timeBeforeAttackEnds;
                    }
                    ;

                    SpawnRay(NPC.Center, 8 * NPC.ai[2], rotation);
                    SpawnRay(NPC.Center, -8 * NPC.ai[2] + 180, -rotation);

                    if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                    {
                        Vector2 spawnPos = NPC.Center + NPC.ai[2] * -1200 * Vector2.UnitY;
                        SpawnRay(spawnPos, 8 * NPC.ai[2] + 180, rotation);
                        SpawnRay(spawnPos, -8 * NPC.ai[2], -rotation);
                    }
                }
            }

            void SpawnPrime(float varianceInDegrees, float rotationInDegrees)
            {
                SoundEngine.PlaySound(SoundID.Item21, NPC.Center);

                if (YharimEXGlobalUtilities.HostCheck)
                {
                    float spawnOffset = (Main.rand.NextBool() ? -1 : 1) * Main.rand.NextFloat(1400, 1800);
                    float maxVariance = MathHelper.ToRadians(varianceInDegrees);
                    Vector2 aimPoint = NPC.Center - Vector2.UnitY * NPC.ai[2] * 600;
                    Vector2 spawnPos = aimPoint + spawnOffset * Vector2.UnitY.RotatedByRandom(maxVariance).RotatedBy(MathHelper.ToRadians(rotationInDegrees));
                    Vector2 vel = 32f * Vector2.Normalize(aimPoint - spawnPos);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, vel, ModContent.ProjectileType<YharimEXGuardian>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0f, Main.myPlayer);
                }
            }

            if (NPC.ai[3] < 180 && ++NPC.localAI[0] > 1)
            {
                NPC.localAI[0] = 0;
                SpawnPrime(15, 0);
            }

            //if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.ai[3] == endTime - 40)
            //{
            //    Vector2 aimPoint = NPC.Center - Vector2.UnitY * NPC.ai[2] * 600;
            //    for (int i = -3; i <= 3; i++)
            //    {
            //        Vector2 spawnPos = aimPoint + 200 * i * Vector2.UnitX;
            //        if (YharimEXGlobalUtilities.HostCheck)
            //            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<YharimEXReticle2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
            //    }
            //}

            if (++NPC.ai[3] > endTime)
            {
                //if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) //maso prime jumpscare after rays
                //{
                //    for (int i = 0; i < 60; i++)
                //        SpawnPrime(45, 90);
                //}

                if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) //use full moveset
                {
                    ChooseNextAttack(11, 13, 16, 19, 21, 24, 29, 31, 33, 35, 37, 39, 41, 42, 45/*, 47*//*, 49*/);
                }
                else
                {
                    AttackChoice = 11;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                }
                NPC.netUpdate = true;
            }
        }

        void PrepareFishron1()
        {
            if (!AliveCheck(player))
                return;
            Vector2 targetPos = new(player.Center.X, player.Center.Y + 600 * Math.Sign(NPC.Center.Y - player.Center.Y));
            Movement(targetPos, 1.4f, false);

            if (NPC.ai[1] == 0) //always dash towards same side i started on
                NPC.ai[2] = Math.Sign(NPC.Center.X - player.Center.X);

            if (++NPC.ai[1] > 60 || NPC.Distance(targetPos) < 64) //dive here
            {
                NPC.velocity.X = 30f * NPC.ai[2];
                NPC.velocity.Y = 0f;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.netUpdate = true;

                EdgyBossText(GFBQuote(18));
            }
        }

        void SpawnFishrons()
        {
            NPC.velocity *= 0.97f;
            if (NPC.ai[1] == 0)
            {
                NPC.ai[2] = Main.rand.NextBool() ? 1 : 0;
            }
            const int fishronDelay = 3;
            int maxFishronSets = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 2;
            if (NPC.ai[1] % fishronDelay == 0 && NPC.ai[1] <= fishronDelay * maxFishronSets)
            {
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    int projType = NPC.ai[0] == 30 ? ModContent.ProjectileType<YharimEXFishron>() : ModContent.ProjectileType<YharimEXShadowHand>();
                    for (int j = -1; j <= 1; j += 2) //to both sides of player
                    {
                        int max = (int)NPC.ai[1] / fishronDelay;
                        for (int i = -max; i <= max; i++) //fan of fishron
                        {
                            if (Math.Abs(i) != max) //only spawn the outmost ones
                                continue;
                            float spread = MathHelper.Pi / 3 / (maxFishronSets + 1);
                            Vector2 offset = NPC.ai[2] == 0 ? Vector2.UnitY.RotatedBy(spread * i) * -450f * j : Vector2.UnitX.RotatedBy(spread * i) * 475f * j;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, projType, YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, offset.X, offset.Y);
                        }
                    }
                }
                for (int i = 0; i < 30; i++)
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceTorch, 0f, 0f, 0, default, 3f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 12f;
                }
            }

            if (++NPC.ai[1] > ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 60 : 120))
            {
                ChooseNextAttack(13, 19, 20, 21, (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 44 : 26, 31, 31, 31, 33, 35, 39, 41, 42, 44/*, 47*//*, 49*/);
            }
        }

        void PrepareTrueEyeDiveP2()
        {
            if (!AliveCheck(player))
                return;
            Vector2 targetPos = player.Center;
            targetPos.X += 400 * (NPC.Center.X < targetPos.X ? -1 : 1);
            targetPos.Y += 400;
            Movement(targetPos, 1.2f);

            //dive here
            if (++NPC.ai[1] > 60)
            {
                NPC.velocity.X = 30f * (NPC.position.X < player.position.X ? 1 : -1);
                if (NPC.velocity.Y > 0)
                    NPC.velocity.Y *= -1;
                NPC.velocity.Y *= 0.3f;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.netUpdate = true;
            }
        }

        void PrepareNuke()
        {
            if (!AliveCheck(player))
                return;
            Vector2 targetPos = player.Center;
            targetPos.X += 400 * (NPC.Center.X < targetPos.X ? -1 : 1);
            targetPos.Y -= 400;
            Movement(targetPos, 1.2f, false);
            if (++NPC.ai[1] > 60)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    float gravity = 0.2f;
                    float time = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 120f : 180f;
                    Vector2 distance = player.Center - NPC.Center;
                    distance.X /= time;
                    distance.Y = distance.Y / time - 0.5f * gravity * time;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, distance, ModContent.ProjectileType<YharimEXNuke>(), (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 4f / 3f) : 0, 0f, Main.myPlayer, gravity);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXFishronRitual>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0f, Main.myPlayer, NPC.whoAmI);
                }
                AttackChoice++;
                NPC.ai[1] = 0;

                if (Math.Sign(player.Center.X - NPC.Center.X) == Math.Sign(NPC.velocity.X))
                    NPC.velocity.X *= -1f;
                if (NPC.velocity.Y < 0)
                    NPC.velocity.Y *= -1f;
                NPC.velocity.Normalize();
                NPC.velocity *= 3f;

                NPC.netUpdate = true;

                EdgyBossText(GFBQuote(19));
                //NPC.TargetClosest();
            }
        }

        void Nuke()
        {
            if (!AliveCheck(player))
                return;

            Vector2 target = NPC.Bottom.Y < player.Top.Y
                ? player.Center + 300f * Vector2.UnitX * Math.Sign(NPC.Center.X - player.Center.X)
                : NPC.Center + 30 * NPC.DirectionFrom(player.Center).RotatedBy(MathHelper.ToRadians(60) * Math.Sign(player.Center.X - NPC.Center.X));
            Movement(target, 0.1f);
            int maxSpeed = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 2;
            if (NPC.velocity.Length() > maxSpeed)
                NPC.velocity = Vector2.Normalize(NPC.velocity) * maxSpeed;

            if (NPC.ai[1] > ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 120 : 180))
            {
                if (!Main.dedServ && Main.LocalPlayer.active)
                    YharimEXGlobalUtilities.ScreenshakeRumble(6);

                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Vector2 safeZone = NPC.Center;
                    safeZone.Y -= 100;
                    const float safeRange = 150 + 200;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 spawnPos = NPC.Center + Main.rand.NextVector2Circular(1200, 1200);
                        if (Vector2.Distance(safeZone, spawnPos) < safeRange)
                        {
                            Vector2 directionOut = spawnPos - safeZone;
                            directionOut.Normalize();
                            spawnPos = safeZone + directionOut * Main.rand.NextFloat(safeRange, 1200);
                        }
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<YharimEXBomb>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0f, Main.myPlayer);
                    }
                }
            }

            if (++NPC.ai[1] > 360 + 210 * endTimeVariance)
            {
                ChooseNextAttack(11, 13, 16, 19, 24, (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 26 : 29, 31, 35, 37, 39, 41, 42/*, 47*//*, 49*/);
            }

            if (NPC.ai[1] > 45)
            {
                for (int i = 0; i < 20; i++)
                {
                    Vector2 offset = new();
                    offset.Y -= 100;
                    double angle = Main.rand.NextDouble() * 2d * Math.PI;
                    offset.X += (float)(Math.Sin(angle) * 150);
                    offset.Y += (float)(Math.Cos(angle) * 150);
                    Dust dust = Main.dust[Dust.NewDust(NPC.Center + offset - new Vector2(4, 4), 0, 0, DustID.SolarFlare, 0, 0, 100, Color.White, 1.5f)];
                    dust.velocity = NPC.velocity;
                    if (Main.rand.NextBool(3))
                        dust.velocity += Vector2.Normalize(offset) * 5f;
                    dust.noGravity = true;
                }
            }
        }

        void PrepareSlimeRain()
        {
            if (!AliveCheck(player))
                return;
            Vector2 targetPos = player.Center;
            targetPos.X += 700 * (NPC.Center.X < targetPos.X ? -1 : 1);
            targetPos.Y += 200;
            Movement(targetPos, 2f);

            if (++NPC.ai[2] > 30 || (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.Distance(targetPos) < 64)
            {
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                NPC.netUpdate = true;
                //NPC.TargetClosest();

                EdgyBossText(GFBQuote(20));
            }
        }

        void SlimeRain()
        {
            if (NPC.ai[3] == 0)
            {
                NPC.ai[3] = 1;
                //Main.NewText(NPC.position.Y);
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSlimeRain>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);
            }

            if (NPC.ai[1] == 0) //telegraphs for where slime will fall
            {
                bool first = NPC.localAI[0] == 0;
                NPC.localAI[0] = Main.rand.Next(5, 9) * 120;
                if (first) //always start on the same side as the player
                {
                    if (player.Center.X < NPC.Center.X && NPC.localAI[0] > 1200)
                        NPC.localAI[0] -= 1200;
                    else if (player.Center.X > NPC.Center.X && NPC.localAI[0] < 1200)
                        NPC.localAI[0] += 1200;
                }
                else //after that, always be on opposite side from player
                {
                    if (player.Center.X < NPC.Center.X && NPC.localAI[0] < 1200)
                        NPC.localAI[0] += 1200;
                    else if (player.Center.X > NPC.Center.X && NPC.localAI[0] > 1200)
                        NPC.localAI[0] -= 1200;
                }
                NPC.localAI[0] += 60;

                Vector2 basePos = NPC.Center;
                basePos.X -= 1200;
                for (int i = -360; i <= 2760; i += 120) //spawn telegraphs
                {
                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        if (i + 60 == (int)NPC.localAI[0])
                            continue;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), basePos.X + i + 60, basePos.Y, 0f, 0f, ModContent.ProjectileType<YharimEXReticle>(), 0, 0f, Main.myPlayer);
                    }
                }

                if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                {
                    NPC.ai[1] += 20; //less startup
                    NPC.ai[2] += 20; //stay synced
                }
            }

            if (NPC.ai[1] > 120 && NPC.ai[1] % 5 == 0) //rain down slime balls
            {
                SoundEngine.PlaySound(SoundID.Item34, player.Center);
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    void Slime(Vector2 pos, float off, Vector2 vel)
                    {
                        //dont flip in maso wave 3
                        int flip = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.ai[2] < 180 * 2 && Main.rand.NextBool() ? -1 : 1;
                        Vector2 spawnPos = pos + off * Vector2.UnitY * flip;
                        float ai0 = YharimEXGlobalUtilities.ProjectileExists(ritualProj, ModContent.ProjectileType<YharimEXRitual>()) == null ? 0f : NPC.Distance(Main.projectile[ritualProj].Center);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, vel * flip * 2 /* x2 to compensate for removed extraUpdates */, ModContent.ProjectileType<YharimEXSlimeBall>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0);
                    }

                    Vector2 basePos = NPC.Center;
                    basePos.X -= 1200;
                    float yOffset = -1300;

                    const float safeRange = 110;
                    for (int i = -360; i <= 2760; i += 75)
                    {
                        float xOffset = i + Main.rand.Next(75);
                        if (Math.Abs(xOffset - NPC.localAI[0]) < safeRange) //dont fall over safespot
                            continue;

                        Vector2 spawnPos = basePos;
                        spawnPos.X += xOffset;
                        Vector2 velocity = Vector2.UnitY * Main.rand.NextFloat(15f, 20f);

                        Slime(spawnPos, yOffset, velocity);
                    }

                    //spawn right on safespot borders
                    Slime(basePos + Vector2.UnitX * (NPC.localAI[0] + safeRange), yOffset, Vector2.UnitY * 20f);
                    Slime(basePos + Vector2.UnitX * (NPC.localAI[0] - safeRange), yOffset, Vector2.UnitY * 20f);
                }
            }
            if (++NPC.ai[1] > 180)
            {
                if (!AliveCheck(player))
                    return;
                NPC.ai[1] = 0;
            }

            const int masoMovingRainAttackTime = 180 * 3 - 60;
            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.ai[1] == 120 && NPC.ai[2] < masoMovingRainAttackTime && Main.rand.NextBool(3))
                NPC.ai[2] = masoMovingRainAttackTime;

            NPC.velocity = Vector2.Zero;

            const int timeToMove = 240;
            if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
            {
                if (NPC.ai[2] == masoMovingRainAttackTime)
                {
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                    EdgyBossText(GFBQuote(21));
                }


                if (NPC.ai[2] > masoMovingRainAttackTime + 30)
                {
                    if (NPC.ai[1] > 170) //let the balls keep falling
                        NPC.ai[1] -= 30;

                    if (NPC.localAI[1] == 0) //direction to move safespot towards
                    {
                        float safespotX = NPC.Center.X - 1200f + NPC.localAI[0];
                        NPC.localAI[1] = Math.Sign(NPC.Center.X - safespotX);
                    }

                    //move the safespot
                    //NPC.localAI[0] += 1000f / timeToMove * NPC.localAI[1];

                    NPC.Center += Vector2.UnitX * 1000f / timeToMove * NPC.localAI[1]; //move along with the movement
                }
            }

            int endTime = 180 * 3;
            if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                endTime += timeToMove + (int)(300 * endTimeVariance) - 30;
            if (++NPC.ai[2] > endTime)
            {
                ChooseNextAttack(11, 16, 19, 20, (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 26 : 29, 31, 33, 37, 39, 41, 42, 45);
            }
        }

        void QueenSlimeRain()
        {
            if (NPC.ai[3] == 0)
            {
                NPC.ai[3] = 1;
                //Main.NewText(NPC.position.Y);
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSlimeRain>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);
            }

            if (NPC.ai[1] == 0) //telegraphs for where slime will fall
            {
                NPC.localAI[0] = Main.rand.Next(6, 9) * 120;
                //always start on the same side as the player
                if (player.Center.X > NPC.Center.X)
                    NPC.localAI[0] += 600;
                NPC.localAI[0] += 60;

                Vector2 basePos = NPC.Center;
                basePos.X -= 1200;
                for (int i = -360; i <= 2760; i += 120) //spawn telegraphs
                {
                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        if (i + 60 == (int)NPC.localAI[0])
                            continue;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), basePos.X + i + 60, basePos.Y, 0f, 0f, ModContent.ProjectileType<YharimEXReticle>(), 0, 0f, Main.myPlayer, ai2: 1);
                    }
                }
            }

            const int masoMovingRainAttackTime = 60;

            if (NPC.ai[1] > masoMovingRainAttackTime && NPC.ai[1] % 3 == 0) //rain down slime balls
            {
                SoundEngine.PlaySound(SoundID.Item34, player.Center);
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    int frame = Main.rand.Next(3);

                    void Slime(Vector2 pos, float off, Vector2 vel)
                    {
                        const int flip = 1;
                        Vector2 spawnPos = pos + off * Vector2.UnitY * flip;
                        float ai0 = YharimEXGlobalUtilities.ProjectileExists(ritualProj, ModContent.ProjectileType<YharimEXRitual>()) == null ? 0f : NPC.Distance(Main.projectile[ritualProj].Center);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, vel * flip, ModContent.ProjectileType<YharimEXSlimeSpike>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0, ai2: frame);
                    }

                    Vector2 basePos = NPC.Center;
                    basePos.X -= 1200;
                    float yOffset = -1300;

                    const int safeRange = 110;
                    const int spacing = safeRange;
                    for (int i = 0; i < 2400; i += spacing)
                    {
                        float rightOffset = NPC.localAI[0] + safeRange + i;
                        if (basePos.X + rightOffset < NPC.Center.X + 1200)
                            Slime(basePos + Vector2.UnitX * rightOffset, yOffset, Vector2.UnitY * 20f);
                        float leftOffset = NPC.localAI[0] - safeRange - i;
                        if (basePos.X + leftOffset > NPC.Center.X - 1200)
                            Slime(basePos + Vector2.UnitX * leftOffset, yOffset, Vector2.UnitY * 20f);
                    }
                }
            }

            NPC.velocity = Vector2.Zero;

            const int timeToMove = 360;
            if (NPC.ai[1] == masoMovingRainAttackTime)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                EdgyBossText(GFBQuote(21));
            }

            if (NPC.ai[1] > masoMovingRainAttackTime && --NPC.ai[2] < 0)
            {
                float safespotMoveSpeed = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 7f : 6f;

                if (--NPC.localAI[2] < 0) //reset and recalibrate for the other direction
                {
                    float safespotX = NPC.Center.X - 1200f + NPC.localAI[0];
                    NPC.localAI[1] = Math.Sign(NPC.Center.X - safespotX); //direction to move safespot towards

                    float farSideArenaBorder = NPC.Center.X + 1200f * NPC.localAI[1];
                    float distanceToBorder = Math.Abs(farSideArenaBorder - safespotX);
                    float minRequiredDistance = Math.Abs(NPC.Center.X - safespotX) + 100;

                    float distanceToTravel = MathHelper.Lerp(minRequiredDistance, distanceToBorder, Main.rand.NextFloat(0.6f));

                    NPC.localAI[2] = distanceToTravel / safespotMoveSpeed;
                    NPC.ai[2] = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 15 : 30; //adds a pause when turning around
                }

                //move the safespot
                NPC.localAI[0] += safespotMoveSpeed * NPC.localAI[1];
            }

            int endTime = masoMovingRainAttackTime + timeToMove + (int)(300 * endTimeVariance);
            if (++NPC.ai[1] > endTime)
            {
                ChooseNextAttack(11, 16, 19, 20, (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 26 : 29, 31, 33, 37, 39, 41, 42, 45);
            }
        }

        void PrepareFishron2()
        {
            if (!AliveCheck(player))
                return;

            Vector2 targetPos = player.Center;
            targetPos.X += 400 * (NPC.Center.X < targetPos.X ? -1 : 1);
            targetPos.Y -= 400;
            Movement(targetPos, 0.9f);

            if (++NPC.ai[1] > 60 || (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.Distance(targetPos) < 32) //dive here
            {
                NPC.velocity.X = 35f * (NPC.position.X < player.position.X ? 1 : -1);
                NPC.velocity.Y = 10f;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.netUpdate = true;
                //NPC.TargetClosest();

                EdgyBossText(GFBQuote(18));
            }
        }

        void PrepareOkuuSpheresP2()
        {
            if (!AliveCheck(player))
                return;
            Vector2 targetPos = player.Center + player.SafeDirectionTo(NPC.Center) * 450;
            if (++NPC.ai[1] < 180 && NPC.Distance(targetPos) > 50)
            {
                Movement(targetPos, 0.8f);
            }
            else
            {
                NPC.netUpdate = true;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
            }
        }

        void OkuuSpheresP2()
        {
            NPC.velocity = Vector2.Zero;

            int endTime = 420 + (int)(300 * endTimeVariance);

            if (++NPC.ai[1] > 10 && NPC.ai[3] > 60 && NPC.ai[3] < endTime - 60)
            {
                NPC.ai[1] = 0;
                float rotation = MathHelper.ToRadians(60) * (NPC.ai[3] - 45) / 240 * NPC.ai[2];
                int max = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 10 : 9;
                float speed = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 11f : 10f;
                SpawnSphereRing(max, speed, YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), -1f, rotation);
                SpawnSphereRing(max, speed, YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 1f, rotation);


            }

            if (NPC.ai[2] == 0)
            {
                NPC.ai[2] = Main.rand.NextBool() ? -1 : 1;
                NPC.ai[3] = Main.rand.NextFloat((float)Math.PI * 2);
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                if (YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXGlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, -2);

                EdgyBossText(GFBQuote(22));
            }

            if (++NPC.ai[3] > endTime)
            {
                ChooseNextAttack(13, 19, 20, (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 13 : 26, (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 44 : 33, 41, 44/*, 49*/);
            }

            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.SolarFlare, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }
        }

        void SpawnSpearTossDirectP2Attack()
        {
            if (YharimEXGlobalUtilities.HostCheck)
            {
                Vector2 vel = NPC.SafeDirectionTo(player.Center) * 30f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(vel), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.Normalize(vel), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<YharimEXSpearThrown>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target);
            }

            EdgyBossText(RandomObnoxiousQuote());
        }

        void SpearTossDirectP2()
        {
            if (!AliveCheck(player))
                return;

            if (NPC.ai[1] == 0)
            {
                NPC.localAI[0] = MathHelper.WrapAngle((NPC.Center - player.Center).ToRotation()); //remember initial angle offset

                //random max number of attacks
                if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
                    NPC.localAI[1] = Main.rand.Next((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 5, 9);
                else
                    NPC.localAI[1] = 5;

                if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                {
                    NPC.localAI[1] += Main.rand.Next(6);
                    if (Main.getGoodWorld)
                        NPC.localAI[1] += 5;
                }
                NPC.localAI[2] = Main.rand.NextBool() ? -1 : 1; //pick a random rotation direction
                NPC.netUpdate = true;
            }

            //slowly rotate in full circle around player
            Vector2 targetPos = player.Center + 500f * Vector2.UnitX.RotatedBy(MathHelper.TwoPi / 300 * NPC.ai[3] * NPC.localAI[2] + NPC.localAI[0]);
            if (NPC.Distance(targetPos) > 25)
                Movement(targetPos, 0.6f);

            ++NPC.ai[3]; //for keeping track of how much time has actually passed (ai1 jumps around)

            if (++NPC.ai[1] > 180)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 150;

                bool shouldAttack = true;
                if (++NPC.ai[2] > NPC.localAI[1])
                {
                    if (Main.getGoodWorld) // Can't combo into slime rain in ftw
                        ChooseNextAttack(11, 16, 19, 20, (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 44 : 26, 31, 33, /*35,*/ 42, 44, 45/*, 47*/);
                    else
                        ChooseNextAttack(11, 16, 19, 20, (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 44 : 26, 31, 33, 35, 42, 44, 45/*, 47*/);
                    shouldAttack = false;
                }

                if (shouldAttack || (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                {
                    SpawnSpearTossDirectP2Attack();
                }
            }
            else if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.ai[1] == 165)
            {
                SpawnSpearTossDirectP2Attack();
            }
            else if (NPC.ai[1] == 151)
            {
                if (NPC.ai[2] > 0 && (NPC.ai[2] < NPC.localAI[1] || (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)) && YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearAim>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 1);
            }
            else if (NPC.ai[1] == 1)
            {
                if (YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearAim>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, -1);
            }
        }

        void PrepareTwinRangsAndCrystals()
        {
            if (!AliveCheck(player))
                return;
            Vector2 targetPos = player.Center;
            targetPos.X += 500 * (NPC.Center.X < targetPos.X ? -1 : 1);
            if (NPC.Distance(targetPos) > 50)
                Movement(targetPos, 0.8f);
            if (++NPC.ai[1] > 45)
            {
                NPC.netUpdate = true;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                //NPC.TargetClosest();

                EdgyBossText(GFBQuote(23));
            }
        }

        void TwinRangsAndCrystals()
        {
            NPC.velocity = Vector2.Zero;

            if (NPC.ai[3] == 0)
            {
                NPC.localAI[0] = NPC.DirectionFrom(player.Center).ToRotation();

                if (!(YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && YharimEXGlobalUtilities.HostCheck)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + Vector2.UnitX.RotatedBy(Math.PI / 2 * i) * 525, Vector2.Zero, ModContent.ProjectileType<YharimEXGlowRingHollow>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 1f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + Vector2.UnitX.RotatedBy(Math.PI / 2 * i + Math.PI / 4) * 350, Vector2.Zero, ModContent.ProjectileType<YharimEXGlowRingHollow>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 2f);
                    }
                }
            }

            int ringDelay = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 12 : 15;
            int ringMax = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 5 : 4;
            if (NPC.ai[3] % ringDelay == 0 && NPC.ai[3] < ringDelay * ringMax)
            {
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    float rotationOffset = MathHelper.TwoPi / ringMax * NPC.ai[3] / ringDelay + NPC.localAI[0];
                    int baseDelay = 60;
                    float flyDelay = 120 + NPC.ai[3] / ringDelay * ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 40 : 50);
                    int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, 300f / baseDelay * Vector2.UnitX.RotatedBy(rotationOffset), ModContent.ProjectileType<YharimEXMark2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, baseDelay, baseDelay + flyDelay);
                    if (p != Main.maxProjectiles)
                    {
                        const int max = 5;
                        const float distance = 125f;
                        float rotation = MathHelper.TwoPi / max;
                        for (int i = 0; i < max; i++)
                        {
                            float myRot = rotation * i + rotationOffset;
                            Vector2 spawnPos = NPC.Center + new Vector2(distance, 0f).RotatedBy(myRot);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<YharimEXCrystalLeaf>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, Main.projectile[p].identity, myRot);
                        }
                    }
                }
            }

            if (NPC.ai[3] > 45 && --NPC.ai[1] < 0)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 20;
                NPC.ai[2] = NPC.ai[2] > 0 ? -1 : 1;

                SoundEngine.PlaySound(SoundID.Item92, NPC.Center);

                if (YharimEXGlobalUtilities.HostCheck && NPC.ai[3] < 330)
                {
                    const float retiRad = 525;
                    const float spazRad = 350;
                    float retiSpeed = 2 * (float)Math.PI * retiRad / 300;
                    float spazSpeed = 2 * (float)Math.PI * spazRad / 180;
                    float retiAcc = retiSpeed * retiSpeed / retiRad * NPC.ai[2];
                    float spazAcc = spazSpeed * spazSpeed / spazRad * -NPC.ai[2];
                    float rotationOffset = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? MathHelper.PiOver4 : 0;
                    for (int i = 0; i < 4; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitX.RotatedBy(Math.PI / 2 * i + rotationOffset) * retiSpeed, ModContent.ProjectileType<YharimEXRetirang>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, retiAcc, 300);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitX.RotatedBy(Math.PI / 2 * i + Math.PI / 4 + rotationOffset) * spazSpeed, ModContent.ProjectileType<YharimEXSpazmarang>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, spazAcc, 180);
                    }
                }
            }
            if (++NPC.ai[3] > 450)
            {
                ChooseNextAttack(11, 13, 16, 21, 24, 26, 29, 31, 33, 35, 39, 41, 44, 45/*, 47*//*, 49*/);
            }
        }

        void EmpressSwordWave()
        {
            if (!AliveCheck(player))
                return;

            if (!(YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
            {
                AttackChoice++; //dont do this attack in expert
                return;
            }

            //Vector2 targetPos = player.Center + 360 * NPC.DirectionFrom(player.Center).RotatedBy(MathHelper.ToRadians(10)); Movement(targetPos, 0.25f);
            NPC.velocity = Vector2.Zero;

            int attackThreshold = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 48 : 60;
            int timesToAttack = 4 + (int)Math.Round(3 * endTimeVariance);
            int startup = 90;

            if (NPC.ai[1] == 0)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                NPC.ai[3] = Main.rand.NextFloat(MathHelper.TwoPi);

                EdgyBossText(GFBQuote(24));
            }

            void Sword(Vector2 pos, float ai0, float ai1, Vector2 vel)
            {
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), pos - vel * 60f, vel,
                        ProjectileID.FairyQueenLance, YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0, ai1);
                }
            }

            if (NPC.ai[1] >= startup && NPC.ai[1] < startup + attackThreshold * timesToAttack && --NPC.ai[2] < 0) //walls of swords
            {
                NPC.ai[2] = attackThreshold;

                SoundEngine.PlaySound(SoundID.Item163, player.Center);

                if (Math.Abs(MathHelper.WrapAngle(NPC.DirectionFrom(player.Center).ToRotation() - NPC.ai[3])) > MathHelper.PiOver2)
                    NPC.ai[3] += MathHelper.Pi; //swords always spawn closer to player

                const int maxHorizSpread = 1600 * 2;
                const int arenaRadius = 1200;
                int max = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 16 : 12;
                float gap = maxHorizSpread / max;

                float attackAngle = NPC.ai[3];// + Main.rand.NextFloat(MathHelper.ToDegrees(10)) * (Main.rand.NextBool() ? -1 : 1);
                Vector2 spawnOffset = -attackAngle.ToRotationVector2();

                //start by focusing on player
                Vector2 focusPoint = player.Center;

                //move focus point along grid closer so attack stays centered
                Vector2 home = NPC.Center;// YharimEXGlobalUtilities.ProjectileExists(ritualProj, ModContent.ProjectileType<YharimEXRitual>()) == null ? NPC.Center : Main.projectile[ritualProj].Center;
                for (float i = 0; i < arenaRadius; i += gap)
                {
                    Vector2 newFocusPoint = focusPoint + gap * attackAngle.ToRotationVector2();
                    if ((home - newFocusPoint).Length() > (home - focusPoint).Length())
                        break;
                    focusPoint = newFocusPoint;
                }

                //doing it this way to guarantee it always remains aligned to grid
                float spawnDistance = 0;
                while (spawnDistance < arenaRadius)
                    spawnDistance += gap;

                float mirrorLength = 2f * (float)Math.Sqrt(2f * spawnDistance * spawnDistance);
                int swordCounter = 0;
                for (int i = -max; i <= max; i++)
                {
                    Vector2 spawnPos = focusPoint + spawnOffset * spawnDistance + spawnOffset.RotatedBy(MathHelper.PiOver2) * gap * i;
                    float Ai1 = swordCounter++ / (max * 2f + 1);

                    Vector2 randomOffset = Main.rand.NextVector2Unit();
                    if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                    {
                        if (randomOffset.Length() < 0.5f)
                            randomOffset = 0.5f * randomOffset.SafeNormalize(Vector2.UnitX);
                        randomOffset *= 2f;
                    }

                    Sword(spawnPos, attackAngle + MathHelper.PiOver4, Ai1, randomOffset);
                    Sword(spawnPos, attackAngle - MathHelper.PiOver4, Ai1, randomOffset);

                    if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                    {
                        Sword(spawnPos + mirrorLength * (attackAngle + MathHelper.PiOver4).ToRotationVector2(), attackAngle + MathHelper.PiOver4 + MathHelper.Pi, Ai1, randomOffset);
                        Sword(spawnPos + mirrorLength * (attackAngle - MathHelper.PiOver4).ToRotationVector2(), attackAngle - MathHelper.PiOver4 + MathHelper.Pi, Ai1, randomOffset);
                    }
                }

                NPC.ai[3] += MathHelper.PiOver4 * (Main.rand.NextBool() ? -1 : 1) //rotate 90 degrees
                    + Main.rand.NextFloat(MathHelper.PiOver4 / 2) * (Main.rand.NextBool() ? -1 : 1); //variation

                NPC.netUpdate = true;
            }

            void MegaSwordSwarm(Vector2 target)
            {
                SoundEngine.PlaySound(SoundID.Item164, player.Center);

                float safeAngle = NPC.ai[3];
                float safeRange = MathHelper.ToRadians(10);
                int max = 60;
                for (int i = 0; i < max; i++)
                {
                    float rotationOffset = Main.rand.NextFloat(safeRange, MathHelper.Pi - safeRange);
                    Vector2 offset = Main.rand.NextFloat(600f, 2400f) * (safeAngle + rotationOffset).ToRotationVector2();
                    if (Main.rand.NextBool())
                        offset *= -1;

                    //if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) //block one side so only one real exit exists
                    //    target += Main.rand.NextFloat(600) * safeAngle.ToRotationVector2();

                    Vector2 spawnPos = target + offset;
                    Vector2 vel = (target - spawnPos) / 60f;
                    Sword(spawnPos, vel.ToRotation(), (float)i / max, -vel * 0.75f);
                }
                EdgyBossText(GFBQuote(25)); //you really didn't
            }

            //massive sword barrage
            int swordSwarmTime = startup + attackThreshold * timesToAttack + 40;
            if (NPC.ai[1] == swordSwarmTime)
            {
                MegaSwordSwarm(player.Center);
                NPC.localAI[0] = player.Center.X;
                NPC.localAI[1] = player.Center.Y;
            }

            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.ai[1] == swordSwarmTime + 30)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    MegaSwordSwarm(new Vector2(NPC.localAI[0], NPC.localAI[1]) + 600 * i * NPC.ai[3].ToRotationVector2());
                }
            }

            if (++NPC.ai[1] > swordSwarmTime + ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 60 : 30))
            {
                ChooseNextAttack(11, 13, 16, 21, (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 26 : 24, 29, 31, 35, 37, 39, 41, 45/*, 47*//*, 49*/);
            }
        }

        void SANSGOLEM()
        {
            Vector2 targetPos = player.Center + NPC.DirectionFrom(player.Center) * 300;
            Movement(targetPos, 0.3f);

            int attackDelay = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 50 : 70;

            if (NPC.ai[1] > 0 && NPC.ai[1] % attackDelay == 0)
            {
                EdgyBossText(GFBQuote(35));

                float oldOffset = NPC.ai[2];
                while (NPC.ai[2] == oldOffset)
                    NPC.ai[2] = Main.rand.Next(-1, 2); //roll -1, 0, 1

                Vector2 centerPoint = YharimEXGlobalUtilities.ProjectileExists(ritualProj, ModContent.ProjectileType<YharimEXRitual>()) == null ? player.Center : Main.projectile[ritualProj].Center;
                float maxVariance = 150; //variance seems a LOT more than this, whatever
                float maxOffsetWithinStep = maxVariance / 3 * .75f; //x.75 so player always has to move a noticeable amount
                centerPoint.Y += maxVariance * NPC.ai[2]; //choose one of 3 base heights
                centerPoint.Y += Main.rand.NextFloat(-maxOffsetWithinStep, maxOffsetWithinStep);

                for (int i = -1; i <= 1; i += 2) //left and right
                {
                    float xSpeedWhenAttacking = Main.rand.NextFloat(8f, 20f);

                    for (int j = -1; j <= 1; j += 2) //flappy bird tubes
                    {
                        float gapRadiusHeight = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 120 : 150;
                        Vector2 sansTargetPos = centerPoint;
                        const int timeToReachMiddle = 60;
                        sansTargetPos.X += xSpeedWhenAttacking * timeToReachMiddle * i;
                        sansTargetPos.Y += gapRadiusHeight * j;

                        int travelTime = 50;
                        Vector2 vel = (sansTargetPos - NPC.Center) / travelTime;

                        if (YharimEXGlobalUtilities.HostCheck)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                                ModContent.ProjectileType<YharimEXGolemHead>(),
                                YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer,
                                travelTime, xSpeedWhenAttacking * -i, j);
                        }
                    }
                }
            }

            //(attacks + 1) - 5 to stop mutant from doing the last one, give a gap to next attack
            //doing the math round to make the endtimes discrete
            const int attacksToDo = 6;
            int endTime = attackDelay * (attacksToDo + 1) - 5 + attackDelay * (int)Math.Round(4 * endTimeVariance);
            if (++NPC.ai[1] > endTime)
            {
                ChooseNextAttack(13, 19, 20, 21, 24, 31, 33, 35, 41, 44);
            }
        }

        void P2NextAttackPause() //choose next attack but actually, this also gives breathing space for mp to sync up
        {
            if (!AliveCheck(player))
                return;

            EModeSpecialEffects(); //manage these here, for case where players log out/rejoin in mp

            Vector2 targetPos = player.Center + NPC.DirectionFrom(player.Center) * 400;
            Movement(targetPos, 0.3f);
            if (NPC.Distance(targetPos) > 200) //faster if offscreen
                Movement(targetPos, 0.3f);

            if (++NPC.ai[1] > 60 || NPC.Distance(targetPos) < 200 && NPC.ai[1] > (NPC.localAI[3] >= 3 ? 15 : 30))
            {
                /*EModeGlobalNPC.PrintAI(npc);
                string output = "";
                foreach (float attack in attackHistory)
                    output += attack.ToString() + " ";
                Main.NewText(output);*/

                NPC.velocity *= (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 0.25f : 0.75f;

                //NPC.TargetClosest();
                AttackChoice = NPC.ai[2];
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.netUpdate = true;

                EdgyBossText(RandomObnoxiousQuote());
            }
        }
    }
}