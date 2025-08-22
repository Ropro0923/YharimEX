// YHARIMEX
using YharimEX.Assets.Sounds.Attacks;
using YharimEX.Content.Items;
using YharimEX.Core.Globals;
using YharimEX.Content.Projectiles;
using YharimEX.Assets.ExtraTextures;
using YharimEX.Content.BossBars;
using YharimEX.Content.NPCs.Town;
using YharimEX.Core.Systems;
using YharimEX.Content.Buffs;

// IMPORTANT
using CalamityMod;
using Luminance.Core.Graphics;
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
using YharimEX.Content.Projectiles.MutantAttack;    

namespace YharimEX.Content.NPCs.Bosses
{
    public class YharimEXBoss : ModNPC
    {
        public override string Texture => "YharimEX/Assets/NPCs/YharimEXBoss";
        public override string BossHeadTexture => "YharimEX/Assets/NPCs/YharimEXBoss_Head";
        public SlotId? TelegraphSound = null;
        Player player => Main.player[NPC.target];
        public bool playerInvulTriggered;
        public int ritualProj, spriteProj, ringProj;
        private bool droppedSummon = false;
        public Queue<float> attackHistory = new();
        public int attackCount;
        public int hyper;
        public float endTimeVariance;
        public bool ShouldDrawAura;
        public float AuraScale = 1f;
        public ref float AttackChoice => ref NPC.ai[0];
        public Vector2 AuraCenter = Vector2.Zero;
        string TownNPCName;
        public const int HyperMax = 5;

