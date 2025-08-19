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
        #region Stuff
        public override string Texture => "YharimEX/Assets/NPCs/Boss/YharimEXBoss";
        public override string BossHeadTexture => "YharimEX/Assets/NPCs/Boss/YharimEXBoss_Head";
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
                Mod FargoSouls = YharimEXCrossmodSystem.FargowiltasSouls.Mod;
                NPC.AddDebuffImmunities(
                [
                    FargosSouls.Find<ModBuff>("LethargicBuff").Type,
                    FargosSouls.Find<ModBuff>("ClippedWingsBuff").Type,
                    FargosSouls.Find<ModBuff>("MutantNibbleBuff").Type,
                    FargosSouls.Find<ModBuff>("OceanicMaulBuff").Type,
                    FargosSouls.Find<ModBuff>("LightningRodBuff").Type,
                    FargosSouls.Find<ModBuff>("SadismBuff").Type,
                    FargosSouls.Find<ModBuff>("GodEaterBuff").Type,
                    FargosSouls.Find<ModBuff>("TimeFrozenBuff").Type,
                    FargosSouls.Find<ModBuff>("LeadPoisonBuff").Type,
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
            NPC.width = 120;
            NPC.height = 120;
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
            if (target.type == ModContent.NPCType<TheGodseeker>())
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
                if (((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode) || YharimEXWorldFlags.InfernumMode) && !Main.dedServ)
                {
                    if (!Main.LocalPlayer.ItemTimeIsZero && (Main.LocalPlayer.HeldItem.type == ItemID.RodofDiscord || Main.LocalPlayer.HeldItem.type == ItemID.RodOfHarmony))
                        Main.LocalPlayer.AddBuff(FargosSouls.Find<ModBuff>("TimeFrozenBuff").Type, 600);
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

        private Mod FargosSouls
        {
            get
            {
                if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                {
                    return YharimEXCrossmodSystem.FargowiltasSouls.Mod;
                }
                return null;
            }
        }
        #endregion
        public override void AI()
        {
            YharimEXGlobalNPC.yharimEXBoss = NPC.whoAmI;
            NPC.dontTakeDamage = AttackChoice < 0; //invul in p3

            NPC.dontTakeDamage = AttackChoice < 0;

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
                case 23:
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

                case 45: PrepareMutantSword(); break;
                case 46: MutantSword(); break;

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
            //manage arena position
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
                if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                    time = Main.getGoodWorld ? 5000 : 4350;
                int drain = NPC.lifeMax / time;
                NPC.life -= drain;
                if (NPC.life < 1)
                    NPC.life = 1;
            }

            if (player.immune || player.hurtCooldowns[0] != 0 || player.hurtCooldowns[1] != 0)
                playerInvulTriggered = true;
            //drop summon
            if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) && !YharimEXWorldFlags.DownedYharimEX && YharimEXGlobalUtilities.HostCheck && NPC.HasPlayerTarget && !droppedSummon)
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
        #region Stuff
        bool spawned;
        void ManageAurasAndPreSpawn()
        {
            if (!spawned)
            {
                spawned = true;

                int prevLifeMax = NPC.lifeMax;
                if (YharimEXWorldFlags.AngryYharimEX) //doing it here to avoid overflow i think
                {
                    NPC.lifeMax *= 100;
                    if (NPC.lifeMax < prevLifeMax)
                        NPC.lifeMax = int.MaxValue;
                }
                NPC.life = NPC.lifeMax;

                if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                    EdgyBossText(GFBQuote(1));
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
                    if (YharimEXGlobalUtilities.HostCheck)
                    {
                        //if (FargowiltasSouls.Instance.MasomodeEXLoaded) Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModLoader.GetMod("MasomodeEX").ProjectileType("MutantText"), 0, 0f, Main.myPlayer, NPC.whoAmI);

                        if (YharimEXWorldFlags.AngryYharimEX && (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<YharimEXBossRush>(), 0, 0f, Main.myPlayer, NPC.whoAmI);
                    }
                }
            }
            else if (NPC.localAI[3] == 1)
            {
                ShouldDrawAura = true;
                // -1 means no dust is drawn, as it looks ugly.
                if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                {
                    ArenaAura(AuraCenter, 2000f * AuraScale, true, -1, default, FargosSouls.Find<ModBuff>("GodEaterBuff").Type, ModContent.BuffType<TyrantPresenceBuff>());
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
                                Main.LocalPlayer.AddBuff(FargosSouls.Find<ModBuff>("GoldenStasisCDBuff").Type, 2);
                        }
                    }

                    if ((YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode) && AttackChoice < 0 && AttackChoice > -6)
                    {
                        Main.LocalPlayer.AddBuff(FargosSouls.Find<ModBuff>("GoldenStasisCDBuff").Type, 2);
                        if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
                        {
                            Main.LocalPlayer.AddBuff(FargosSouls.Find<ModBuff>("GoldenStasisCDBuff").Type, 2);
                        }
                        if ((YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode))
                        {
                            Main.LocalPlayer.AddBuff(ModContent.BuffType<TyrantDesperationBuff>(), 2);
                            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
                            {
                                Main.LocalPlayer.AddBuff(FargosSouls.Find<ModBuff>("TimeStopCDBuff").Type, 2);
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

            if (YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.DeathMode)
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
                    YharimEXGlobalUtilities.ClearHostileProjectiles(1, NPC.whoAmI);
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

            if (YharimEXWorldFlags.MasochistModeReal || YharimEXWorldFlags.InfernumMode)
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
            Mod FargoSouls = YharimEXCrossmodSystem.FargowiltasSouls.Mod;
            NPC.velocity = Vector2.Zero;
            if (fightIsOver)
            {
                Main.player[NPC.target].ClearBuff(FargoSouls.Find<ModBuff>("MutantFangBuff").Type);
                Main.player[NPC.target].ClearBuff(FargoSouls.Find<ModBuff>("AbomRebirthBuff").Type);
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
                    : 0.1f * -Vector2.UnitY.RotatedBy(MathHelper.TwoPi / max * i); //looks controlled during mutant p1 skip
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
        const string GFBLocPath = $"Mods.YharimEX.NPCs.YharimEXBoss.GFBText.";
        private string RandomObnoxiousQuote() => Language.GetTextValue($"{GFBLocPath}Random{Main.rand.Next(ObnoxiousQuoteCount)}");
        private string GFBQuote(int num) => Language.GetTextValue($"{GFBLocPath}Quote{num}");

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
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
                EdgyBossText(GFBQuote(34));
            }
            return false;
        }

        public override void OnKill()
        {
            base.OnKill();

            YharimEXWorldFlags.SkipYharimEXP1 = 0;

            NPC.SetEventFlagCleared(ref YharimEXWorldFlags.downedYharimEX, -1);
        }



        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            base.ModifyNPCLoot(npcLoot);

            npcLoot.Add(ModContent.ItemType<YharimsJournal>());
        }



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

        public override void BossHeadSpriteEffects(ref SpriteEffects spriteEffects)
        {
            //spriteEffects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        }

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
            Color outerColor = Color.Red;
            outerColor.A = 0;

            Color darkColor = outerColor;
            Color mediumColor = Color.Lerp(outerColor, Color.White, 0.75f);
            Color lightColor2 = Color.Lerp(outerColor, Color.White, 0.5f);

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
        #endregion
    }
}