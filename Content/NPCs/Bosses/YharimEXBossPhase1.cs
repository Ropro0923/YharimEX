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
    public partial class YharimEXBoss
    {
        void SpearTossDirectP1AndChecks()
        {
            if (!AliveCheck(player))
                return;
            if (Phase2Check())
                return;
            NPC.localAI[2] = 0;
            Vector2 targetPos = player.Center;
            targetPos.X += 500 * (NPC.Center.X < targetPos.X ? -1 : 1);
            if (NPC.Distance(targetPos) > 50)
            {
                Movement(targetPos, NPC.localAI[3] > 0 ? 0.5f : 2f, true, NPC.localAI[3] > 0);
            }

            if (NPC.ai[3] == 0)
            {
                NPC.ai[3] = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? Main.rand.Next(2, 8) : 5;
                NPC.netUpdate = true;
            }

            if (NPC.localAI[3] > 0) //dont begin proper ai timer until in range to begin fight
                NPC.ai[1]++;

            if (NPC.ai[1] < 145) //track player up until just before attack
            {
                NPC.localAI[0] = NPC.SafeDirectionTo(player.Center + player.velocity * 30f).ToRotation();
            }

            if (NPC.ai[1] > 150) //120)
            {
                NPC.netUpdate = true;
                //NPC.TargetClosest();
                NPC.ai[1] = 60;
                if (++NPC.ai[2] > NPC.ai[3])
                {
                    P1NextAttackOrMasoOptions(AttackChoice);
                    NPC.velocity = NPC.SafeDirectionTo(player.Center) * 2f;
                }
                else if (YharimEXGlobalUtilities.HostCheck)
                {
                    Vector2 vel = NPC.localAI[0].ToRotationVector2() * 25f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<YharimEXSpearThrown>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target);
                    if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(vel), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.Normalize(vel), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                    }
                }
                NPC.localAI[0] = 0;
            }
            else if (NPC.ai[1] == 61 && NPC.ai[2] < NPC.ai[3] && YharimEXGlobalUtilities.HostCheck)
            {
                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) && YharimEXWorldFlags.SkipYharimEXP1 >= 10 && !(YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                {
                    AttackChoice = 10; //skip to phase 2
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                    NPC.localAI[0] = 0;
                    NPC.netUpdate = true;

                    if (YharimEXWorldFlags.SkipYharimEXP1 == 10)
                        YharimEXGlobalUtilities.PrintLocalization($"Mods.{Mod.Name}.NPCs.YharimEXBoss.SkipPhase1", Color.OrangeRed);

                    if (YharimEXWorldFlags.SkipYharimEXP1 >= 10)
                        NPC.ai[2] = 1; //flag for different p2 transition animation

                    return;
                }

                if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.ai[2] == 0) //first time only
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath13, NPC.Center);
                    if (YharimEXGlobalUtilities.HostCheck) //spawn worm
                    {
                        int appearance = 0;
                        for (int j = 0; j < 8; j++)
                        {
                            Vector2 vel = NPC.DirectionFrom(player.Center).RotatedByRandom(MathHelper.ToRadians(120)) * 10f;
                            float ai1 = 0.8f + 0.4f * j / 5f;
                            int current = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<YharimEXDestroyerHead>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, ai1, appearance);
                            //timeleft: remaining duration of this case + extra delay after + successive death
                            Main.projectile[current].timeLeft = 90 * ((int)NPC.ai[3] + 1) + 30 + j * 6;
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



                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center + player.velocity * 30f), ModContent.ProjectileType<YharimEXDeathrayAim>(), 0, 0f, Main.myPlayer, 85f, NPC.whoAmI);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearAim>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 3);

            }
        }
        void OkuuSpheresP1()
        {
            if (Phase2Check())
                return;

            if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                NPC.velocity = Vector2.Zero;
            if (--NPC.ai[1] < 0)
            {
                NPC.netUpdate = true;
                float modifier = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 1;
                NPC.ai[1] = 90 / modifier;
                if (++NPC.ai[2] > 4 * modifier)
                {
                    if (!(YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) || NPC.ai[2] > 6 * modifier) //extra endtime in maso
                    {
                        P1NextAttackOrMasoOptions(AttackChoice);
                    }

                }
                else
                {
                    EdgyBossText(RandomObnoxiousQuote());

                    int max = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 9 : 6;
                    float speed = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 10 : 9;
                    int sign = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? NPC.ai[2] % 2 == 0 ? 1 : -1 : 1;
                    SpawnSphereRing(max, speed, (int)(0.8 * YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage)), 1f * sign);
                    SpawnSphereRing(max, speed, (int)(0.8 * YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage)), -0.5f * sign);
                }

            }
        }
        void PrepareTrueEyeDiveP1()
        {
            if (!AliveCheck(player))
                return;
            if (Phase2Check())
                return;
            Vector2 targetPos = player.Center;
            targetPos.X += 700 * (NPC.Center.X < targetPos.X ? -1 : 1);
            targetPos.Y -= 400;
            Movement(targetPos, 0.6f);
            if (NPC.Distance(targetPos) < 50 || ++NPC.ai[1] > 180) //dive here
            {
                NPC.velocity.X = 35f * (NPC.position.X < player.position.X ? 1 : -1);
                if (NPC.velocity.Y < 0)
                    NPC.velocity.Y *= -1;
                NPC.velocity.Y *= 0.3f;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                EdgyBossText(RandomObnoxiousQuote());
            }
        }
        void TrueEyeDive()
        {
            if (NPC.ai[3] == 0)
                NPC.ai[3] = Math.Sign(NPC.Center.X - player.Center.X);

            if (NPC.ai[2] > 3)
            {
                Vector2 targetPos = player.Center;
                targetPos.X += NPC.Center.X < player.Center.X ? -500 : 500;
                if (NPC.Distance(targetPos) > 50)
                    Movement(targetPos, 0.3f);
            }
            else
            {
                NPC.velocity *= 0.99f;
            }

            if (--NPC.ai[1] < 0)
            {
                NPC.ai[1] = 15;
                int maxEyeThreshold = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 6 : 3;
                int endlag = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 5;
                if (++NPC.ai[2] > maxEyeThreshold + endlag)
                {
                    if (AttackChoice == 3)
                        P1NextAttackOrMasoOptions(2);
                    else
                        ChooseNextAttack(13, 19, 21, 24, 33, 33, 33, 39, 41, 44);
                }
                else if (NPC.ai[2] <= maxEyeThreshold)
                {
                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        int type;
                        float ratio = NPC.ai[2] / maxEyeThreshold * 3;
                        if (ratio <= 1f)
                            type = ModContent.ProjectileType<YharimEXTrueEyeL>();
                        else if (ratio <= 2f)
                            type = ModContent.ProjectileType<YharimEXTrueEyeS>();
                        else
                            type = ModContent.ProjectileType<YharimEXTrueEyeR>();

                        int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, type, YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 0.8f), 0f, Main.myPlayer, NPC.target);
                        if (p != Main.maxProjectiles) //inform them which side attack began on
                        {
                            Main.projectile[p].localAI[1] = NPC.ai[3]; //this is ok, they sync this
                            Main.projectile[p].netUpdate = true;
                        }
                    }
                    SoundEngine.PlaySound(SoundID.Item92, NPC.Center);
                    for (int i = 0; i < 30; i++)
                    {
                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.IceTorch, 0f, 0f, 0, default, 3f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 12f;
                    }
                }
            }
        }
        void PrepareSpearDashDirectP1()
        {
            if (Phase2Check())
                return;
            if (NPC.ai[3] == 0)
            {
                if (!AliveCheck(player))
                    return;
                NPC.ai[3] = 1;
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearSpin>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 240); // 250);
                    TelegraphSound = SoundEngine.PlaySound(YharimEXSoundRegistry.YharimEXUnpredictive with { Volume = 2f }, NPC.Center);
                }


                EdgyBossText(GFBQuote(4));
            }

            if (++NPC.ai[1] > 240)
            {
                if (!AliveCheck(player))
                    return;
                AttackChoice++;
                NPC.ai[3] = 0;
                NPC.netUpdate = true;
            }

            Vector2 targetPos = player.Center;
            if (NPC.Top.Y < player.Bottom.Y)
                targetPos.X += 600f * Math.Sign(NPC.Center.X - player.Center.X);
            targetPos.Y += 400f;
            Movement(targetPos, 0.7f, false);
        }
        void SpearDashDirectP1()
        {
            if (Phase2Check())
                return;
            NPC.velocity *= 0.9f;

            if (NPC.ai[3] == 0)
                NPC.ai[3] = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? Main.rand.Next(3, 15) : 10;

            if (++NPC.ai[1] > NPC.ai[3])
            {
                NPC.netUpdate = true;
                AttackChoice++;
                NPC.ai[1] = 0;
                if (++NPC.ai[2] > 5)
                {
                    P1NextAttackOrMasoOptions(4); //go to next attack after dashes
                }
                else
                {
                    float speed = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 45f : 30f;
                    NPC.velocity = speed * NPC.SafeDirectionTo(player.Center + player.velocity);
                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearDash>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);

                        if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<YharimEXDeathray2>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                        }
                    }

                    EdgyBossText(GFBQuote(5));
                }
            }
        }
        void WhileDashingP1()
        {
            NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);
            if (++NPC.ai[1] > 30)
            {
                if (!AliveCheck(player))
                    return;
                NPC.netUpdate = true;
                AttackChoice--;
                NPC.ai[1] = 0;
            }
        }
        void ApproachForNextAttackP1()
        {
            if (!AliveCheck(player))
                return;
            if (Phase2Check())
                return;
            Vector2 targetPos = player.Center + player.SafeDirectionTo(NPC.Center) * 250;
            if (NPC.Distance(targetPos) > 50 && ++NPC.ai[2] < 180)
            {
                Movement(targetPos, 0.5f);
            }
            else
            {
                NPC.netUpdate = true;
                AttackChoice++;
                NPC.ai[1] = 0;
                NPC.ai[2] = player.SafeDirectionTo(NPC.Center).ToRotation();
                NPC.ai[3] = (float)Math.PI / 10f;
                if (player.Center.X < NPC.Center.X)
                    NPC.ai[3] *= -1;
            }
        }
        void VoidRaysP1()
        {
            if (Phase2Check())
                return;
            NPC.velocity = Vector2.Zero;
            if (--NPC.ai[1] < 0)
            {
                if (YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(2, 0).RotatedBy(NPC.ai[2]), ModContent.ProjectileType<YharimEXMark1>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                NPC.ai[1] = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 5; //delay between projs
                NPC.ai[2] += NPC.ai[3];
                if (NPC.localAI[0]++ == 20 || NPC.localAI[0] == 40)
                {
                    NPC.netUpdate = true;
                    NPC.ai[2] -= NPC.ai[3] / ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 2);

                    EdgyBossText(GFBQuote(6));
                }
                else if (NPC.localAI[0] >= ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 60 : 40))
                {
                    P1NextAttackOrMasoOptions(7);
                }
            }
        }
        const int MUTANT_SWORD_SPACING = 80;
        const int MUTANT_SWORD_MAX = 12;
        void BoundaryBulletHellAndSwordP1()
        {
            switch ((int)NPC.localAI[2])
            {
                case 0: //boundary lite
                    if (NPC.ai[3] == 0)
                    {
                        if (AliveCheck(player))
                        {
                            NPC.ai[3] = 1;
                            NPC.localAI[0] = Math.Sign(NPC.Center.X - player.Center.X);
                        }
                        else
                        {
                            break;
                        }

                        EdgyBossText(GFBQuote(7));
                    }

                    if (Phase2Check())
                        return;

                    NPC.velocity = Vector2.Zero;

                    if (++NPC.ai[1] > 2) //boundary
                    {
                        SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
                        NPC.ai[1] = 0;
                        //ai3 - 300 so that when attack ends, the projs will behave like at start of attack normally (straight streams)
                        NPC.ai[2] += (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) //maso uses true boundary
                                ? (float)Math.PI / 8 / 480 * (NPC.ai[3] - 300) * NPC.localAI[0]
                                : MathHelper.Pi / 77f;

                        if (YharimEXGlobalUtilities.HostCheck)
                        {
                            int max = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 5 : 4;
                            for (int i = 0; i < max; i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0f, -7f).RotatedBy(NPC.ai[2] + MathHelper.TwoPi / max * i),
                                    ModContent.ProjectileType<YharimEXEye>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                            }
                        }

                    }

                    if (++NPC.ai[3] > ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 360 : 240))
                    {
                        P1NextAttackOrMasoOptions(AttackChoice);
                    }
                    break;

                case 1:
                    PrepareMutantSword();
                    break;

                case 2:
                    MutantSword();
                    break;

                default:
                    break;
            }
        }
        void PrepareMutantSword()
        {
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded && AttackChoice == 9 && Main.LocalPlayer.active && NPC.Distance(Main.LocalPlayer.Center) < 3000f && Main.expertMode)
                Main.LocalPlayer.AddBuff(FargosSouls.Find<ModBuff>("PurgedBuff").Type, 2);

            //can alternate directions
            int sign = AttackChoice != 9 && NPC.localAI[2] % 2 == 1 ? -1 : 1;

            if (NPC.ai[2] == 0) //move onscreen so player can see
            {
                if (!AliveCheck(player))
                    return;

                Vector2 targetPos = player.Center;
                targetPos.X += 420 * Math.Sign(NPC.Center.X - player.Center.X);
                targetPos.Y -= 210 * sign;
                Movement(targetPos, 1.2f);

                if (++NPC.localAI[0] > 30 || (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.Distance(targetPos) < 64)
                {
                    NPC.velocity = Vector2.Zero;
                    NPC.netUpdate = true;

                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                    NPC.localAI[1] = Math.Sign(player.Center.X - NPC.Center.X);
                    float startAngle = MathHelper.PiOver4 * -NPC.localAI[1];
                    NPC.ai[2] = startAngle * -4f / 20 * sign; //travel the full arc over number of ticks
                    if (sign < 0)
                        startAngle += MathHelper.PiOver2 * -NPC.localAI[1];

                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        Vector2 offset = Vector2.UnitY.RotatedBy(startAngle) * -MUTANT_SWORD_SPACING;

                        void MakeSword(Vector2 pos, float spacing, float rotation = 0)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + pos, Vector2.Zero, ModContent.ProjectileType<YharimEXSword>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0f, Main.myPlayer, NPC.whoAmI, spacing);
                        }

                        for (int i = 0; i < MUTANT_SWORD_MAX; i++)
                        {
                            MakeSword(offset * i, MUTANT_SWORD_SPACING * i);
                        }

                        for (int i = -1; i <= 1; i += 2)
                        {
                            MakeSword(offset.RotatedBy(MathHelper.ToRadians(26.5f * i)), 60 * 3);
                            MakeSword(offset.RotatedBy(MathHelper.ToRadians(40 * i)), 60 * 4f);
                        }
                    }

                    EdgyBossText(GFBQuote(8));
                }
            }
            else
            {
                NPC.velocity = Vector2.Zero;

                int endtime = 90;

                FancyFireballs((int)(NPC.ai[1] / endtime * 60f));

                if (++NPC.ai[1] > endtime)
                {
                    if (AttackChoice != 9)
                        AttackChoice++;

                    NPC.localAI[2]++; //progresses state in p1, counts swings in p2

                    Vector2 targetPos = player.Center;
                    targetPos.X -= 300 * NPC.ai[2];
                    NPC.velocity = (targetPos - NPC.Center) / 20;
                    NPC.ai[1] = 0;
                    NPC.netUpdate = true;
                }

                NPC.direction = NPC.spriteDirection = Math.Sign(NPC.localAI[1]);
            }
        }
        void MutantSword()
        {
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded && AttackChoice == 9 && Main.LocalPlayer.active && NPC.Distance(Main.LocalPlayer.Center) < 3000f && Main.expertMode)
                Main.LocalPlayer.AddBuff(FargosSouls.Find<ModBuff>("PurgedBuff").Type, 2);

            NPC.ai[3] += NPC.ai[2];
            NPC.direction = NPC.spriteDirection = Math.Sign(NPC.localAI[1]);

            if (NPC.ai[1] == 20)
            {
                if (!Main.dedServ && Main.LocalPlayer.active)
                    ScreenShakeSystem.StartShake(10, shakeStrengthDissipationIncrement: 10f / 30);

                //moon chain explosions
                int explosions = 0;
                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) && AttackChoice != 9 || YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                    explosions = 8;
                else if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
                    explosions = 5;
                if (explosions > 0)
                {
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(SoundID.Thunder with { Pitch = -0.5f }, NPC.Center);

                    float lookSign = Math.Sign(NPC.localAI[1]);
                    float arcSign = Math.Sign(NPC.ai[2]);
                    Vector2 offset = lookSign * Vector2.UnitX.RotatedBy(MathHelper.PiOver4 * arcSign);

                    const float length = MUTANT_SWORD_SPACING * MUTANT_SWORD_MAX / 2f;
                    Vector2 spawnPos = NPC.Center + length * offset;
                    Vector2 baseDirection = player.DirectionFrom(spawnPos);

                    int max = explosions; //spread
                    for (int i = 0; i < max; i++)
                    {
                        Vector2 angle = baseDirection.RotatedBy(MathHelper.TwoPi / max * i);
                        float ai1 = i <= 2 || i == max - 2 ? 48 : 24;
                        if (YharimEXGlobalUtilities.HostCheck)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos + Main.rand.NextVector2Circular(NPC.width / 2, NPC.height / 2), Vector2.Zero, ModContent.ProjectileType<YharimEXSunBlast>(),
                                YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0f, Main.myPlayer, MathHelper.WrapAngle(angle.ToRotation()), ai1);
                        }
                    }
                }
            }
            if (++NPC.ai[1] > 25)
            {
                if (AttackChoice == 9)
                {
                    P1NextAttackOrMasoOptions(AttackChoice);
                }
                else if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.localAI[2] < 3 * (endTimeVariance + 0.5))
                {
                    AttackChoice--;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                    NPC.localAI[1] = 0;
                    NPC.netUpdate = true;
                }
                else
                {
                    ChooseNextAttack(13, 21, 24, 29, 31, 33, 37, 41, 42, 44);
                }
            }
        }
    }
}