        public Mod FargoSouls()
        {
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                return YharimEXCrossmodSystem.FargowiltasSouls.Mod;
            else return null;
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
            NPCID.Sets.NoMultiplayerSmoothingByType[NPC.type] = true;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(NPC.type);
            NPCID.Sets.MustAlwaysDraw[Type] = true;

            NPC.AddDebuffImmunities(
            [
                BuffID.Confused,
                BuffID.Chilled,
                BuffID.OnFire,
                BuffID.Suffocation,
            ]);

            if (YharimEXCrossmodSystem.Fargowiltas.Loaded)
            {
                NPC.AddDebuffImmunities(
                [
                    FargoSouls().Find<ModBuff>("LethargicBuff").Type,
                    FargoSouls().Find<ModBuff>("ClippedWingsBuff").Type,
                    FargoSouls().Find<ModBuff>("MutantNibbleBuff").Type,
                    FargoSouls().Find<ModBuff>("OceanicMaulBuff").Type,
                    FargoSouls().Find<ModBuff>("LightningRodBuff").Type,
                    FargoSouls().Find<ModBuff>("SadismBuff").Type,
                    FargoSouls().Find<ModBuff>("GodEaterBuff").Type,
                    FargoSouls().Find<ModBuff>("TimeFrozenBuff").Type,
                    FargoSouls().Find<ModBuff>("LeadPoisonBuff").Type,
                ]);
            }
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
                new FlavorTextBestiaryInfoElement("Mods.YharimEX.Bestiary.YharimEXBoss")
            ]);
        }


        public override void SetDefaults()
        {
            NPC.width = 120;//34;
            NPC.height = 120;//50;
            if (Main.getGoodWorld)
            {
                NPC.width = Player.defaultWidth;
                NPC.height = Player.defaultHeight;
            }
            NPC.damage = 444 + 44;
            NPC.defense = 255;
            NPC.value = Item.buyPrice(15);
            NPC.lifeMax = Main.expertMode ? 9700000 : 5100000;
            NPC.HitSound = SoundID.NPCHit57;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.npcSlots = 50f;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.aiStyle = -1;
            NPC.netAlways = true;
            NPC.timeLeft = NPC.activeTime * 30;
            NPC.BossBar = ModContent.GetInstance<YharimEXBossBar>();
            if (YharimEXWorldFlags.AngryYharimEX)
            {
                NPC.damage *= 17;
                NPC.defense *= 10;
                Main.NewText("RAH");
            }
            Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/TheRealityoftheProphecy");
            SceneEffectPriority = SceneEffectPriority.BossHigh + 1;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.damage = (int)Math.Round(NPC.damage * 0.5);
            NPC.lifeMax = (int)Math.Round(NPC.lifeMax * 0.5 * balance);
        }
        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            //modifiers.FinalDamage *= 0.65f;
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            //modifiers.FinalDamage *= 0.65f;
        }
        public override void UpdateLifeRegen(ref int damage)
        {
            //damage /= 3;
            base.UpdateLifeRegen(ref damage);
        }
        public override bool CanHitPlayer(Player target, ref int CooldownSlot)
        {
            CooldownSlot = 1;
            if (!(YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                return false;
            return NPC.Distance(YharimEXGlobalUtilities.ClosestPointInHitbox(target, NPC.Center)) < Player.defaultHeight && AttackChoice > -1;
        }

        public override bool CanHitNPC(NPC target)
        {
            if (target.boss)
                return false;
            return base.CanHitNPC(target);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.localAI[0]);
            writer.Write(NPC.localAI[1]);
            writer.Write(NPC.localAI[2]);
            writer.Write(endTimeVariance);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.localAI[0] = reader.ReadSingle();
            NPC.localAI[1] = reader.ReadSingle();
            NPC.localAI[2] = reader.ReadSingle();
            endTimeVariance = reader.ReadSingle();
        }

        public override void OnSpawn(IEntitySource source)
        {
            int n = NPC.FindFirstNPC(ModContent.NPCType<TheGodseeker>());
            if (n != -1 && n != Main.maxNPCs)
            {
                NPC.Bottom = Main.npc[n].Bottom;
                TownNPCName = Main.npc[n].GivenName;

                Main.npc[n].life = 0;
                Main.npc[n].active = false;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
            }
            AuraCenter = NPC.Center;
        }

        public override bool PreAI()
        {
            
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && !Main.dedServ)
                {
                    if (!Main.LocalPlayer.ItemTimeIsZero && (Main.LocalPlayer.HeldItem.type == ItemID.RodofDiscord || Main.LocalPlayer.HeldItem.type == ItemID.RodOfHarmony))
                        Main.LocalPlayer.AddBuff(FargoSouls().Find<ModBuff>("TimeFrozenBuff").Type, 600);
                }
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.dead || !player.active || !NPC.WithinRange(player.Center, 10000f))
                    continue;

                player.wingTime = player.wingTimeMax;
                player.Calamity().infiniteFlight = true;
            }

            return base.PreAI();
        }

        public override void AI()
        {
            YharimEXGlobalNPC.yharimEXBoss = NPC.whoAmI;
            
            NPC.dontTakeDamage = AttackChoice < 0; //invul in p3

            // Set this to false by default.
            ShouldDrawAura = false;

            ManageAurasAndPreSpawn();
            ManageNeededProjectiles();

            NPC.direction = NPC.spriteDirection = NPC.Center.X < player.Center.X ? 1 : -1;

            bool drainLifeInP3 = true;

            if (TelegraphSound != null)
            {
                if (SoundEngine.TryGetActiveSound(TelegraphSound.Value, out ActiveSound s))
                {
                    s.Position = NPC.Center;
                }
            }

            switch ((int)AttackChoice)
            {
                #region phase 1

                case 0: SpearTossDirectP1AndChecks(); break;

                case 1: OkuuSpheresP1(); break;

                case 2: PrepareTrueEyeDiveP1(); break;
                case 3: TrueEyeDive(); break;

                case 4: PrepareSpearDashDirectP1(); break;
                case 5: SpearDashDirectP1(); break;
                case 6: WhileDashingP1(); break;

                case 7: ApproachForNextAttackP1(); break;
                case 8: VoidRaysP1(); break;

                case 9: BoundaryBulletHellAndSwordP1(); break;

                #endregion

                #region phase 2

                case 10: Phase2Transition(); break;

                case 11: ApproachForNextAttackP2(); break;
                case 12: VoidRaysP2(); break;

                case 13: PrepareSpearDashPredictiveP2(); break;
                case 14: SpearDashPredictiveP2(); break;
                case 15: WhileDashingP2(); break;

                case 16: goto case 11; //approach for bullet hell
                case 17: BoundaryBulletHellP2(); break;

                case 18: AttackChoice++; break; //new attack can be put here

                case 19: PillarDunk(); break;

                case 20: EOCStarSickles(); break;

                case 21: PrepareSpearDashDirectP2(); break;
                case 22: SpearDashDirectP2(); break;
                case 23: //while dashing
                    if (NPC.ai[1] % 3 == 0)
                        NPC.ai[1]++;
                    goto case 15;

                case 24: SpawnDestroyersForPredictiveThrow(); break;
                case 25: SpearTossPredictiveP2(); break;

                case 26: PrepareMechRayFan(); break;
                case 27: MechRayFan(); break;

                case 28: AttackChoice++; break; //free slot for new attack

                case 29: PrepareFishron1(); break;
                case 30: SpawnFishrons(); break;

                case 31: PrepareTrueEyeDiveP2(); break;
                case 32: goto case 3; //spawn eyes

                case 33: PrepareNuke(); break;
                case 34: Nuke(); break;

                case 35: PrepareSlimeRain(); break;
                case 36: SlimeRain(); break;

                case 37: PrepareFishron2(); break;
                case 38: goto case 30; //spawn fishrons

                case 39: PrepareOkuuSpheresP2(); break;
                case 40: OkuuSpheresP2(); break;

                case 41: SpearTossDirectP2(); break;

                case 42: PrepareTwinRangsAndCrystals(); break;
                case 43: TwinRangsAndCrystals(); break;

                case 44: EmpressSwordWave(); break;

                case 45: PrepareYharimEXSword(); break;
                case 46: YharimEXSword(); break;

                //case 47: goto case 35;
                //case 48: QueenSlimeRain(); break;

                //case 49: SANSGOLEM(); break;

                //case 50: //wof

                //gap in the numbers here so the ai loops right
                //when adding a new attack, remember to make ChooseNextAttack() point to the right case!

                case 52: P2NextAttackPause(); break;

                #endregion

                #region phase 3

                case -1: drainLifeInP3 = Phase3Transition(); break;

                case -2: VoidRaysP3(); break;

                case -3: OkuuSpheresP3(); break;

                case -4: BoundaryBulletHellP3(); break;

                case -5: FinalSpark(); break;

                case -6: DyingDramaticPause(); break;
                case -7: DyingAnimationAndHandling(); break;

                #endregion

                default: AttackChoice = 11; goto case 11; //return to first phase 2 attack
            }
            //manage aura scale
            if (AttackChoice == 1) //ooku spheres p1
            {
                AuraScale = MathHelper.Lerp(AuraScale, 0.7f, 0.02f);
            }
            else if (AttackChoice == 5 || AttackChoice == 6)
            {
                AuraScale = MathHelper.Lerp(AuraScale, 1.25f, 0.1f);
            }
            else
            {
                AuraScale = MathHelper.Lerp(AuraScale, 1f, 0.1f);
            }
            if (!(YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) || (AttackChoice != 5 && AttackChoice != 6)) //spear dash direct p1
            {
                AuraCenter = Vector2.Lerp(AuraCenter, NPC.Center, 0.3f);
            }
            //in emode p2
            if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) && (AttackChoice < 0 || AttackChoice > 10 || AttackChoice == 10 && NPC.ai[1] > 150))
            {
                Main.dayTime = false;
                Main.time = 16200; //midnight, for empress visuals

                Main.raining = false; //disable rain
                Main.rainTime = 0;
                Main.maxRaining = 0;

                Main.bloodMoon = false; //disable blood moon
            }

            if (AttackChoice < 0 && NPC.life > 1 && drainLifeInP3) //in desperation
            {
                int time = 480 + 240 + 420 + 480 + 1020 - 60;
                if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                    time = Main.getGoodWorld ? 5000 : 4350;
                int drain = NPC.lifeMax / time;
                NPC.life -= drain;
                if (NPC.life < 1)
                    NPC.life = 1;
            }

            if (player.immune || player.hurtCooldowns[0] != 0 || player.hurtCooldowns[1] != 0)
                playerInvulTriggered = true;

            //drop summon
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) && (DownedBossSystem.downedCalamitas || DownedBossSystem.downedExoMechs) && !YharimEXWorldFlags.DownedYharimEX && YharimEXGlobalUtilities.HostCheck && NPC.HasPlayerTarget && !droppedSummon)
                {
                    Item.NewItem(NPC.GetSource_Loot(), player.Hitbox, ModContent.ItemType<YharimsRage>());
                    droppedSummon = true;
                }

                if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && Main.getGoodWorld && ++hyper > HyperMax + 1)
                {
                    hyper = 0;
                    NPC.AI();
                }
            }
        }

        #region helper functions

        bool spawned;
        void ManageAurasAndPreSpawn()
        {
            
            if (!spawned)
            {
                spawned = true;

                int prevLifeMax = NPC.lifeMax;
                if (YharimEXWorldFlags.AngryYharimEX)
                {
                    NPC.lifeMax *= 100;
                    if (NPC.lifeMax < prevLifeMax)
                        NPC.lifeMax = int.MaxValue;
                }
                NPC.life = NPC.lifeMax;
            }

            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && Main.LocalPlayer.active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost)
                Main.LocalPlayer.AddBuff(ModContent.BuffType<TyrantPresenceBuff>(), 2);

            if (NPC.localAI[3] == 0)
            {
                NPC.TargetClosest();
                if (NPC.timeLeft < 30)
                    NPC.timeLeft = 30;
                if (NPC.Distance(Main.player[NPC.target].Center) < 1500)
                {
                    NPC.localAI[3] = 1;
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    EdgyBossText(GFBQuote(2));

                    if (YharimEXWorldFlags.AngryYharimEX && (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXBossRush>(), 0, 0f, Main.myPlayer, NPC.whoAmI);
                }
            }
            else if (NPC.localAI[3] == 1)
            {
                ShouldDrawAura = true;
                // -1 means no dust is drawn, as it looks ugly.
                if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                {
                    ArenaAura(AuraCenter, 2000f * AuraScale, true, -1, default, YharimEXCrossmodSystem.Fargowiltas.Mod.Find<ModBuff>("GodEaterBuff").Type, ModContent.BuffType<TyrantPresenceBuff>());
                }
                else
                {
                    ArenaAura(AuraCenter, 2000f * AuraScale, true, -1, default, ModContent.BuffType<TyrantPresenceBuff>());
                }
            }
            else
            {
                if (Main.LocalPlayer.active && NPC.Distance(Main.LocalPlayer.Center) < 3000f)
                {
                    if (Main.expertMode)
                    {
                        Main.LocalPlayer.AddBuff(ModContent.BuffType<TyrantPresenceBuff>(), 2);
                        if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                        {
                            if (Main.getGoodWorld)
                                Main.LocalPlayer.AddBuff(YharimEXCrossmodSystem.Fargowiltas.Mod.Find<ModBuff>("GoldenStasisCDBuff").Type, 2);
                        }
                    }

                    if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) && AttackChoice < 0 && AttackChoice > -6)
                    {
                        if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                        {
                            Main.LocalPlayer.AddBuff(YharimEXCrossmodSystem.Fargowiltas.Mod.Find<ModBuff>("GoldenStasisCDBuff").Type, 2);
                        }
                        if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                        {
                            Main.LocalPlayer.AddBuff(ModContent.BuffType<TyrantDesperationBuff>(), 2);
                            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                            {
                                Main.LocalPlayer.AddBuff(YharimEXCrossmodSystem.Fargowiltas.Mod.Find<ModBuff>("TimeStopCDBuff").Type, 2);
                            }
                        }
                    }

                }
            }
        }

        void ManageNeededProjectiles()
        {
            if (YharimEXGlobalUtilities.HostCheck) //checks for needed projs
            {
                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) && AttackChoice != -7 && (AttackChoice < 0 || AttackChoice > 10) && YharimEXGlobalUtilities.ProjectileExists(ritualProj, ModContent.ProjectileType<YharimEXRitual>()) == null)
                    ritualProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXRitual>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0f, NPC.whoAmI);

                if (YharimEXGlobalUtilities.ProjectileExists(ringProj, ModContent.ProjectileType<YharimEXRitual5>()) == null)
                    ringProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXRitual5>(), 0, 0f, Main.myPlayer, 0f, NPC.whoAmI);

                if (YharimEXGlobalUtilities.ProjectileExists(spriteProj, ModContent.ProjectileType<YharimEXBossProjectile>()) == null)
                {
                    /*if (Main.netMode == NetmodeID.Server)
                        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("wheres my sprite"), Color.LimeGreen);
                    else
                        Main.NewText("wheres my sprite");*/
                    if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        int number = 0;
                        for (int index = 999; index >= 0; --index)
                        {
                            if (!Main.projectile[index].active)
                            {
                                number = index;
                                break;
                            }
                        }
                        if (number >= 0)
                        {
                            Projectile projectile = Main.projectile[number];
                            projectile.SetDefaults(ModContent.ProjectileType<YharimEXBossProjectile>());
                            projectile.Center = NPC.Center;
                            projectile.owner = Main.myPlayer;
                            projectile.velocity.X = 0;
                            projectile.velocity.Y = 0;
                            projectile.damage = 0;
                            projectile.knockBack = 0f;
                            projectile.identity = number;
                            projectile.gfxOffY = 0f;
                            projectile.stepSpeed = 1f;
                            projectile.ai[1] = NPC.whoAmI;

                            spriteProj = number;
                        }
                    }
                    else //server
                    {
                        spriteProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXBossProjectile>(), 0, 0f, Main.myPlayer, 0f, NPC.whoAmI);
                        /*if (Main.netMode == NetmodeID.Server)
                            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"got sprite {spriteProj}"), Color.LimeGreen);
                        else
                            Main.NewText($"got sprite {spriteProj}");*/
                    }
                }
            }
        }

        void ChooseNextAttack(params int[] args)
        {
            float buffer = AttackChoice + 1;
            AttackChoice = 52;
            NPC.ai[1] = 0;
            NPC.ai[2] = buffer;
            NPC.ai[3] = 0;
            NPC.localAI[0] = 0;
            NPC.localAI[1] = 0;
            NPC.localAI[2] = 0;
            //NPC.TargetClosest();
            NPC.netUpdate = true;

            EdgyBossText(RandomObnoxiousQuote());

            /*string text = "-------------------------------------------------";
            Main.NewText(text);

            text = "";
            foreach (float f in attackHistory)
                text += f.ToString() + " ";
            Main.NewText($"history: {text}");*/

            if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
            {
                //become more likely to use randoms as life decreases
                bool useRandomizer = NPC.localAI[3] >= 3 && ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) || Main.rand.NextFloat(0.8f) + 0.2f > (float)Math.Pow((float)NPC.life / NPC.lifeMax, 2));

                if (YharimEXGlobalUtilities.HostCheck)
                {
                    Queue<float> recentAttacks = new(attackHistory); //copy of attack history that i can remove elements from freely

                    //if randomizer, start with a random attack, else use the previous state + 1 as starting attempt BUT DO SOMETHING ELSE IF IT'S ALREADY USED
                    if (useRandomizer)
                        NPC.ai[2] = Main.rand.Next(args);

                    //Main.NewText(useRandomizer ? "(Starting with random)" : "(Starting with regular next attack)");

                    while (recentAttacks.Count > 0)
                    {
                        bool foundAttackToUse = false;

                        for (int i = 0; i < 5; i++) //try to get next attack that isnt in this queue
                        {
                            if (!recentAttacks.Contains(NPC.ai[2]))
                            {
                                foundAttackToUse = true;
                                break;
                            }
                            NPC.ai[2] = Main.rand.Next(args);
                        }

                        if (foundAttackToUse)
                            break;

                        //couldn't find an attack to use after those attempts, forget 1 attack and repeat
                        recentAttacks.Dequeue();

                        //Main.NewText("REDUCE");
                    }

                    /*text = "";
                    foreach (float f in recentAttacks)
                        text += f.ToString() + " ";
                    Main.NewText($"recent: {text}");*/
                }
            }

            if (YharimEXGlobalUtilities.HostCheck)
            {
                int maxMemory = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 12 : 18;

                if (attackCount++ > maxMemory * 1.25) //after doing this many attacks, shorten queue so i can be more random again
                {
                    attackCount = 0;
                    maxMemory /= 4;
                }

                attackHistory.Enqueue(NPC.ai[2]);
                while (attackHistory.Count > maxMemory)
                    attackHistory.Dequeue();
            }

            endTimeVariance = (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? Main.rand.NextFloat(-0.5f, 1f) : 0;

            /*text = "";
            foreach (float f in attackHistory)
                text += f.ToString() + " ";
            Main.NewText($"after: {text}");*/
        }

        void P1NextAttackOrMasoOptions(float sourceAI)
        {
            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && Main.rand.NextBool(3))
            {
                int[] options = [0, 1, 2, 4, 7, 9, 9];
                AttackChoice = Main.rand.Next(options);
                if (AttackChoice == sourceAI) //dont repeat attacks consecutively
                    AttackChoice = sourceAI == 9 ? 0 : 9;

                bool badCombo = false;
                //dont go into boundary/sword from spheres, true eye dive, void rays
                if (AttackChoice == 9 && (sourceAI == 1 || sourceAI == 2 || sourceAI == 7))
                    badCombo = true;
                //dont go into destroyer-toss or void rays from true eye dive
                if ((AttackChoice == 0 || AttackChoice == 7) && sourceAI == 2)
                    badCombo = true;

                if (badCombo)
                    AttackChoice = 4; //default to dashes
                else if (AttackChoice == 9 && Main.rand.NextBool())
                    NPC.localAI[2] = 1f; //force sword attack instead of boundary
                else
                    NPC.localAI[2] = 0f;
            }
            else
            {
                if (AttackChoice == 9 && NPC.localAI[2] == 0)
                {
                    NPC.localAI[2] = 1;
                }
                else
                {
                    AttackChoice++;
                    NPC.localAI[2] = 0f;
                }
            }

            if (AttackChoice >= 10) //dont accidentally go into p2
                AttackChoice = 0;

            EdgyBossText(RandomObnoxiousQuote());

            NPC.ai[1] = 0;
            NPC.ai[2] = 0;
            NPC.ai[3] = 0;
            NPC.localAI[0] = 0;
            NPC.localAI[1] = 0;
            //NPC.localAI[2] = 0; //excluded because boundary-sword logic
            NPC.netUpdate = true;
        }

        void SpawnSphereRing(int max, float speed, int damage, float rotationModifier, float offset = 0)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float rotation = 2f * (float)Math.PI / max;
            int type = ModContent.ProjectileType<YharimEXSphereRing>();
            for (int i = 0; i < max; i++)
            {
                Vector2 vel = speed * Vector2.UnitY.RotatedBy(rotation * i + offset);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, type, damage, 0f, Main.myPlayer, rotationModifier * NPC.spriteDirection, speed);
            }
            SoundEngine.PlaySound(SoundID.Item84, NPC.Center);
        }

        bool AliveCheck(Player p, bool forceDespawn = false)
        {
            if (forceDespawn || (!p.active || p.dead || Vector2.Distance(NPC.Center, p.Center) > 3000f) && NPC.localAI[3] > 0)
            {
                NPC.TargetClosest();
                p = Main.player[NPC.target];
                if (forceDespawn || !p.active || p.dead || Vector2.Distance(NPC.Center, p.Center) > 3000f)
                {
                    if (NPC.timeLeft > 30)
                        NPC.timeLeft = 30;
                    NPC.velocity.Y -= 1f;
                    if (NPC.timeLeft == 1)
                    {
                        EdgyBossText(GFBQuote(36));
                        if (NPC.position.Y < 0)
                            NPC.position.Y = 0;
                        if (YharimEXGlobalUtilities.HostCheck && !NPC.AnyNPCs(ModContent.NPCType<TheGodseeker>()) && YharimEXWorldFlags.DownedYharimEX)
                        {
                            YharimEXGlobalUtilities.ClearHostileProjectiles(2, NPC.whoAmI);
                            int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<TheGodseeker>());
                            if (n != Main.maxNPCs)
                            {
                                Main.npc[n].homeless = true;
                                if (TownNPCName != default)
                                    Main.npc[n].GivenName = TownNPCName;
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                            }
                        }
                    }
                    return false;
                }
            }

            if (NPC.timeLeft < 3600)
                NPC.timeLeft = 3600;

            if (player.Center.Y / 16f > Main.worldSurface)
            {
                NPC.velocity.X *= 0.95f;
                NPC.velocity.Y -= 1f;
                if (NPC.velocity.Y < -32f)
                    NPC.velocity.Y = -32f;
                return false;
            }

            return true;
        }

        bool Phase2Check()
        {
            if (Main.expertMode && NPC.life < NPC.lifeMax * (2f / 3))
            {
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    AttackChoice = 10;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                    NPC.netUpdate = true;
                    // NOTE                YharimEXGlobalUtilities.ClearHostileProjectiles(1, NPC.whoAmI);
                    EdgyBossText(GFBQuote(3));
                }
                return true;
            }
            return false;
        }

        void Movement(Vector2 target, float speed, bool fastX = true, bool obeySpeedCap = true)
        {
            float turnaroundModifier = 1f;
            float maxSpeed = 24;

            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
            {
                speed *= 2;
                turnaroundModifier *= 2f;
                maxSpeed *= 1.5f;
            }

            if (Math.Abs(NPC.Center.X - target.X) > 10)
            {
                if (NPC.Center.X < target.X)
                {
                    NPC.velocity.X += speed;
                    if (NPC.velocity.X < 0)
                        NPC.velocity.X += speed * (fastX ? 2 : 1) * turnaroundModifier;
                }
                else
                {
                    NPC.velocity.X -= speed;
                    if (NPC.velocity.X > 0)
                        NPC.velocity.X -= speed * (fastX ? 2 : 1) * turnaroundModifier;
                }
            }
            if (NPC.Center.Y < target.Y)
            {
                NPC.velocity.Y += speed;
                if (NPC.velocity.Y < 0)
                    NPC.velocity.Y += speed * 2 * turnaroundModifier;
            }
            else
            {
                NPC.velocity.Y -= speed;
                if (NPC.velocity.Y > 0)
                    NPC.velocity.Y -= speed * 2 * turnaroundModifier;
            }

            if (obeySpeedCap)
            {
                if (Math.Abs(NPC.velocity.X) > maxSpeed)
                    NPC.velocity.X = maxSpeed * Math.Sign(NPC.velocity.X);
                if (Math.Abs(NPC.velocity.Y) > maxSpeed)
                    NPC.velocity.Y = maxSpeed * Math.Sign(NPC.velocity.Y);
            }
        }

        void DramaticTransition(bool fightIsOver, bool normalAnimation = true)
        {
            NPC.velocity = Vector2.Zero;

            if (fightIsOver)
            {
                if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                {
                    Main.player[NPC.target].ClearBuff(YharimEXCrossmodSystem.FargowiltasSouls.Mod.Find<ModBuff>("MutantFangBuff").Type);
                    Main.player[NPC.target].ClearBuff(YharimEXCrossmodSystem.FargowiltasSouls.Mod.Find<ModBuff>("AbomRebirthBuff").Type);
                }
            }
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 1.5f }, NPC.Center);

            if (normalAnimation)
            {
                if (YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXBomb>(), 0, 0f, Main.myPlayer);
            }

            const int max = 40;
            float totalAmountToHeal = fightIsOver
                ? Main.player[NPC.target].statLifeMax2 / 4f
                : NPC.lifeMax - NPC.life + NPC.lifeMax * 0.1f;
            for (int i = 0; i < max; i++)
            {
                int heal = (int)(Main.rand.NextFloat(0.9f, 1.1f) * totalAmountToHeal / max);
                Vector2 vel = normalAnimation
                    ? Main.rand.NextFloat(2f, 18f) * -Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) //looks messier normally
                    : 0.1f * -Vector2.UnitY.RotatedBy(MathHelper.TwoPi / max * i);
                float ai0 = fightIsOver ? -Main.player[NPC.target].whoAmI - 1 : NPC.whoAmI; //player -1 necessary for edge case of player 0
                float ai1 = vel.Length() / Main.rand.Next(fightIsOver ? 90 : 150, 180); //window in which they begin homing in
                if (YharimEXGlobalUtilities.HostCheck)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<YharimEXHeal>(), heal, 0f, Main.myPlayer, ai0, ai1);
            }
        }

        void EModeSpecialEffects()
        {
            if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
            {
                //because this breaks the background???
                if (Main.GameModeInfo.IsJourneyMode && CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().Enabled)
                    CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().SetPowerInfo(false);

                if (!SkyManager.Instance["YharimEX:YharimEXBoss"].IsActive())
                    SkyManager.Instance.Activate("YharimEX:YharimEXBoss");

                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/Storia");
            }
        }

        void TryMasoP3Theme()
        {
            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
            {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/StoriaShort");
            }
        }

        void FancyFireballs(int repeats)
        {
            float modifier = 0;
            for (int i = 0; i < repeats; i++)
                modifier = MathHelper.Lerp(modifier, 1f, 0.08f);

            float distance = 1600 * (1f - modifier);
            float rotation = MathHelper.TwoPi * modifier;
            const int max = 6;
            for (int i = 0; i < max; i++)
            {
                int d = Dust.NewDust(NPC.Center + distance * Vector2.UnitX.RotatedBy(rotation + MathHelper.TwoPi / max * i), 0, 0, DustID.SolarFlare, NPC.velocity.X * 0.3f, NPC.velocity.Y * 0.3f, newColor: Color.White);
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = 6f - 4f * modifier;
            }
        }

        private void EdgyBossText(string text)
        {
            if (Main.zenithWorld) //edgy boss text
            {
                Color color = Color.OrangeRed;
                YharimEXGlobalUtilities.PrintText(text, color);
                CombatText.NewText(NPC.Hitbox, color, text, true);
                /*
                if (Main.netMode == NetmodeID.SinglePlayer)
                    Main.NewText(text, Color.LimeGreen);
                else if (Main.netMode == NetmodeID.Server)
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), Color.LimeGreen);
                */
            }
        }
        const int ObnoxiousQuoteCount = 71;
        const string GFBLocPath = $"Mods.YharimEX.NPCs.GFBText.";
        private string RandomObnoxiousQuote() => Language.GetTextValue($"{GFBLocPath}Random{Main.rand.Next(ObnoxiousQuoteCount)}");
        private string GFBQuote(int num) => Language.GetTextValue($"{GFBLocPath}Quote{num}");

        #endregion

        #region p1

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
                    if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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
                        YharimEXGlobalUtilities.PrintLocalization($"Mods.{Mod.Name}.NPCs.SkipPhase1", Color.OrangeRed);

                    if (YharimEXWorldFlags.SkipYharimEXP1 >= 10)
                        NPC.ai[2] = 1; //flag for different p2 transition animation

                    return;
                }

                if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.ai[2] == 0) //first time only
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath13, NPC.Center);
                    if (YharimEXGlobalUtilities.HostCheck) //spawn worm
                    {
                        int appearance = Main.rand.Next(2);
                        // RETURN        if (YharimEXGlobalUtilities.AprilFools)
                        //           appearance = 0;
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

                //Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearAim>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);
            }
        }

        void OkuuSpheresP1()
        {
            if (Phase2Check())
                return;

            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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

                        if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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

        const int YHARIMEX_SWORD_SPACING = 80;
        const int YHARIMEX_SWORD_MAX = 12;

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

                    //if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) && NPC.ai[3] >= 300) //spear barrage
                    //{
                    //    if (NPC.ai[3] == 0)
                    //    {
                    //        NPC.ai[1] = 0;
                    //        NPC.ai[2] = Main.rand.NextFloat(MathHelper.TwoPi);
                    //        NPC.localAI[0] = player.Center.X;
                    //        NPC.localAI[1] = player.Center.Y;
                    //    }

                    //    const int spearsToMake = 18;
                    //    const int timeToFullSpears = 180;
                    //    const int timeGap = timeToFullSpears / spearsToMake;
                    //    if (NPC.ai[3] % timeGap == 0 && NPC.ai[3] < 300 + timeToFullSpears)
                    //    {
                    //        NPC.ai[2] += MathHelper.TwoPi / spearsToMake * ++NPC.ai[1];

                    //        Vector2 target = new Vector2(NPC.localAI[0], NPC.localAI[1]);
                    //        Vector2 spawnpos = target + 600 * NPC.ai[2].ToRotationVector2();

                    //        if (YharimEXGlobalUtilities.HostCheck)
                    //        {

                    //        }
                    //    }
                    //}
                    //else
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
                    PrepareYharimEXSword();
                    break;

                case 2:
                    YharimEXSword();
                    break;

                default:
                    break;
            }
        }

        void PrepareYharimEXSword()
        {
            
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded && AttackChoice == 9 && Main.LocalPlayer.active && NPC.Distance(Main.LocalPlayer.Center) < 3000f && Main.expertMode)
                Main.LocalPlayer.AddBuff(FargoSouls().Find<ModBuff>("PurgedBuff").Type, 2);

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

                if ((++NPC.localAI[0] > 30 || (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)) && NPC.Distance(targetPos) < 64)
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
                        Vector2 offset = Vector2.UnitY.RotatedBy(startAngle) * -YHARIMEX_SWORD_SPACING;

                        void MakeSword(Vector2 pos, float spacing, float rotation = 0)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + pos, Vector2.Zero, ModContent.ProjectileType<YharimEXSword>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage, 4f / 3f), 0f, Main.myPlayer, NPC.whoAmI, spacing);
                        }

                        for (int i = 0; i < YHARIMEX_SWORD_MAX; i++)
                        {
                            MakeSword(offset * i, YHARIMEX_SWORD_SPACING * i);
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

        void YharimEXSword()
        {
            
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded && AttackChoice == 9 && Main.LocalPlayer.active && NPC.Distance(Main.LocalPlayer.Center) < 3000f && Main.expertMode)
                Main.LocalPlayer.AddBuff(FargoSouls().Find<ModBuff>("PurgedBuff").Type, 2);

            NPC.ai[3] += NPC.ai[2];
            NPC.direction = NPC.spriteDirection = Math.Sign(NPC.localAI[1]);

            if (NPC.ai[1] == 20)
            {
                if (!Main.dedServ && Main.LocalPlayer.active)
                    ScreenShakeSystem.StartShake(10, shakeStrengthDissipationIncrement: 10f / 30);

                //moon chain explosions
                int explosions = 0;
                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) && AttackChoice != 9 || (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                    explosions = 8;
                else if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
                    explosions = 5;
                if (explosions > 0)
                {
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(SoundID.Thunder with { Pitch = -0.5f }, NPC.Center);

                    float lookSign = Math.Sign(NPC.localAI[1]);
                    float arcSign = Math.Sign(NPC.ai[2]);
                    Vector2 offset = lookSign * Vector2.UnitX.RotatedBy(MathHelper.PiOver4 * arcSign);

                    const float length = YHARIMEX_SWORD_SPACING * YHARIMEX_SWORD_MAX / 2f;
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
                    ChooseNextAttack(13, 21, 24, 29, 31, 33, 37, 41, 42, 44/*, 47*//*, 49*/);
                }
            }
        }

        #endregion

        #region p2

        void Phase2Transition()
        {
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
                if (Main.LocalPlayer.active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost && NPC.Distance(Main.LocalPlayer.Center) < 3000)
                {
                    Main.LocalPlayer.controlUseItem = false;
                    Main.LocalPlayer.controlUseTile = false;

                    // NOTE            Main.LocalPlayer.FargoSouls()().NoUsingItems = 2;

                }
            }

            if (NPC.ai[1] == 0)
            {
                // NOTE        YharimEXGlobalUtilities.ClearAllProjectiles(2, NPC.whoAmI);

                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
                {
                    DramaticTransition(false, NPC.ai[2] == 0);

                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        ritualProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXRitual>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0f, NPC.whoAmI);

                        if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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
                    //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.GlowRingHollow>(), 0, 0f, Main.myPlayer, 5);
                    //Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.GlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, -22);
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
            }
            else if (NPC.ai[1] > 150)
            {
                NPC.localAI[3] = 3;
            }

            if (++NPC.ai[1] > 270)
            {
                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
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
                    TelegraphSound = SoundEngine.PlaySound(YharimEXSoundRegistry.YharimEXUnpredictive with { Volume = 8f }, NPC.Center);
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
                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
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
                //if (WorldSavingSystem.MasochistMode) NPC.ai[2] = NPC.SafeDirectionTo(player.Center).ToRotation(); //starting rotation offset to avoid hitting at close range
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
                    if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
                        max += 1;
                    if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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
                if (YharimEXGlobalUtilities.HostCheck)
                {
                    void Clone(float ai1, float ai2, float ai3) => YharimEXGlobalUtilities.NewNPCEasy(NPC.GetSource_FromAI(), NPC.Center, ModContent.NPCType<YharimEXIllusion>(), NPC.whoAmI, NPC.whoAmI, ai1, ai2, ai3);
                    Clone(-1, 1, pillarAttackDelay * 4);
                    Clone(1, -1, pillarAttackDelay * 2);
                    Clone(1, 1, pillarAttackDelay * 3);
                    if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                    {
                        Clone(1, 1, pillarAttackDelay * 6);
                        if (Main.getGoodWorld)
                        {
                            Clone(-1, 1, pillarAttackDelay * 7);
                            Clone(1, -1, pillarAttackDelay * 8);
                        }
                    }

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Center, new Vector2(0, -4), ModContent.ProjectileType<YharimEXBrain>(), 0, 0, Main.myPlayer);
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
            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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

                if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)) //begin attack much faster
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
                if (++NPC.ai[2] > (WorldSavingSystem.MasochistMode ? 3 : 1))
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
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXSpearSpin>(), YharimEXGlobalUtilities.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 180);// + (WorldSavingSystem.MasochistMode ? 10 : 20));
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
                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
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
                    if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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

            if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
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
                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
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
                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
                {
                    cap += 2;
                }
                if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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
                        if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                            ai1 += 0.4f;
                        float appearance = NPC.localAI[2];
                        //        if (YharimEXGlobalUtilities.AprilFools)
                        //            appearance = 0;
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
                    if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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

                if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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

                    if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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
                //if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)) //maso prime jumpscare after rays
                //{
                //    for (int i = 0; i < 60; i++)
                //        SpawnPrime(45, 90);
                //}

                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)) //use full moveset
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

                if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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
            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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
            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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
                if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode))
                    NPC.localAI[1] = Main.rand.Next((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) ? 3 : 5, 9);
                else
                    NPC.localAI[1] = 5;

                if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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
                    if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                    {
                        if (randomOffset.Length() < 0.5f)
                            randomOffset = 0.5f * randomOffset.SafeNormalize(Vector2.UnitX);
                        randomOffset *= 2f;
                    }

                    Sword(spawnPos, attackAngle + MathHelper.PiOver4, Ai1, randomOffset);
                    Sword(spawnPos, attackAngle - MathHelper.PiOver4, Ai1, randomOffset);

                    if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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

                    //if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)) //block one side so only one real exit exists
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
                                ModContent.ProjectileType<YharimEXSansHead>(),
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

        #endregion

        #region p3

        bool Phase3Transition()
        {
            bool retval = true;

            NPC.localAI[3] = 3;

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
                    // NOTE    Main.LocalPlayer.FargoSouls()().NoUsingItems = 2;
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
            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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
            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
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

                    //    if (SoulConfig.Instance.ForcedFilters && Main.WaveQuality == 0)
                    //        Main.WaveQuality = 1;
                }

                if (NPC.ai[1] % 3 == 0 && YharimEXGlobalUtilities.HostCheck)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, 24f * Vector2.UnitX.RotatedBy(NPC.ai[3]), ModContent.ProjectileType<YharimEXEyeWavy>(), 0, 0f, Main.myPlayer,
                      Main.rand.NextFloat(0.5f, 1.25f) * (Main.rand.NextBool() ? -1 : 1), Main.rand.Next(10, 60));
                }
            }

            int endTime = 1020;
            if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                endTime += 180;
            if (++NPC.ai[2] > endTime && NPC.life <= 1)
            {
                NPC.netUpdate = true;
                AttackChoice--;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                // NOTE    YharimEXGlobalUtilities.ClearAllProjectiles(2, NPC.whoAmI);
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
            NPC.ai[3] -= (float)Math.PI / 6f / 60f;
            NPC.velocity = Vector2.Zero;
            bool killPlayer = YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode;
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                if (Main.player[NPC.target].HasBuff(FargoSouls().Find<ModBuff>("TimeFrozenBuff").Type))
                {
                    killPlayer = true;
                }
            }
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
            if (++NPC.ai[1] > 120)
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
                if (YharimEXGlobalUtilities.HostCheck && !NPC.AnyNPCs(ModContent.NPCType<TheGodseeker>()))
                {
                    int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<TheGodseeker>());
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

        #endregion


        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (YharimEXWorldFlags.DeathMode & !YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                target.YharimPlayer().MaxLifeReduction += 100;
            }
            else if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                EternityDebuffs.ManageOnHitDebuffs(target);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 3; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.SolarFlare, 0f, 0f, 0, default, 1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 3f;
            }
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            if (YharimEXWorldFlags.AngryYharimEX)
                modifiers.FinalDamage *= 0.07f;
        }

        public override bool CheckDead()
        {
            if (AttackChoice == -7)
                return true;

            NPC.life = 1;
            NPC.active = true;
            if (YharimEXGlobalUtilities.HostCheck && AttackChoice > -1)
            {
                AttackChoice = (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) ? AttackChoice >= 10 ? -1 : 10 : -6;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                NPC.localAI[0] = 0;
                NPC.localAI[1] = 0;
                NPC.localAI[2] = 0;
                NPC.dontTakeDamage = true;
                NPC.netUpdate = true;
                YharimEXGlobalUtilities.ClearAllProjectiles(2, NPC.whoAmI, AttackChoice < 0);
            }
            return false;
        }

        public override void OnKill()
        {
            base.OnKill();

            //    if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) || (!playerInvulTriggered && (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)))
            //    {
            //        Item.NewItem(NPC.GetSource_Loot(), NPC.Hitbox, ModContent.ItemType<PhantasmalEnergy>());
            //    }

            YharimEXWorldFlags.SkipYharimEXP1 = 0;

            NPC.SetEventFlagCleared(ref YharimEXWorldFlags.downedYharimEX, -1);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            base.ModifyNPCLoot(npcLoot);

            //    npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<MutantBag>()));
            //    npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<MutantTrophy>(), 10));

            //    npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<MutantRelic>()));
            //    npcLoot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<SpawnSack>(), 4));

            //    LeadingConditionRule emodeRule = new(new EModeDropCondition());
            //    emodeRule.OnSuccess(YharimEXGlobalUtilities.BossBagDropCustom(ModContent.ItemType<Items.Accessories.Masomode.MutantEye>()));
            //    npcLoot.Add(emodeRule);
        }

        //  public override void BossLoot(ref string name, ref int potionType)
        //  {
        //    potionType = ItemID.SuperHealingPotion;
        //  }

        public override void FindFrame(int frameHeight)
        {
            if (++NPC.frameCounter > 4)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= Main.npcFrameCount[NPC.type] * frameHeight)
                    NPC.frame.Y = 0;
            }
        }

        // Not Used in Base Fargos, so probably don't use this 

        //    public override void BossHeadSpriteEffects(ref SpriteEffects spriteEffects)
        //    {
        //        spriteEffects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        //    }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            Vector2 position = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);
            Rectangle rectangle = NPC.frame;
            Vector2 origin2 = rectangle.Size() / 2f;

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Main.EntitySpriteDraw(texture2D13, position, new Rectangle?(rectangle), NPC.GetAlpha(drawColor), NPC.rotation, origin2, NPC.scale, effects, 0);

            Vector2 auraPosition = AuraCenter;
            if (ShouldDrawAura)
                DrawAura(spriteBatch, auraPosition, AuraScale);

            return false;
        }

        public void DrawAura(SpriteBatch spriteBatch, Vector2 position, float auraScale)
        {
            Color outerColor = Color.DarkRed;
            outerColor.A = 0;
            Color darkColor = outerColor;
            Color mediumColor = Color.Lerp(outerColor, Color.Red, 0.75f);
            Color lightColor2 = Color.Lerp(outerColor, Color.OrangeRed, 0.5f);

            Vector2 auraPos = position;
            float radius = 2000f * auraScale;
            var target = Main.LocalPlayer;
            var blackTile = TextureAssets.MagicPixel;
            var diagonalNoise = YharimEXTextureRegistry.WavyNoise;
            if (!blackTile.IsLoaded || !diagonalNoise.IsLoaded)
                return;
            var maxOpacity = NPC.Opacity;

            ManagedShader borderShader = ShaderManager.GetShader("YharimEX.YharimEXP1Aura");
            borderShader.TrySetParameter("colorMult", 7.35f);
            borderShader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
            borderShader.TrySetParameter("radius", radius);
            borderShader.TrySetParameter("anchorPoint", auraPos);
            borderShader.TrySetParameter("screenPosition", Main.screenPosition);
            borderShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
            borderShader.TrySetParameter("playerPosition", target.Center);
            borderShader.TrySetParameter("maxOpacity", maxOpacity);
            borderShader.TrySetParameter("darkColor", darkColor.ToVector4());
            borderShader.TrySetParameter("midColor", mediumColor.ToVector4());
            borderShader.TrySetParameter("lightColor", lightColor2.ToVector4());

            spriteBatch.GraphicsDevice.Textures[1] = diagonalNoise.Value;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, borderShader.WrappedEffect, Main.GameViewMatrix.TransformationMatrix);
            Rectangle rekt = new(Main.screenWidth / 2, Main.screenHeight / 2, Main.screenWidth, Main.screenHeight);
            spriteBatch.Draw(blackTile.Value, rekt, null, default, 0f, blackTile.Value.Size() * 0.5f, 0, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            //spriteBatch.Draw(FargosTextureRegistry.SoftEdgeRing.Value, position, null, outerColor * 0.7f, 0f, FargosTextureRegistry.SoftEdgeRing.Value.Size() * 0.5f, 9.2f * auraScale, SpriteEffects.None, 0f);
        }

        public static void ArenaAura(Vector2 center, float distance, bool reverse = false, int dustid = -1, Color color = default, params int[] buffs)
        {
            Player p = Main.LocalPlayer;


            if (buffs.Length == 0 || buffs[0] < 0)
                return;

            //works because buffs are client side anyway :ech:
            float range = center.Distance(p.Center);
            if (p.active && !p.dead && !p.ghost && (reverse ? range > distance && range < Math.Max(3000f, distance * 2) : range < distance))
            {
                foreach (int buff in buffs)
                {
                    YharimEXGlobalUtilities.AddDebuffFixedDuration(p, buff, 2);
                }
            }
        }
    }
}