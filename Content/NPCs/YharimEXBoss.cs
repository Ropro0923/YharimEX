using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Creative;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static System.Net.Mime.MediaTypeNames;
using YharimEX.Core;
using Fargowiltas;
using Fargowiltas.NPCs;
using Fargowiltas.Projectiles;
using Luminance.Core.ModCalls;
using ReLogic.Content;
using Terraria.Chat;
using Terraria.Graphics.Shaders;
using Terraria.UI;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Buffs.Souls;
using static Terraria.GameContent.Creative.CreativePowers;
using YharimEX.Core.Systems;
using YharimEX.Content.Projectiles;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Content.Projectiles;

namespace YharimEX.Content.NPCs
{
    [AutoloadBossHead]
    public class YharimEXBoss : ModNPC
    {
        public override string Texture => "YharimEX/Assets/NPCs/YharimEXBoss/YharimEXBoss";
        public override string BossHeadTexture => "YharimEX/Assets/NPCs/YharimEXBoss/YharimEXBossHead";

        public bool PlayerInvulTriggered;
        public int RitualProjectile;
        public int SpriteProjectile;
        public int RingProjectile;
        private bool DroppedSummon;
        public Queue<float> AttackHistory = new Queue<float>();
        public int AttackCount;
        public float EndTimeVariance;
        public bool ShouldDrawAura;
        private Vector2 SwordTarget = Vector2.Zero;
        public float[] NewAI = new float[4];
        private int ShouldDoSword;
        private bool Spawned;
        private float HistoryAttack1;
        private int ShouldChampion;
        private const int MUTANT_SWORD_SPACING = 80;
        private const int MUTANT_SWORD_MAX = 12;
        private bool MakedSword;
        private Player Player => Main.player[NPC.target];

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
            NPCID.Sets.NoMultiplayerSmoothingByType[NPC.type] = true;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(NPC.type);
            NPCID.Sets.MustAlwaysDraw[Type] = true;
            NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
            NPC.buffImmune[ModContent.BuffType<LethargicBuff>()] = true;
            NPC.buffImmune[ModContent.BuffType<ClippedWingsBuff>()] = true;
            NPC.buffImmune[ModContent.BuffType<MutantNibbleBuff>()] = true;
            NPC.buffImmune[ModContent.BuffType<OceanicMaulBuff>()] = true;
            NPC.buffImmune[ModContent.BuffType<LightningRodBuff>()] = true;
            NPC.buffImmune[ModContent.BuffType<SadismBuff>()] = true;
            NPC.buffImmune[ModContent.BuffType<GodEaterBuff>()] = true;
            NPC.buffImmune[ModContent.BuffType<TimeFrozenBuff>()] = true;
            NPC.buffImmune[ModContent.BuffType<LeadPoisonBuff>()] = true;
        }

        public override void PostAI()
        {
            if (!Player.HasBuff(ModContent.BuffType<TimeStopCDBuff>()))
            {
                Player.AddBuff(ModContent.BuffType<TimeStopCDBuff>(), 100, true, false);
            }
            base.PostAI();
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry BestiaryEntry)
        {
            BestiaryEntry.Info.AddRange([BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky, new FlavorTextBestiaryInfoElement($"Mods.YharimEX.Bestiary.{Name}")]);
        }

        public override void SetDefaults()
        {
            NPC.width = 120;
            NPC.height = 120;
            NPC.damage = 444;
            NPC.defense = 255;
            NPC.value = Item.buyPrice(7, 0, 0, 0);
            NPC.lifeMax = Main.expertMode ? 7700000 : 3500000;
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
            if (YharimEXWorld.YharimEXEnraged)
            {
                NPC.damage *= 17;
                NPC.defense *= 10;
            }
            if (ModLoader.TryGetMod("FargowiltasMusic", out Mod musicMod))
            {
                Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/rePrologue");
            }
            else
            {
                //Music = MusicLoader.GetMusicSlot(((ModType)this).Mod, "Sounds/Music/P1");
                Music = MusicID.OtherworldlyTowers;
            }
            SceneEffectPriority = SceneEffectPriority.BossHigh;
        }
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.damage = (int)(NPC.damage * 0.6f);
            NPC.lifeMax = (int)(NPC.lifeMax * 0.6f * balance);
        }

        public override bool CanHitPlayer(Player target, ref int CooldownSlot)
        {
            if (NPC.ai[0] >= 132f && (NPC.ai[0] <= 136f))
            {
                return false;
            }
            CooldownSlot = 1;
            if (NPC.Distance(YharimEXUtil.ClosestPointInHitbox((Entity)(object)target, NPC.Center)) < 42f)
            {
                return NPC.ai[0] > -1f;
            }
            return false;
        }

        public override bool CanHitNPC(NPC target)
        {
            if (target.boss)
                return false;
            return true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.localAI[0]);
            writer.Write(NPC.localAI[1]);
            writer.Write(NPC.localAI[2]);
            writer.Write(EndTimeVariance);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.localAI[0] = reader.ReadSingle();
            NPC.localAI[1] = reader.ReadSingle();
            NPC.localAI[2] = reader.ReadSingle();
            EndTimeVariance = reader.ReadSingle();
        }
        private void ClearNewAI()
        {
            for (int i = 0; i < 4; i++)
            {
                NewAI[i] = 0f;
            }
        }
        private void MovementY(float targetY, float speedModifier)
        {
            if (NPC.Center.Y < targetY)
            {
                NPC.velocity.Y += speedModifier;
                if (NPC.velocity.Y < 0f)
                {
                    NPC.velocity.Y += speedModifier * 2f;
                }
            }
            else
            {
                NPC.velocity.Y -= speedModifier;
                if (NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y -= speedModifier * 2f;
                }
            }
            if (Math.Abs(NPC.velocity.Y) > 24f)
            {
                NPC.velocity.Y = 24 * Math.Sign(NPC.velocity.Y);
            }
        }
        private void TeleportDust()
        {
            for (int index1 = 0; index1 < 25; index1++)
            {
                int index2 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.WitherLightning, 0f, 0f, 100, default, 2f);
                Main.dust[index2].noGravity = true;
                Dust obj = Main.dust[index2];
                obj.velocity *= 7f;
                Main.dust[index2].noLight = true;
                int index3 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.WitherLightning, 0f, 0f, 100, default, 1f);
                Dust obj2 = Main.dust[index3];
                obj2.velocity *= 4f;
                Main.dust[index3].noGravity = true;
                Main.dust[index3].noLight = true;
            }
        }
        private void StrongAttackTeleport(Vector2 teleportTarget = default(Vector2))
        {
            if (teleportTarget == default ? NPC.Distance(Player.Center) < 450f : NPC.Distance(teleportTarget) < 80f)
            {
                return;
            }
            TeleportDust();
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (teleportTarget != default)
                {
                    NPC.Center = teleportTarget;
                }
                else if (Player.velocity == Vector2.Zero)
                {
                    NPC.Center = Player.Center + 450f * Utils.RotatedByRandom(Vector2.UnitX, Math.PI * 2.0);
                }
                else
                {
                    NPC.Center = Player.Center + 450f * Vector2.Normalize(Player.velocity);
                }
                NPC nPC = NPC;
                ((Entity)nPC).velocity = ((Entity)nPC).velocity / 2f;
                NPC.netUpdate = true;
            }
            TeleportDust();
            SoundEngine.PlaySound(SoundID.Item84, (Vector2?)NPC.Center);
        }
        /*
        private void GoNextAI0()
        {
            NPC.ai[0] += 1f;
        }

        public override void AI()
        {
            if (YharimEXCrossmod.FargowiltasSouls.Loaded) 
            {
                EModeGlobalNPC.mutantBoss = NPC.whoAmI;
            }
            NPC.dontTakeDamage = NPC.ai[0] < 0f;
            ShouldDrawAura = false;
            ManageAurasAndPreSpawn();
            ManageNeededProjectiles();
            NPC.direction = (NPC.spriteDirection = (NPC.Center.X < Player.Center.X) ? 1 : (-1));
            bool drainLifeInP3 = true;
            Vector2 val;
            switch ((int)NPC.ai[0])
            {
                case 888:
                    {
                        if (NPC.localAI[0] == 0f)
                        {
                            Projectile[] projectile = Main.projectile;
                            foreach (Projectile p3 in projectile)
                            {
                                if (p3.type == ModContent.ProjectileType<MutantSphereRing>() && ((Entity)p3).active)
                                {
                                    p3.Kill();
                                }
                            }
                            Vector2 center2 = Player.Center;
                            Vector2 val2 = Utils.SafeNormalize(((Entity)NPC).Center - Player.Center, Vector2.Zero);
                            double num5 = (Utils.NextBool(Main.rand, 2) ? 0.7854f : (-0.7854f));
                            val = default(Vector2);
                            StrongAttackTeleport(center2 + Utils.RotatedBy(val2, num5, val) * 600f);
                            NPC.localAI[0] = 1f;
                        }
                        Vector2 targetPos = Vector2.Zero;
                        if ((NewAI[1] += 1f) < 150f)
                        {
                            ((Entity)NPC).velocity = Vector2.Zero;
                            if (NewAI[2] == 0f)
                            {
                                double angle = (NPC.position.X < (Player.position.X) ? (-Math.PI / 4.0) : (Math.PI / 4.0));
                                NewAI[2] = (float)angle * -4f / 30f;
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    IEntitySource source_FromThis = ((Entity)NPC).GetSource_FromThis((string)null);
                                    Vector2 center3 = ((Entity)NPC).Center;
                                    Vector2 unitY = Vector2.UnitY;
                                    val = default(Vector2);
                                    Projectile.NewProjectile(source_FromThis, center3 + -Utils.RotatedBy(unitY, angle, val) * 90f, Vector2.Zero, ModContent.ProjectileType<SparklingLove>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 2f), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 90f);
                                }
                                Vector2 unitY2 = Vector2.UnitY;
                                val = default(Vector2);
                                Vector2 offset2 = -Utils.RotatedBy(unitY2, angle, val) * 80f;
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    for (int n = 0; n < 8; n++)
                                    {
                                        SpawnAxeHitbox(((Entity)NPC).Center + offset2 * (float)n);
                                    }
                                    for (int num6 = 1; num6 < 3; num6++)
                                    {
                                        Vector2 val3 = ((Entity)NPC).Center + offset2 * 5f;
                                        double num7 = (0.0 - angle) * 2.0;
                                        val = default(Vector2);
                                        SpawnAxeHitbox(val3 + Utils.RotatedBy(offset2, num7, val) * (float)num6);
                                        Vector2 val4 = ((Entity)NPC).Center + offset2 * 6f;
                                        double num8 = (0.0 - angle) * 2.0;
                                        val = default(Vector2);
                                        SpawnAxeHitbox(val4 + Utils.RotatedBy(offset2, num8, val) * (float)num6);
                                    }
                                }
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    for (int num9 = 0; num9 < 4; num9++)
                                    {
                                        Vector2 val5 = new Vector2(80f, 80f);
                                        double num10 = (float)Math.PI / 2f * (float)num9;
                                        val = default(Vector2);
                                        Vector2 target = Utils.RotatedBy(val5, num10, val);
                                        Vector2 speed2 = 2f * target / 90f;
                                        float acceleration = (0f - ((Vector2)(speed2)).Length()) / 90f;
                                        int damage = (NPC.localAI[3] > 1f) ? YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f) : YharimEXUtil.ScaledProjectileDamage(NPC.damage);
                                        Projectile.NewProjectile(NPC.GetSource_FromThis(null), NPC.Center, speed2, ModContent.ProjectileType<DeviEnergyHeart>(), damage, 0f, Main.myPlayer, 0f, acceleration);
                                    }
                                }
                            }
                            NPC.direction = NPC.spriteDirection = Math.Sign(NewAI[2]);
                        }
                        else if (NewAI[1] == 150f)
                        {
                            targetPos = Player.Center;
                            targetPos.X -= 360 * Math.Sign(NewAI[2]);
                            NPC.velocity = (targetPos - NPC.Center) / 30f;
                            NPC.netUpdate = true;
                            NPC.direction = (NPC.spriteDirection = Math.Sign(NewAI[2]));
                            if (!FargoSoulsWorld.MasochistModeReal && Math.Sign(targetPos.X - ((Entity)NPC).Center.X) != Math.Sign(NewAI[2]))
                            {
                                ((Entity)NPC).velocity.X *= 0.5f;
                            }
                        }
                        else if (NewAI[1] < 180f)
                        {
                            NewAI[3] += NewAI[2];
                            ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(NewAI[2]));
                        }
                        else
                        {
                            targetPos = Player.Center + Player.DirectionTo(NPC.Center) * 400f;
                            if (NPC.Distance(targetPos) > 50f)
                            {
                                Movement(targetPos, 0.2f);
                            }
                            if (NewAI[1] > 300f)
                            {
                                ClearNewAI();
                                ChooseNextAttack(44, 45, 26, 29);
                            }
                        }
                        break;
                    }
                case 132:
                    if (!(NewAI[1] < 90f) || AliveCheck(Player))
                    {
                        if (NewAI[2] == 0f && NewAI[3] == 0f)
                        {
                            NewAI[2] = NPC.Center.X + ((Player.Center.X < NPC.Center.X) ? (-1000) : 1000);
                        }
                        if (NPC.localAI[2] == 0f)
                        {
                            NPC.localAI[2] = ((!(NewAI[2] > ((Entity)NPC).Center.X)) ? 1 : (-1));
                        }
                        if (NewAI[1] > 90f)
                        {
                            FancyFireballs2((int)NewAI[1] - 90);
                        }
                        else
                        {
                            bool k2 = Utils.NextBool(Main.rand, 2);
                            NewAI[3] = Player.Center.Y + (k2 ? 1000 : (-1000));
                            NewAI[0] = ((!k2) ? 1 : (-1));
                        }
                        Vector2 targetPos4 = new Vector2(NewAI[2], NewAI[3]);
                        Movement(targetPos4, 1.4f);
                        if ((NewAI[1] += 1f) > 150f)
                        {
                            SoundEngine.PlaySound(SoundID.Roar, (Vector2?)NPC.Center);
                            NPC.netUpdate = true;
                            NPC.ai[0] += 1f;
                            NewAI[1] = 0f;
                            NewAI[2] = NPC.localAI[2];
                            NewAI[3] = 0f;
                            NPC.localAI[2] = 0f;
                        }
                    }
                    break;
                case 133:
                    {
                        ((Entity)NPC).velocity.X = NewAI[2] * 12f;
                        ((Entity)NPC).velocity.Y = NewAI[0] * 12f;
                        Vector2 val7 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                        val = default(Vector2);
                        Vector2 v = Utils.RotatedBy(val7, 1.5707000494003296, val);
                        Vector2 d = ((Entity)Player).Center - ((Entity)NPC).Center;
                        float num17 = v.X * d.X + v.Y * d.Y;
                        float Dis = ((Entity)NPC).Distance(((Entity)Player).Center);
                        if (num17 > 300f)
                        {
                            Vector2 val8 = ((Entity)Player).Center - ((Entity)NPC).Center;
                            Vector2 val9 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                            val = default(Vector2);
                            val = val8 - Utils.RotatedBy(val9, 1.5707000494003296, val) * 50f;
                            if (((Vector2)(val)).Length() < Dis)
                            {
                                NPC nPC4 = NPC;
                                Vector2 velocity2 = ((Entity)nPC4).velocity;
                                Vector2 val10 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                                val = default(Vector2);
                                ((Entity)nPC4).velocity = velocity2 + Utils.RotatedBy(val10, 1.5707000494003296, val) * 3f;
                            }
                            else
                            {
                                NPC nPC5 = NPC;
                                Vector2 velocity3 = ((Entity)nPC5).velocity;
                                Vector2 val11 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                                val = default(Vector2);
                                ((Entity)nPC5).velocity = velocity3 + Utils.RotatedBy(val11, -1.5707000494003296, val) * 3f;
                            }
                        }
                        if (num17 <= 300f)
                        {
                            Vector2 val12 = ((Entity)Player).Center - ((Entity)NPC).Center;
                            Vector2 val13 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                            val = default(Vector2);
                            val = val12 - Utils.RotatedBy(val13, 1.5707000494003296, val) * 50f;
                            if (((Vector2)(val)).Length() < Dis)
                            {
                                NPC nPC6 = NPC;
                                Vector2 velocity4 = ((Entity)nPC6).velocity;
                                Vector2 val14 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                                val = default(Vector2);
                                ((Entity)nPC6).velocity = velocity4 - Utils.RotatedBy(val14, 1.5707000494003296, val) * 3f;
                            }
                            else
                            {
                                NPC nPC7 = NPC;
                                Vector2 velocity5 = ((Entity)nPC7).velocity;
                                Vector2 val15 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                                val = default(Vector2);
                                ((Entity)nPC7).velocity = velocity5 - Utils.RotatedBy(val15, -1.5707000494003296, val) * 3f;
                            }
                        }
                    ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(((Entity)NPC).velocity.X));
                        if ((NewAI[3] += 1f) > 5f)
                        {
                            NewAI[3] = 0f;
                            SoundEngine.PlaySound(SoundID.Item12, (Vector2?)((Entity)NPC).Center);
                            float timeLeft = 2400f / Math.Abs(((Entity)NPC).velocity.X) * 2f - NewAI[1] + 120f;
                            if (NewAI[1] <= 15f)
                            {
                                timeLeft = 0f;
                            }
                            else
                            {
                                if (NPC.localAI[2] != 0f)
                                {
                                    timeLeft = 0f;
                                }
                                if ((NPC.localAI[2] += 1f) > 2f)
                                {
                                    NPC.localAI[2] = 0f;
                                }
                            }
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float R = Utils.ToRotation(new Vector2(NewAI[2], NewAI[0]));
                                IEntitySource source_FromThis2 = ((Entity)NPC).GetSource_FromThis((string)null);
                                Vector2 center6 = ((Entity)NPC).Center;
                                Vector2 unitY3 = Vector2.UnitY;
                                double num18 = (double)MathHelper.ToRadians(20f) * (Main.rand.NextDouble() - 0.5) + (double)R;
                                val = default(Vector2);
                                Projectile.NewProjectile(source_FromThis2, center6, Utils.RotatedBy(unitY3, num18, val), ModContent.ProjectileType<AbomDeathrayMark>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.5f), 0f, Main.myPlayer, timeLeft, 0f);
                                IEntitySource source_FromThis3 = ((Entity)NPC).GetSource_FromThis((string)null);
                                Vector2 center7 = ((Entity)NPC).Center;
                                Vector2 unitY4 = Vector2.UnitY;
                                double num19 = (double)MathHelper.ToRadians(20f) * (Main.rand.NextDouble() - 0.5) + (double)R;
                                val = default(Vector2);
                                Projectile.NewProjectile(source_FromThis3, center7, -Utils.RotatedBy(unitY4, num19, val), ModContent.ProjectileType<AbomDeathrayMark>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.5f), 0f, Main.myPlayer, timeLeft, 0f);
                            }
                        }
                        if ((NewAI[1] += 1f) > 218.18182f)
                        {
                            NPC.netUpdate = true;
                            ((Entity)NPC).velocity.X = NewAI[2] * 18f;
                            NPC.ai[0] += 1f;
                            NewAI[1] = 0f;
                            NewAI[3] = 0f;
                        }
                        break;
                    }
                case 134:
                    if (!(NewAI[1] < 150f) || AliveCheck(Player))
                    {
                        ((Entity)NPC).velocity.Y = 0f;
                        NPC nPC13 = NPC;
                        ((Entity)nPC13).velocity = ((Entity)nPC13).velocity * 0.947f;
                        NewAI[3] += ((Vector2)NPC.velocity).Length();
                        if (NewAI[1] > 150f)
                        {
                            FancyFireballs2((int)NewAI[1] - 150);
                        }
                        if ((NewAI[1] += 1f) > 210f)
                        {
                            SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                            NPC.netUpdate = true;
                            NPC.ai[0] += 1f;
                            NewAI[1] = 0f;
                            NewAI[3] = 0f;
                            NewAI[2] = 0f - NewAI[2];
                            NewAI[0] = 0f - NewAI[0];
                        }
                    }
                    break;
                case 135:
                    {
                        ((Entity)NPC).velocity.X = NewAI[2] * 12f;
                        ((Entity)NPC).velocity.Y = NewAI[0] * 12f;
                        Vector2 val16 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                        val = default(Vector2);
                        Vector2 v = Utils.RotatedBy(val16, 1.5707000494003296, val);
                        Vector2 d = Player.Center - NPC.Center;
                        float num22 = v.X * d.X + v.Y * d.Y;
                        float Dis = NPC.Distance(Player.Center);
                        if (num22 > 300f)
                        {
                            Vector2 val17 = Player.Center - NPC.Center;
                            Vector2 val18 = Utils.SafeNormalize(NPC.velocity, Vector2.Zero);
                            val = default(Vector2);
                            val = val17 - Utils.RotatedBy(val18, 1.5707000494003296, val) * 2f;
                            if (((Vector2)(val)).Length() < Dis)
                            {
                                NPC nPC9 = NPC;
                                Vector2 velocity6 = ((Entity)nPC9).velocity;
                                Vector2 val19 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                                val = default(Vector2);
                                ((Entity)nPC9).velocity = velocity6 + Utils.RotatedBy(val19, 1.5707000494003296, val) * 3f;
                            }
                            else
                            {
                                NPC nPC10 = NPC;
                                Vector2 velocity7 = ((Entity)nPC10).velocity;
                                Vector2 val20 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                                val = default(Vector2);
                                ((Entity)nPC10).velocity = velocity7 + Utils.RotatedBy(val20, -1.5707000494003296, val) * 3f;
                            }
                        }
                        else
                        {
                            Vector2 val21 = Player.Center - NPC.Center;
                            Vector2 val22 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                            val = default(Vector2);
                            val = val21 - Utils.RotatedBy(val22, 1.5707000494003296, val) * 2f;
                            if (((Vector2)(val)).Length() < Dis)
                            {
                                NPC nPC11 = NPC;
                                Vector2 velocity8 = ((Entity)nPC11).velocity;
                                Vector2 val23 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                                val = default(Vector2);
                                ((Entity)nPC11).velocity = velocity8 - Utils.RotatedBy(val23, 1.5707000494003296, val) * 3f;
                            }
                            else
                            {
                                NPC nPC12 = NPC;
                                Vector2 velocity9 = ((Entity)nPC12).velocity;
                                Vector2 val24 = Utils.SafeNormalize(((Entity)NPC).velocity, Vector2.Zero);
                                val = default(Vector2);
                                ((Entity)nPC12).velocity = velocity9 - Utils.RotatedBy(val24, -1.5707000494003296, val) * 3f;
                            }
                        }
                    ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(((Entity)NPC).velocity.X));
                        if ((NewAI[3] += 1f) > 5f)
                        {
                            NewAI[3] = 0f;
                            SoundEngine.PlaySound(SoundID.Item12, (Vector2?)((Entity)NPC).Center);
                            float timeLeft2 = 2400f / Math.Abs(((Entity)NPC).velocity.X) * 2f - NewAI[1] + 120f;
                            if (NewAI[1] <= 15f)
                            {
                                timeLeft2 = 0f;
                            }
                            else
                            {
                                if (NPC.localAI[2] != 0f)
                                {
                                    timeLeft2 = 0f;
                                }
                                if ((NPC.localAI[2] += 1f) > 2f)
                                {
                                    NPC.localAI[2] = 0f;
                                }
                            }
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float R2 = Utils.ToRotation(new Vector2(NewAI[2], NewAI[0]));
                                IEntitySource source_FromThis4 = ((Entity)NPC).GetSource_FromThis((string)null);
                                Vector2 center8 = ((Entity)NPC).Center;
                                Vector2 unitY5 = Vector2.UnitY;
                                double num23 = (double)MathHelper.ToRadians(20f) * (Main.rand.NextDouble() - 0.5) + (double)R2;
                                val = default(Vector2);
                                Projectile.NewProjectile(source_FromThis4, center8, Utils.RotatedBy(unitY5, num23, val), ModContent.ProjectileType<AbomDeathrayMark>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.5f), 0f, Main.myPlayer, timeLeft2, 0f);
                                IEntitySource source_FromThis5 = ((Entity)NPC).GetSource_FromThis((string)null);
                                Vector2 center9 = ((Entity)NPC).Center;
                                Vector2 unitY6 = Vector2.UnitY;
                                double num24 = (double)MathHelper.ToRadians(20f) * (Main.rand.NextDouble() - 0.5) + (double)R2;
                                val = default(Vector2);
                                Projectile.NewProjectile(source_FromThis5, center9, -Utils.RotatedBy(unitY6, num24, val), ModContent.ProjectileType<AbomDeathrayMark>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.5f), 0f, Main.myPlayer, timeLeft2, 0f);
                            }
                        }
                        if (!((NewAI[1] += 1f) > 218.18182f))
                        {
                            break;
                        }
                        NPC.netUpdate = true;
                        ((Entity)NPC).velocity.X = NewAI[2] * -18f;
                        NewAI[0] = 0f;
                        ChooseNextAttack(11, 13, 19, 33, 24, 41, 44, 45);
                        Projectile[] projectile = Main.projectile;
                        foreach (Projectile proj2 in projectile)
                        {
                            if (proj2.type == ModContent.ProjectileType<AbomDeathrayMark>())
                            {
                                ((AbomDeathrayMark)(object)proj2.ModProjectile).DontS = true;
                            }
                            if (proj2.type == ModContent.ProjectileType<AbomDeathray>())
                            {
                                ((AbomDeathray)(object)proj2.ModProjectile).DontSpawn = true;
                            }
                            if (proj2.type == ModContent.ProjectileType<AbomScytheSplit>())
                            {
                                proj2.Kill();
                            }
                        }
                        ShouldDoSword = 0;
                        NewAI[1] = 0f;
                        NewAI[2] = 0f;
                        NewAI[3] = 0f;
                        break;
                    }
                case 1919:
                    if (NewAI[1] == 1f)
                    {
                        SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                    }
                    if (NewAI[2] <= 114f)
                    {
                        Vector2 targetPos = Player.Center;
                        targetPos.X += 600 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
                        NPC nPC18 = NPC;
                        ((Entity)nPC18).position = ((Entity)nPC18).position + Player.velocity / 3f;
                        Movement(targetPos, 1.2f);
                    }
                    if (NewAI[2] == 114f)
                    {
                        Projectile[] projectile = Main.projectile;
                        foreach (Projectile p5 in projectile)
                        {
                            if (p5.type == ModContent.ProjectileType<MutantSphereRing>() && ((Entity)p5).active)
                            {
                                p5.Kill();
                            }
                        }
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<GlowRingEX>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, -20f);
                        }
                    }
                    if ((NewAI[2] += 1f) <= 315f)
                    {
                        Vector2 targetPos = Player.Center;
                        targetPos.X += 600 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
                        NPC nPC19 = NPC;
                        nPC19.position = nPC19.position + Player.velocity / 3f;
                        Movement(targetPos, 1.2f);
                        if ((NPC.localAI[0] -= 1f) < 0f && NewAI[2] > 114f)
                        {
                            NPC.localAI[0] = 90f;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int j2 = -1; j2 <= 1; j2 += 2)
                                {
                                    for (int i2 = -11; i2 <= 11; i2++)
                                    {
                                        Vector2 target4 = Player.Center;
                                        target4.X += 180f * (float)i2;
                                        target4.Y += (400f + 27.272728f * (float)Math.Abs(i2)) * (float)j2;
                                        Vector2 speed5 = (target4 - ((Entity)NPC).Center) / 20f;
                                        int individualTiming = 60 + Math.Abs(i2 * 2);
                                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, speed5 / 2f, ModContent.ProjectileType<CosmosSphere>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 20f, (float)individualTiming);
                                    }
                                }
                            }
                        }
                        NPC.rotation = Utils.ToRotation(((Entity)NPC).DirectionTo(((Entity)Player).Center));
                        if (((Entity)NPC).direction < 0)
                        {
                            NPC nPC20 = NPC;
                            nPC20.rotation += (float)Math.PI;
                        }
                        NewAI[3] = ((((Entity)NPC).Center.X < ((Entity)Player).Center.X) ? 1 : (-1));
                        if (NewAI[2] == 315f)
                        {
                            ((Entity)NPC).velocity = 42f * ((Entity)NPC).DirectionTo(((Entity)Player).Center);
                            NPC.netUpdate = true;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int modifier4 = Math.Sign(((Entity)NPC).Center.Y - ((Entity)Player).Center.Y);
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center + 3000f * ((Entity)NPC).DirectionFrom(((Entity)Player).Center) * (float)modifier4, ((Entity)NPC).DirectionTo(((Entity)Player).Center) * (float)modifier4, ModContent.ProjectileType<CosmosDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                            }
                        }
                    }
                    else
                    {
                        ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(NewAI[3]));
                    }
                    if ((NewAI[1] += 1f) > 400f || (NewAI[2] > 315f && ((NewAI[3] > 0f) ? (((Entity)NPC).Center.X > ((Entity)Player).Center.X + 800f) : (((Entity)NPC).Center.X < ((Entity)Player).Center.X - 800f))))
                    {
                        ((Entity)NPC).velocity.X = 0f;
                        NPC.TargetClosest(true);
                        GoNextAI0();
                        NewAI[1] = 0f;
                        NewAI[2] = 0f;
                        NewAI[3] = 0f;
                        NPC.localAI[0] = 0f;
                        NPC.netUpdate = true;
                    }
                    break;
                case 1920:
                    if ((NewAI[1] += 1f) < 110f)
                    {
                        Vector2 targetPos = ((Entity)Player).Center;
                        targetPos.X += 300 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
                        if (((Entity)NPC).Distance(targetPos) > 50f)
                        {
                            Movement(targetPos, 0.8f);
                        }
                        if (NewAI[1] == 1f)
                        {
                            SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                        }
                        if ((NewAI[2] += 1f) <= 6f)
                        {
                            NPC.rotation = Utils.ToRotation(((Entity)NPC).DirectionTo(((Entity)Player).Center));
                            if (((Entity)NPC).direction < 0)
                            {
                                NPC nPC14 = NPC;
                                nPC14.rotation += (float)Math.PI;
                            }
                            NewAI[3] = ((((Entity)NPC).Center.X < ((Entity)Player).Center.X) ? 1 : (-1));
                            if (NewAI[2] != 6f)
                            {
                                break;
                            }
                            NPC.netUpdate = true;
                            if (NewAI[1] > 50f)
                            {
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    Vector2 offset5 = Vector2.UnitX;
                                    if (((Entity)NPC).direction < 0)
                                    {
                                        offset5.X *= -1f;
                                    }
                                    Vector2 val25 = offset5;
                                    double num25 = Utils.ToRotation(((Entity)NPC).DirectionTo(((Entity)Player).Center));
                                    val = default(Vector2);
                                    offset5 = Utils.RotatedBy(val25, num25, val);
                                    int modifier = Math.Sign(((Entity)NPC).Center.Y - ((Entity)Player).Center.Y);
                                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center + offset5 + 3000f * ((Entity)NPC).DirectionFrom(((Entity)Player).Center) * (float)modifier, ((Entity)NPC).DirectionTo(((Entity)Player).Center) * (float)modifier, ModContent.ProjectileType<CosmosDeathray>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                                }
                            }
                            else
                            {
                                NewAI[2] = 0f;
                            }
                        }
                        else
                        {
                            ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(NewAI[3]));
                            if (NewAI[2] > 12f)
                            {
                                NewAI[2] = 0f;
                                NewAI[3] = 0f;
                                NPC.netUpdate = true;
                            }
                        }
                        break;
                    }
                    if (NewAI[1] <= 155f)
                    {
                        Vector2 targetPos = ((Entity)Player).Center;
                        targetPos.X += 350 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
                        targetPos.Y += 700f;
                        NPC nPC15 = NPC;
                        ((Entity)nPC15).position = ((Entity)nPC15).position + ((Entity)Player).velocity / 3f;
                        Movement(targetPos, 2.4f);
                        NPC.rotation = Utils.ToRotation(((Entity)NPC).DirectionTo(((Entity)Player).Center));
                        if (((Entity)NPC).direction < 0)
                        {
                            NPC nPC16 = NPC;
                            nPC16.rotation += (float)Math.PI;
                        }
                        if (NewAI[1] == 155f)
                        {
                            ((Entity)NPC).velocity = 42f * ((Entity)NPC).DirectionTo(((Entity)Player).Center);
                            NPC.netUpdate = true;
                            NewAI[3] = Math.Abs(((Entity)Player).Center.Y - ((Entity)NPC).Center.Y) / 42f;
                            NewAI[3] *= 2f;
                            NPC.localAI[0] = ((Entity)Player).Center.X;
                            NPC.localAI[1] = ((Entity)Player).Center.Y;
                            NPC.localAI[0] += ((((Entity)NPC).Center.X < ((Entity)Player).Center.X) ? (-50) : 50);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int modifier2 = Math.Sign(((Entity)NPC).Center.Y - ((Entity)Player).Center.Y);
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center + 3000f * ((Entity)NPC).DirectionFrom(((Entity)Player).Center) * (float)modifier2, ((Entity)NPC).DirectionTo(((Entity)Player).Center) * (float)modifier2, ModContent.ProjectileType<CosmosDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                            }
                        }
                        break;
                    }
                ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(((Entity)NPC).velocity.X));
                    NPC.rotation = Utils.ToRotation(((Entity)NPC).velocity);
                    if (((Entity)NPC).direction < 0)
                    {
                        NPC nPC17 = NPC;
                        nPC17.rotation += (float)Math.PI;
                    }
                    if (Math.Abs(((Entity)NPC).Center.Y - NPC.localAI[1]) < 300f)
                    {
                        Vector2 val26 = ((Entity)NPC).Center - ((Entity)NPC).velocity / 2f;
                        Vector2 target2 = new Vector2(NPC.localAI[0], NPC.localAI[1]);
                        Vector2 vel4 = Vector2.Normalize(val26 - target2);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int modifier3 = ((Math.Sign(((Entity)Player).Center.X - target2.X) == ((Entity)NPC).direction) ? 1 : (-1));
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, (float)modifier3 * 0.5f * vel4, ModContent.ProjectileType<CosmosBolt>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, (float)modifier3 * 0.5f * ((Entity)NPC).DirectionFrom(target2), ModContent.ProjectileType<CosmosBolt>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                        }
                    }
                    else if ((NewAI[2] += 1f) > 1f)
                    {
                        NewAI[2] = 0f;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 target3 = new Vector2(NPC.localAI[0], NPC.localAI[1]);
                            Math.Sign(((Entity)Player).Center.X - target3.X);
                            _ = ((Entity)NPC).direction;
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, 0.5f * ((Entity)NPC).DirectionFrom(target3), ModContent.ProjectileType<CosmosBolt>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                        }
                    }
                    if (NewAI[1] > 155f + NewAI[3])
                    {
                        ((Entity)NPC).velocity.Y = 0f;
                        NPC.TargetClosest(true);
                        NPC.ai[0] = 1922f;
                        NewAI[1] = (NPC.localAI[2] == 0f) ? (-120) : 0;
                        NewAI[2] = 0f;
                        NewAI[3] = 0f;
                        NPC.localAI[0] = 0f;
                        NPC.localAI[1] = 0f;
                        NPC.netUpdate = true;
                    }
                    break;
                case 1921:
                    GoNextAI0();
                    break;
                case 1922:
                    {
                        Vector2 targetPos = ((Entity)Player).Center + ((Entity)NPC).DirectionFrom(((Entity)Player).Center) * 500f;
                        if (NewAI[1] < 130f || (((Entity)NPC).Distance(((Entity)Player).Center) > 200f && ((Entity)NPC).Distance(((Entity)Player).Center) < 600f))
                        {
                            NPC nPC2 = NPC;
                            ((Entity)nPC2).velocity = ((Entity)nPC2).velocity * 0.97f;
                        }
                        else if (((Entity)NPC).Distance(targetPos) > 50f)
                        {
                            Movement(targetPos, 0.8f);
                            NPC nPC3 = NPC;
                            ((Entity)nPC3).position = ((Entity)nPC3).position + ((Entity)Player).velocity / 4f;
                        }
                        if (NewAI[1] >= 10f && Main.netMode != NetmodeID.MultiplayerClient && ((EffectManager<Filter>)(object)Terraria.Graphics.Effects.Filters.Scene)["FargowiltasSouls:Invert"].IsActive())
                        {
                            ((EffectManager<Filter>)(object)Terraria.Graphics.Effects.Filters.Scene)["FargowiltasSouls:Invert"].GetShader().UseTargetPosition(((Entity)NPC).Center);
                        }
                        if (NewAI[1] == 10f)
                        {
                            NPC.localAI[0] = Utils.NextFloat(Main.rand, (float)Math.PI * 2f);
                            if (!Main.dedServ)
                            {
                                SoundStyle val6 = new SoundStyle("FargowiltasSouls/Sounds/ZaWarudo", (SoundType)0);
                                SoundEngine.PlaySound(val6, (Vector2?)((Entity)Player).Center);
                            }
                        }
                        else if (NewAI[1] < 210f)
                        {
                            int duration = 60 + Math.Max(2, 210 - (int)NewAI[1]);
                            if (((Entity)Main.LocalPlayer).active && !Main.LocalPlayer.dead)
                            {
                                Main.LocalPlayer.AddBuff(ModContent.BuffType<TimeFrozen>(), duration, true, false);
                            }
                            for (int num11 = 0; num11 < 200; num11++)
                            {
                                if (((Entity)Main.npc[num11]).active)
                                {
                                    Main.npc[num11].AddBuff(ModContent.BuffType<TimeFrozen>(), duration, true);
                                }
                            }
                            for (int num12 = 0; num12 < 1000; num12++)
                            {
                                if (((Entity)Main.projectile[num12]).active && !Main.projectile[num12].GetGlobalProjectile<FargoSoulsGlobalProjectile>().TimeFreezeImmune)
                                {
                                    Main.projectile[num12].GetGlobalProjectile<FargoSoulsGlobalProjectile>().TimeFrozen = duration;
                                }
                            }
                            if (NewAI[1] < 130f && (NewAI[2] += 1f) > 12f)
                            {
                                NewAI[2] = 0f;
                                bool altAttack = NPC.localAI[2] != 0f;
                                int baseDistance = 300;
                                float offset3 = (altAttack ? 250f : 150f);
                                float speed3 = (altAttack ? 4f : 2.5f);
                                int damage2 = YharimEXUtil.ScaledProjectileDamage(NPC.damage);
                                if (NewAI[1] < 85f || !altAttack)
                                {
                                    if (altAttack && NewAI[3] % 2f == 0f)
                                    {
                                        float radius = (float)baseDistance + NewAI[3] * offset3;
                                        int circumference = (int)((float)Math.PI * 2f * radius);
                                        NPC.localAI[0] = MathHelper.WrapAngle(NPC.localAI[0] + (float)Math.PI + Utils.NextFloat(Main.rand, (float)Math.PI / 2f));
                                        for (int num13 = 0; num13 < circumference; num13 += 120)
                                        {
                                            float angle2 = (float)num13 / radius;
                                            if (!((double)angle2 > Math.PI * 2.0 - (double)MathHelper.WrapAngle(MathHelper.ToRadians(60f))))
                                            {
                                                float spawnOffset = radius;
                                                Vector2 center4 = ((Entity)Player).Center;
                                                Vector2 unitX2 = Vector2.UnitX;
                                                double num14 = angle2 + NPC.localAI[0];
                                                val = default(Vector2);
                                                Vector2 spawnPos2 = center4 + spawnOffset * Utils.RotatedBy(unitX2, num14, val);
                                                Vector2 vel2 = speed3 * ((Entity)Player).DirectionFrom(spawnPos2);
                                                float ai3 = ((Entity)Player).Distance(spawnPos2) / speed3 + 30f;
                                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                                {
                                                    ((CosmosInvaderTime)(object)Projectile.NewProjectileDirect(((Entity)NPC).GetSource_FromThis((string)null), spawnPos2, vel2, ModContent.ProjectileType<CosmosInvaderTime>(), damage2, 0f, Main.myPlayer, ai3, Utils.ToRotation(vel2)).ModProjectile).SpeedUP = true;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        int max2 = (altAttack ? 12 : (8 + (int)NewAI[3] * ((NPC.localAI[2] == 0f) ? 2 : 4)));
                                        float rotationOffset = Utils.NextFloat(Main.rand, (float)Math.PI * 2f);
                                        for (int num15 = 0; num15 < max2; num15++)
                                        {
                                            float ai4 = baseDistance;
                                            float distance = ai4 + NewAI[3] * offset3;
                                            Vector2 center5 = ((Entity)Player).Center;
                                            Vector2 unitX3 = Vector2.UnitX;
                                            double num16 = Math.PI * 2.0 / (double)max2 * (double)num15 + (double)rotationOffset;
                                            val = default(Vector2);
                                            Vector2 spawnPos3 = center5 + distance * Utils.RotatedBy(unitX3, num16, val);
                                            Vector2 vel3 = speed3 * ((Entity)Player).DirectionFrom(spawnPos3);
                                            ai4 = distance / speed3 + 30f;
                                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                            {
                                                ((CosmosInvaderTime)(object)Projectile.NewProjectileDirect(((Entity)NPC).GetSource_FromThis((string)null), spawnPos3, vel3, ModContent.ProjectileType<CosmosInvaderTime>(), damage2, 0f, Main.myPlayer, ai4, Utils.ToRotation(vel3)).ModProjectile).SpeedUP = true;
                                            }
                                        }
                                    }
                                }
                                NewAI[3] += 1f;
                            }
                        }
                        if (!((NewAI[1] += 1f) > 480f))
                        {
                            break;
                        }
                        Projectile[] projectile = Main.projectile;
                        foreach (Projectile proj in projectile)
                        {
                            if (((Entity)proj).active && proj.type == ModContent.ProjectileType<CosmosInvaderTime>())
                            {
                                proj.Kill();
                            }
                        }
                        NPC.TargetClosest(true);
                        ClearNewAI();
                        NPC.localAI[0] = 0f;
                        NPC.localAI[1] = 0f;
                        NPC.localAI[2] = 0f;
                        NPC.localAI[3] = 0f;
                        ChooseNextAttack(13, 21, 24, 44, 45);
                        break;
                    }
                case 1919810:
                    if (Phase2Check())
                    {
                        return;
                    }
                    if (NewAI[1] < 110f)
                    {
                        FancyFireballs3((int)NewAI[1]);
                    }
                    if ((NewAI[1] += 1f) == 110f)
                    {
                        Projectile[] projectile = Main.projectile;
                        foreach (Projectile p2 in projectile)
                        {
                            if (p2.type == ModContent.ProjectileType<MutantEyeHoming>() || p2.type == ModContent.ProjectileType<MutantSphereRing>() || p2.type == ModContent.ProjectileType<MutantTrueEyeDeathray>() || p2.type == ModContent.ProjectileType<MutantTrueEyeL>() || p2.type == ModContent.ProjectileType<MutantTrueEyeR>() || p2.type == ModContent.ProjectileType<MutantTrueEyeS>())
                            {
                                ((Entity)p2).active = false;
                            }
                        }
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int l = 0; l < 10; l++)
                            {
                                for (int m = 0; m < 3; m++)
                                {
                                    Vector2 center = ((Entity)Player).Center;
                                    float num2 = Utils.NextFloat(Main.rand, 500f, 700f);
                                    Vector2 unitX = Vector2.UnitX;
                                    double num3 = Main.rand.NextDouble() * 2.0 * Math.PI;
                                    val = default(Vector2);
                                    Vector2 spawnPos = center + num2 * Utils.RotatedBy(unitX, num3, val);
                                    Vector2 velocity = ((Entity)NPC).velocity;
                                    double num4 = Main.rand.NextDouble() * Math.PI * 2.0;
                                    val = default(Vector2);
                                    Vector2 vel = Utils.RotatedBy(velocity, num4, val);
                                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos, vel, ModContent.ProjectileType<ShadowClone>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)NPC.target, (float)(60 + 30 * l));
                                }
                            }
                        }
                    }
                    if (NewAI[1] == 455f)
                    {
                        ClearNewAI();
                        P1NextAttackOrMasoOptions(HistoryAttack1);
                    }
                    Movement(((Entity)Player).Center + Utils.SafeNormalize(((Entity)NPC).Center - ((Entity)Player).Center, Vector2.Zero) * 666f, 1f);
                    break;
                case 1919811:
                    {
                        if (Phase2Check())
                        {
                            return;
                        }
                        if (NPC.localAI[0] < 100f)
                        {
                            FancyFireballs7((int)NPC.localAI[0]);
                        }
                        if (NPC.localAI[0] == 100f)
                        {
                            Projectile[] projectile = Main.projectile;
                            foreach (Projectile p7 in projectile)
                            {
                                if (p7.type == ModContent.ProjectileType<MutantEyeHoming>() || p7.type == ModContent.ProjectileType<MutantSphereRing>() || p7.type == ModContent.ProjectileType<MutantTrueEyeDeathray>() || p7.type == ModContent.ProjectileType<MutantTrueEyeL>() || p7.type == ModContent.ProjectileType<MutantTrueEyeR>() || p7.type == ModContent.ProjectileType<MutantTrueEyeS>())
                                {
                                    ((Entity)p7).active = false;
                                }
                            }
                        }
                        Vector2 vel6 = ((Entity)Player).Center - ((Entity)NPC).Center;
                        NPC.rotation = Utils.ToRotation(vel6);
                        if (vel6.X > 0f)
                        {
                            vel6.X -= 550f;
                            ((Entity)NPC).direction = (NPC.spriteDirection = 1);
                        }
                        else
                        {
                            vel6.X += 550f;
                            ((Entity)NPC).direction = (NPC.spriteDirection = -1);
                        }
                        vel6.Y -= 250f;
                        ((Vector2)(vel6)).Normalize();
                        vel6 *= 16f;
                        if (((Entity)NPC).velocity.X < vel6.X)
                        {
                            ((Entity)NPC).velocity.X += 0.25f;
                            if (((Entity)NPC).velocity.X < 0f && vel6.X > 0f)
                            {
                                ((Entity)NPC).velocity.X += 0.25f;
                            }
                        }
                        else if (((Entity)NPC).velocity.X > vel6.X)
                        {
                            ((Entity)NPC).velocity.X -= 0.25f;
                            if (((Entity)NPC).velocity.X > 0f && vel6.X < 0f)
                            {
                                ((Entity)NPC).velocity.X -= 0.25f;
                            }
                        }
                        if (((Entity)NPC).velocity.Y < vel6.Y)
                        {
                            ((Entity)NPC).velocity.Y += 0.25f;
                            if (((Entity)NPC).velocity.Y < 0f && vel6.Y > 0f)
                            {
                                ((Entity)NPC).velocity.Y += 0.25f;
                            }
                        }
                        else if (((Entity)NPC).velocity.Y > vel6.Y)
                        {
                            ((Entity)NPC).velocity.Y -= 0.25f;
                            if (((Entity)NPC).velocity.Y > 0f && vel6.Y < 0f)
                            {
                                ((Entity)NPC).velocity.Y -= 0.25f;
                            }
                        }
                        if ((NPC.localAI[0] += 1f) > 100f)
                        {
                            NPC.localAI[0] = 47f;
                            if (Main.netMode != NetmodeID.MultiplayerClient && NewAI[1] < 90f)
                            {
                                SoundEngine.PlaySound(SoundID.Item34, (Vector2?)((Entity)NPC).Center);
                                Vector2 spawn = new Vector2(40f, 50f);
                                if (((Entity)NPC).direction < 0)
                                {
                                    spawn.X *= -1f;
                                    Vector2 val28 = spawn;
                                    val = default(Vector2);
                                    spawn = Utils.RotatedBy(val28, Math.PI, val);
                                }
                                Vector2 val29 = spawn;
                                double num29 = NPC.rotation;
                                val = default(Vector2);
                                spawn = Utils.RotatedBy(val29, num29, val);
                                spawn += ((Entity)NPC).Center;
                                Vector2 val30 = ((Entity)NPC).DirectionTo(((Entity)Player).Center);
                                double num30 = (Main.rand.NextDouble() - 0.5) * Math.PI / 10.0;
                                val = default(Vector2);
                                Vector2 projVel = Utils.RotatedBy(val30, num30, val);
                                ((Vector2)(projVel)).Normalize();
                                projVel *= Utils.NextFloat(Main.rand, 8f, 12f);
                                int type = 467;
                                if (Utils.NextBool(Main.rand))
                                {
                                    type = ModContent.ProjectileType<WillFireball>();
                                    projVel *= 2.5f;
                                }
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawn, projVel, type, NPC.defDamage / 4, 0f, Main.myPlayer, 0f, 0f);
                            }
                        }
                        if ((NPC.localAI[1] -= 1f) < -75f)
                        {
                            NPC.localAI[1] = -50f;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), new Vector2(((Entity)Player).Center.X, Math.Max(600f, ((Entity)Player).Center.Y - 2000f)), Vector2.UnitY, ModContent.ProjectileType<WillDeathraySmall>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f), 0f, Main.myPlayer, ((Entity)Player).Center.X, (float)((Entity)NPC).whoAmI);
                            }
                        }
                        if ((NewAI[1] += 1f) == 1f)
                        {
                            SoundEngine.PlaySound(SoundID.ForceRoarPitched, (Vector2?)((Entity)NPC).Center);
                        }
                        else if (NewAI[1] > 200f)
                        {
                            NewAI[1] = 0f;
                            NPC.localAI[0] = 0f;
                            NPC.netUpdate = true;
                            ClearNewAI();
                            P1NextAttackOrMasoOptions(HistoryAttack1);
                        }
                        break;
                    }
                case 1919812:
                    if (Phase2Check())
                    {
                        return;
                    }
                    if (NewAI[1] == 70f)
                    {
                        Projectile[] projectile = Main.projectile;
                        foreach (Projectile p4 in projectile)
                        {
                            if (p4.type == ModContent.ProjectileType<MutantSphereRing>() || p4.type == ModContent.ProjectileType<MutantTrueEyeDeathray>() || p4.type == ModContent.ProjectileType<MutantTrueEyeL>() || p4.type == ModContent.ProjectileType<MutantTrueEyeR>() || p4.type == ModContent.ProjectileType<MutantTrueEyeS>())
                            {
                                ((Entity)p4).active = false;
                            }
                        }
                        SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                        NPC.localAI[0] = ((Entity)Player).Center.X;
                        NPC.localAI[1] = ((Entity)Player).Center.Y;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, (((Entity)Player).Center - ((Entity)NPC).Center) / 120f, ModContent.ProjectileType<TimberSquirrel>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, NewAI[3] + 10f, (float)((Entity)NPC).whoAmI);
                        }
                    }
                    if (NewAI[1] < 100f)
                    {
                        FancyFireballs5((int)NewAI[1]);
                    }
                    if (NewAI[1] < 100f)
                    {
                        Vector2 targetPos = ((Entity)Player).Center;
                        targetPos.X += ((((Entity)NPC).Center.X < ((Entity)Player).Center.X) ? (-200) : 200);
                        targetPos.Y -= 200f;
                        if (((Entity)NPC).Distance(targetPos) > 50f)
                        {
                            Movement(targetPos, 0.4f);
                        }
                    }
                    else
                    {
                        NPC nPC8 = NPC;
                        ((Entity)nPC8).velocity = ((Entity)nPC8).velocity * 0.9f;
                    }
                    if ((NewAI[1] += 1f) < 160f)
                    {
                        if (NewAI[3] != 0f)
                        {
                            break;
                        }
                        if (NewAI[1] == 90f)
                        {
                            ((Entity)NPC).velocity = Vector2.Zero;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, -Vector2.UnitY, ModContent.ProjectileType<GlowLine>(), 0, 0f, Main.myPlayer, 19f, 0f);
                            }
                        }
                        if (!(NewAI[1] > 90f) || NewAI[1] % 3f != 0f)
                        {
                            break;
                        }
                        float current = NewAI[1] - 90f;
                        current /= 3f;
                        float offset4 = 192f * current;
                        Vector2 spawnPos4 = default(Vector2);
                        for (int num20 = -1; num20 <= 1; num20 += 2)
                        {
                            spawnPos4 = new Vector2(NPC.Center.X + offset4 * num20, Player.Center.Y + 1500f);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos4, -Vector2.UnitY, ModContent.ProjectileType<GlowLine>(), 0, 0f, Main.myPlayer, 19f, 0f);
                            }
                        }
                    }
                    else if (NewAI[1] < 310f)
                    {
                        if (NewAI[3] != 0f)
                        {
                            break;
                        }
                        if (NewAI[1] % 3f == 0f)
                        {
                            SoundEngine.PlaySound(SoundID.Item157, (Vector2?)((Entity)NPC).Center);
                        }
                        Vector2 spawnPos5 = default(Vector2);
                        for (int num21 = 0; num21 < 3; num21++)
                        {
                            spawnPos5 = new Vector2(NPC.localAI[0], NPC.localAI[1]);
                            spawnPos5.X += (Utils.NextBool(Main.rand, 2) ? Main.rand.Next(-1600, -600) : Main.rand.Next(600, 1600));
                            spawnPos5.Y -= Utils.NextFloat(Main.rand, 600f, 800f);
                            Vector2 speed4 = Utils.NextFloat(Main.rand, 7.5f, 12.5f) * Vector2.UnitY;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos5, speed4, ModContent.ProjectileType<TimberLaser>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 0f);
                            }
                        }
                    }
                    else if (!Main.projectile.Any((Projectile val32) => ((Entity)val32).active && val32.type == ModContent.ProjectileType<TimberSquirrel>() && (float)((Entity)NPC).whoAmI == val32.ai[1]))
                    {
                        NPC.TargetClosest(true);
                        P1NextAttackOrMasoOptions(HistoryAttack1);
                        NewAI[1] = 0f;
                        NewAI[2] = 0f;
                        NewAI[3] = 0f;
                        ClearNewAI();
                        NPC.localAI[0] = (NPC.localAI[1] = 0f);
                        NPC.netUpdate = true;
                    }
                    break;
                case 1919813:
                    if (Phase2Check())
                    {
                        return;
                    }
                    if (NewAI[3] == 100f)
                    {
                        Projectile[] projectile = Main.projectile;
                        foreach (Projectile p6 in projectile)
                        {
                            if (p6.type == ModContent.ProjectileType<MutantEyeHoming>() || p6.type == ModContent.ProjectileType<MutantSphereRing>() || p6.type == ModContent.ProjectileType<MutantTrueEyeDeathray>() || p6.type == ModContent.ProjectileType<MutantTrueEyeL>() || p6.type == ModContent.ProjectileType<MutantTrueEyeR>() || p6.type == ModContent.ProjectileType<MutantTrueEyeS>())
                            {
                                ((Entity)p6).active = false;
                            }
                        }
                    }
                    if (NewAI[3] < 100f)
                    {
                        FancyFireballs6((int)NewAI[3]);
                    }
                    else
                    {
                        NPC nPC21 = NPC;
                        ((Entity)nPC21).velocity = ((Entity)nPC21).velocity * 0.9f;
                    }
                    NewAI[3] += 1f;
                    if ((NewAI[2] += 1f) > 100f)
                    {
                        NewAI[2] = 60f;
                        SoundEngine.PlaySound(SoundID.Item92, (Vector2?)((Entity)NPC).Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i3 = 0; i3 < 15; i3++)
                            {
                                float num26 = Utils.NextFloat(Main.rand, 4f, 8f);
                                Vector2 unitX4 = Vector2.UnitX;
                                double num27 = Main.rand.NextDouble() * 2.0 * Math.PI;
                                val = default(Vector2);
                                Vector2 velocity10 = num26 * Utils.RotatedBy(unitX4, num27, val);
                                float ai6 = num26 / Utils.NextFloat(Main.rand, 60f, 120f);
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, velocity10, ModContent.ProjectileType<SpiritSword>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, ai6);
                            }
                            for (int i4 = 0; i4 < 12; i4++)
                            {
                                Vector2 val27 = ((Entity)NPC).DirectionTo(((Entity)Player).Center);
                                double num28 = Math.PI / 6.0 * (double)i4;
                                val = default(Vector2);
                                Vector2 vel5 = Utils.RotatedBy(val27, num28, val);
                                float ai7 = 1.04f;
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel5, ModContent.ProjectileType<SpiritHand>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, ai7, 0f);
                            }
                        }
                    }
                    if (NewAI[3] > 470f)
                    {
                        ClearNewAI();
                        P1NextAttackOrMasoOptions(HistoryAttack1);
                    }
                    break;
                case 0:
                    SpearTossDirectP1AndChecks();
                    break;
                case 1:
                    OkuuSpheresP1();
                    break;
                case 2:
                    PrepareTrueEyeDiveP1();
                    break;
                case 3:
                case 32:
                    TrueEyeDive();
                    break;
                case 4:
                    PrepareSpearDashDirectP1();
                    break;
                case 5:
                    SpearDashDirectP1();
                    break;
                case 6:
                    WhileDashingP1();
                    break;
                case 7:
                    ApproachForNextAttackP1();
                    break;
                case 8:
                    VoidRaysP1();
                    break;
                case 9:
                    BoundaryBulletHellAndSwordP1();
                    break;
                case 114518:
                    ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(NewAI[2] - ((Entity)NPC).Center.X));
                    if (NewAI[1] == 0f && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float horizontalModifier = Math.Sign(NPC.ai[2] - ((Entity)NPC).Center.X);
                        float verticalModifier2 = Math.Sign(NPC.ai[3] - ((Entity)NPC).Center.Y);
                        float ai5 = horizontalModifier * (float)Math.PI / 60f * verticalModifier2;
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.UnitX * (0f - horizontalModifier), ModContent.ProjectileType<AbomSword>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.5f), 0f, Main.myPlayer, ai5, (float)((Entity)NPC).whoAmI);
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, -Vector2.UnitX * (0f - horizontalModifier), ModContent.ProjectileType<AbomSword>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.5f), 0f, Main.myPlayer, ai5, (float)((Entity)NPC).whoAmI);
                    }
                    if ((NewAI[1] += 1f) > 60f)
                    {
                        ChooseNextAttack(11, 13, 19, 33, 24, 41);
                        ((Entity)NPC).velocity.X = 0f;
                        ((Entity)NPC).velocity.Y = 24 * Math.Sign(NewAI[3] - ((Entity)NPC).Center.Y);
                        ClearNewAI();
                    }
                    break;
                case 114514:
                    {
                        NPC nPC22 = NPC;
                        ((Entity)nPC22).velocity = ((Entity)nPC22).velocity * 0.9f;
                        if (NewAI[1] < 60f)
                        {
                            FancyFireballs2((int)NewAI[1]);
                        }
                        if (NewAI[1] == 0f && NewAI[2] != 2f && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float ai8 = ((NewAI[2] != 1f) ? 1 : (-1));
                            ai8 *= MathHelper.ToRadians(270f) / 120f * -1f * 60f;
                            int p8 = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<GlowLine>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 3f, ai8);
                            if (p8 != 1000)
                            {
                                Main.projectile[p8].localAI[1] = ((Entity)NPC).whoAmI;
                                if (Main.netMode == NetmodeID.Server)
                                {
                                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, (NetworkText)null, p8, 0f, 0f, 0f, 0, 0, 0);
                                }
                            }
                            int p9 = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<GlowLine>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 3f, ai8 + 3.1416f);
                            if (p9 != 1000)
                            {
                                Main.projectile[p9].localAI[1] = ((Entity)NPC).whoAmI;
                                if (Main.netMode == NetmodeID.Server)
                                {
                                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, (NetworkText)null, p9, 0f, 0f, 0f, 0, 0, 0);
                                }
                            }
                        }
                        if ((NewAI[1] += 1f) > 90f)
                        {
                            ClearNewAI();
                            NPC.netUpdate = true;
                            NPC.ai[0] += 1f;
                            NewAI[1] = 0f;
                            ((Entity)NPC).velocity = ((Entity)NPC).DirectionTo(((Entity)Player).Center) * 3f;
                        }
                        else if (NewAI[1] == 60f && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.netUpdate = true;
                            ((Entity)NPC).velocity = Vector2.Zero;
                            SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                            float ai9 = ((NewAI[2] != 1f) ? 1 : (-1));
                            ai9 *= MathHelper.ToRadians(270f) / 120f;
                            Vector2 val31 = ((Entity)NPC).DirectionTo(((Entity)Player).Center);
                            double num31 = (0f - ai9) * 60f;
                            val = default(Vector2);
                            Vector2 vel7 = Utils.RotatedBy(val31, num31, val);
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel7, ModContent.ProjectileType<AbomSword>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.5f), 0f, Main.myPlayer, ai9, (float)((Entity)NPC).whoAmI);
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, -vel7, ModContent.ProjectileType<AbomSword>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.5f), 0f, Main.myPlayer, ai9, (float)((Entity)NPC).whoAmI);
                        }
                        break;
                    }
                case 114515:
                    ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(((Entity)NPC).velocity.X));
                    if ((NewAI[1] += 1f) > 120f)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0] = 114516f;
                        ClearNewAI();
                    }
                    break;
                case 114516:
                    if (AliveCheck(Player))
                    {
                        Vector2 targetPos5 = ((Entity)Player).Center + ((Entity)Player).DirectionTo(((Entity)NPC).Center) * 500f;
                        if (((Entity)NPC).Distance(targetPos5) > 50f)
                        {
                            Movement(targetPos5, 0.7f);
                        }
                        if ((NewAI[1] += 1f) > 60f)
                        {
                            NPC.netUpdate = true;
                            ClearNewAI();
                            NPC.localAI[0] = 0f;
                            NPC.localAI[1] = 0f;
                            NPC.localAI[2] = 0f;
                            NPC.localAI[3] = 0f;
                            ChooseNextAttack(20, 21, 24, 25, 29, 44, 45);
                        }
                    }
                    break;
                case 114517:
                    {
                        if (NewAI[1] < 90f && !AliveCheck(Player))
                        {
                            break;
                        }
                        if (NewAI[2] == 0f && NewAI[3] == 0f)
                        {
                            NPC.netUpdate = true;
                            NewAI[2] = ((Entity)Player).Center.X;
                            NewAI[3] = ((Entity)Player).Center.Y;
                            if (YharimEXUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<AbomRitual>()) != null)
                            {
                                NewAI[2] = ((Entity)Main.projectile[ritualProj]).Center.X;
                                NewAI[3] = ((Entity)Main.projectile[ritualProj]).Center.Y;
                            }
                            Vector2 offset = default(Vector2);
                            offset.X = Math.Sign(((Entity)Player).Center.X - NewAI[2]);
                            offset.Y = Math.Sign(((Entity)Player).Center.Y - NewAI[3]);
                            NPC.localAI[2] = Utils.ToRotation(offset);
                        }
                        Vector2 actualTargetPositionOffset = (float)Math.Sqrt(2880000.0) * Utils.ToRotationVector2(NPC.localAI[2]);
                        actualTargetPositionOffset.Y -= 450 * Math.Sign(actualTargetPositionOffset.Y);
                        Vector2 targetPos = new Vector2(NewAI[2], NewAI[3]) + actualTargetPositionOffset;
                        Movement(targetPos, 1f);
                        if (NewAI[1] == 0f && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float num = Math.Sign(NPC.ai[2] - targetPos.X);
                            float verticalModifier = Math.Sign(NPC.ai[3] - targetPos.Y);
                            float startRotation = ((num > 0f) ? (MathHelper.ToRadians(0.1f) * (0f - verticalModifier)) : ((float)Math.PI - MathHelper.ToRadians(0.1f) * (0f - verticalModifier)));
                            float ai2 = ((num > 0f) ? ((float)Math.PI) : 0f);
                            Projectile.NewProjectileDirect(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.ToRotationVector2(startRotation), ModContent.ProjectileType<GlowLine>(), 1, 0f, 0, 4f, ai2).localAI[1] = ((Entity)NPC).whoAmI;
                            Projectile.NewProjectileDirect(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.ToRotationVector2(startRotation), ModContent.ProjectileType<GlowLine>(), 1, 0f, 0, 4f, ai2 + 3.1416f).localAI[1] = ((Entity)NPC).whoAmI;
                        }
                        if (NewAI[1] > 90f)
                        {
                            FancyFireballs2((int)NewAI[1] - 90);
                        }
                        if ((NewAI[1] += 1f) > 150f)
                        {
                            NPC.netUpdate = true;
                            ((Entity)NPC).velocity = Vector2.Zero;
                            NPC.ai[0] += 1f;
                            NewAI[1] = 0f;
                        }
                        break;
                    }
                case 10:
                    Phase2Transition();
                    break;
                case 11:
                case 16:
                    ApproachForNextAttackP2();
                    break;
                case 12:
                    VoidRaysP2();
                    break;
                case 13:
                    PrepareSpearDashPredictiveP2();
                    break;
                case 14:
                    SpearDashPredictiveP2();
                    break;
                case 15:
                    WhileDashingP2();
                    break;
                case 17:
                    BoundaryBulletHellP2();
                    break;
                case 18:
                    NPC.ai[0] += 1f;
                    break;
                case 19:
                    PillarDunk();
                    break;
                case 20:
                    EOCStarSickles();
                    break;
                case 21:
                    PrepareSpearDashDirectP2();
                    break;
                case 22:
                    SpearDashDirectP2();
                    break;
                case 23:
                    if (NPC.ai[1] % 3f == 0f)
                    {
                        NPC.ai[1] += 1f;
                    }
                    goto case 15;
                case 24:
                    SpawnDestroyersForPredictiveThrow();
                    break;
                case 25:
                    SpearTossPredictiveP2();
                    break;
                case 26:
                    PrepareMechRayFan();
                    break;
                case 27:
                    MechRayFan();
                    break;
                case 28:
                    NPC.ai[0] += 1f;
                    break;
                case 29:
                    PrepareFishron1();
                    break;
                case 30:
                case 38:
                    SpawnFishrons();
                    break;
                case 31:
                    PrepareTrueEyeDiveP2();
                    break;
                case 33:
                    PrepareNuke();
                    break;
                case 34:
                    Nuke();
                    break;
                case 35:
                    PrepareSlimeRain();
                    break;
                case 36:
                    SlimeRain();
                    break;
                case 37:
                    PrepareFishron2();
                    break;
                case 39:
                    PrepareOkuuSpheresP2();
                    break;
                case 40:
                    OkuuSpheresP2();
                    break;
                case 41:
                    SpearTossDirectP2();
                    break;
                case 42:
                    PrepareTwinRangsAndCrystals();
                    break;
                case 43:
                    TwinRangsAndCrystals();
                    break;
                case 44:
                    EmpressSwordWave();
                    break;
                case 45:
                    PrepareMutantSword();
                    break;
                case 46:
                    MutantSword();
                    break;
                case 48:
                    P2NextAttackPause();
                    break;
                case 415411:
                    {
                        if (Phase2Check())
                        {
                            return;
                        }
                        if (NPC.localAI[3] == 100f)
                        {
                            Projectile[] projectile = Main.projectile;
                            foreach (Projectile p in projectile)
                            {
                                if (p.type == ModContent.ProjectileType<MutantEyeHoming>() || p.type == ModContent.ProjectileType<MutantSphereRing>() || p.type == ModContent.ProjectileType<MutantTrueEyeDeathray>() || p.type == ModContent.ProjectileType<MutantTrueEyeL>() || p.type == ModContent.ProjectileType<MutantTrueEyeR>() || p.type == ModContent.ProjectileType<MutantTrueEyeS>())
                                {
                                    ((Entity)p).active = false;
                                }
                            }
                        }
                        if (NPC.localAI[3] < 100f)
                        {
                            FancyFireballs((int)NPC.localAI[3]);
                        }
                        NPC nPC = NPC;
                        ((Entity)nPC).velocity = ((Entity)nPC).velocity * 0.9f;
                        if (NPC.localAI[3] < 100f)
                        {
                            NPC.localAI[3] += 1f;
                        }
                        float length = MathHelper.Lerp(2500f, 1000f, NPC.localAI[3] / 100f);
                        for (int j = 0; j < 50; j++)
                        {
                            Dust.NewDustDirect(((Entity)NPC).Center + Utils.NextVector2CircularEdge(Main.rand, length, length), 0, 0, DustID.PinkTorch, 0f, 0f, 0, default(Color), 1.5f).noGravity = true;
                        }
                        if (((Entity)NPC).Distance(((Entity)Player).Center) > length)
                        {
                            Player obj = Player;
                            ((Entity)obj).velocity = ((Entity)obj).velocity * 0f;
                            Player obj2 = Player;
                            ((Entity)obj2).Center = ((Entity)obj2).Center + Utils.SafeNormalize(((Entity)NPC).Center - ((Entity)Player).Center, Vector2.Zero) * 5f;
                        }
                        if (NewAI[3] == 0f)
                        {
                            NewAI[3] = ((!(((Entity)NPC).Center.X < ((Entity)Player).Center.X)) ? 1 : (-1));
                        }
                        if ((NewAI[2] += 1f) > (float)((NPC.localAI[2] == 1f) ? 40 : 60))
                        {
                            NewAI[2] = 0f;
                            SoundEngine.PlaySound(SoundID.Item92, (Vector2?)((Entity)NPC).Center);
                            if (NPC.localAI[0] > 0f)
                            {
                                NPC.localAI[0] = -1f;
                            }
                            else
                            {
                                NPC.localAI[0] = 1f;
                            }
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 projTarget = ((Entity)NPC).Center;
                                projTarget.X += 1200f * NewAI[3];
                                projTarget.Y += 1200f * (0f - NPC.localAI[0]);
                                int max = ((NPC.localAI[2] == 1f) ? 30 : 20);
                                int increment = ((NPC.localAI[2] == 1f) ? 180 : 250);
                                projTarget.Y += Utils.NextFloat(Main.rand, (float)increment);
                                for (int k = 0; k < max; k++)
                                {
                                    projTarget.Y += (float)increment * NPC.localAI[0];
                                    Vector2 speed = (projTarget - ((Entity)NPC).Center) / 40f;
                                    float ai0 = (float)((NPC.localAI[2] == 1f) ? 8 : 6) * (0f - NewAI[3]);
                                    float ai1 = 6f * (0f - NPC.localAI[0]);
                                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, speed, ModContent.ProjectileType<ChampionBeetle>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, ai0, ai1);
                                }
                            }
                        }
                        if ((NewAI[1] += 1f) > 620f)
                        {
                            NPC.localAI[0] = 0f;
                            NPC.localAI[3] = 0f;
                            NPC.TargetClosest(true);
                            P1NextAttackOrMasoOptions(HistoryAttack1);
                            NewAI[1] = 0f;
                            NewAI[2] = 0f;
                            NewAI[3] = 0f;
                            NPC.netUpdate = true;
                        }
                        break;
                    }
                case -1:
                    drainLifeInP3 = Phase3Transition();
                    break;
                case -2:
                    VoidRaysP3();
                    break;
                case -3:
                    OkuuSpheresP3();
                    break;
                case -4:
                    BoundaryBulletHellP3();
                    break;
                case -5:
                    FinalSpark();
                    break;
                case -6:
                    DyingDramaticPause();
                    break;
                case -7:
                    DyingAnimationAndHandling();
                    break;
                default:
                    NPC.ai[0] = 11f;
                    goto case 11;
            }
            if ((NPC.ai[0] < 0f || NPC.ai[0] > 10f || (NPC.ai[0] == 10f && NPC.ai[1] > 150f)))
            {
                Main.dayTime = false;
                Main.time = 16200.0;
                Main.raining = false;
                Main.rainTime = 0.0;
                Main.maxRaining = 0f;
                Main.bloodMoon = false;
            }
            if (NPC.ai[0] < 0f && NPC.life > 1 && drainLifeInP3)
            {
                int time = 4350;
                NPC nPC23 = NPC;
                nPC23.life -= NPC.lifeMax / time;
                if (NPC.life < 1)
                {
                    NPC.life = 1;
                }
            }
            if (Player.immune || Player.hurtCooldowns[0] != 0 || Player.hurtCooldowns[1] != 0)
            {
                playerInvulTriggered = true;
            }
            //if (FargoSoulsWorld.EternityMode && FargoSoulsWorld.downedAbom && !FargoSoulsWorld.downedMutant && Main.netMode != 1 && NPC.HasPlayerTarget && !droppedSummon)
            //{
            //    Item.NewItem(((Entity)NPC).GetSource_Loot((string)null), ((Entity)Player).Hitbox, ModContent.ItemType<MutantsCurse>(), 1, false, 0, false, false);
            //    droppedSummon = true;
            //}
            void FancyFireballs(int repeats)
            {
                float modifier5 = 0f;
                for (int num32 = 0; num32 < repeats; num32++)
                {
                    modifier5 = MathHelper.Lerp(modifier5, 1f, 0.08f);
                }
                float distance2 = 1400f * (1f - modifier5);
                float rotation = (float)Math.PI * 2f * modifier5;
                for (int num33 = 0; num33 < 4; num33++)
                {
                    int d2 = Dust.NewDust(((Entity)NPC).Center + distance2 * Utils.RotatedBy(Vector2.UnitX, (double)(rotation + (float)Math.PI / 2f * (float)num33), default(Vector2)), 0, 0, DustID.PinkTorch, ((Entity)NPC).velocity.X * 0.3f, ((Entity)NPC).velocity.Y * 0.3f, 0, Color.White, 1f);
                    Main.dust[d2].noGravity = true;
                    Main.dust[d2].scale = 6f - 4f * modifier5;
                }
            }
            void FancyFireballs2(int repeats)
            {
                float modifier5 = 0f;
                for (int num32 = 0; num32 < repeats; num32++)
                {
                    modifier5 = MathHelper.Lerp(modifier5, 1f, 0.08f);
                }
                float distance2 = 1400f * (1f - modifier5);
                float rotation = (float)Math.PI * 2f * modifier5;
                for (int num33 = 0; num33 < 4; num33++)
                {
                    int d2 = Dust.NewDust(((Entity)NPC).Center + distance2 * Utils.RotatedBy(Vector2.UnitX, (double)(rotation + (float)Math.PI / 2f * (float)num33), default(Vector2)), 0, 0, DustID.PurpleCrystalShard, ((Entity)NPC).velocity.X * 0.3f, ((Entity)NPC).velocity.Y * 0.3f, 0, Color.White, 1f);
                    Main.dust[d2].noGravity = true;
                    Main.dust[d2].scale = 6f - 4f * modifier5;
                }
            }
            void FancyFireballs3(int repeats)
            {
                float modifier5 = 0f;
                for (int num32 = 0; num32 < repeats; num32++)
                {
                    modifier5 = MathHelper.Lerp(modifier5, 1f, 0.08f);
                }
                float distance2 = 1400f * (1f - modifier5);
                float rotation = (float)Math.PI * 2f * modifier5;
                for (int num33 = 0; num33 < 9; num33++)
                {
                    int d2 = Dust.NewDust(((Entity)NPC).Center + distance2 * Utils.RotatedBy(Vector2.UnitX, (double)(rotation + (float)Math.PI * 2f / 9f * (float)num33), default(Vector2)), 0, 0, DustID.ShadowbeamStaff, ((Entity)NPC).velocity.X * 0.3f, ((Entity)NPC).velocity.Y * 0.3f, 0, Color.White, 1f);
                    Main.dust[d2].noGravity = true;
                    Main.dust[d2].scale = 7f - 4f * modifier5;
                }
            }
            void FancyFireballs5(int repeats)
            {
                float modifier5 = 0f;
                for (int num32 = 0; num32 < repeats; num32++)
                {
                    modifier5 = MathHelper.Lerp(modifier5, 1f, 0.08f);
                }
                float distance2 = 1400f * (1f - modifier5);
                float rotation = (float)Math.PI * 2f * modifier5;
                for (int num33 = 0; num33 < 6; num33++)
                {
                    int d2 = Dust.NewDust(((Entity)NPC).Center + distance2 * Utils.RotatedBy(Vector2.UnitX, (double)(rotation + (float)Math.PI / 3f * (float)num33), default(Vector2)), 0, 0, DustID.MartianHit, ((Entity)NPC).velocity.X * 0.3f, ((Entity)NPC).velocity.Y * 0.3f, 0, Color.White, 1f);
                    Main.dust[d2].noGravity = true;
                    Main.dust[d2].scale = 6f - 4f * modifier5;
                }
            }
            void FancyFireballs6(int repeats)
            {
                Movement(((Entity)Player).Center - Utils.SafeNormalize(((Entity)NPC).DirectionTo(((Entity)Player).Center), Vector2.Zero) * 560f, 1f);
                float modifier5 = 0f;
                for (int num32 = 0; num32 < repeats; num32++)
                {
                    modifier5 = MathHelper.Lerp(modifier5, 1f, 0.08f);
                }
                float distance2 = 1600f * (1f - modifier5);
                float rotation = (float)Math.PI * 2f * modifier5;
                for (int num33 = 0; num33 < 10; num33++)
                {
                    int d2 = Dust.NewDust(((Entity)NPC).Center + distance2 * Utils.RotatedBy(Vector2.UnitX, (double)(rotation + (float)Math.PI / 5f * (float)num33), default(Vector2)), 0, 0, DustID.WhiteTorch, ((Entity)NPC).velocity.X * 0.3f, ((Entity)NPC).velocity.Y * 0.3f, 0, Color.White, 1f);
                    Main.dust[d2].noGravity = true;
                    Main.dust[d2].scale = 7f - 4f * modifier5;
                }
            }
            void FancyFireballs7(int repeats)
            {
                float modifier5 = 0f;
                for (int num32 = 0; num32 < repeats; num32++)
                {
                    modifier5 = MathHelper.Lerp(modifier5, 1f, 0.08f);
                }
                float distance2 = 1400f * (1f - modifier5);
                float rotation = (float)Math.PI * 2f * modifier5;
                for (int num33 = 0; num33 < 7; num33++)
                {
                    int d2 = Dust.NewDust(((Entity)NPC).Center + distance2 * Utils.RotatedBy(Vector2.UnitX, (double)(rotation + 0.8975979f * (float)num33), default(Vector2)), 0, 0, DustID.YellowStarDust, ((Entity)NPC).velocity.X * 0.3f, ((Entity)NPC).velocity.Y * 0.3f, 0, Color.White, 1f);
                    Main.dust[d2].noGravity = true;
                    Main.dust[d2].scale = 6f - 4f * modifier5;
                }
            }
            void SpawnAxeHitbox(Vector2 val32)
            {
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), val32, Vector2.Zero, ModContent.ProjectileType<DeviAxe>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 2f), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, ((Entity)NPC).Distance(val32));
            }
        }

        private void ManageAurasAndPreSpawn()
        {
            if (!spawned)
            {
                spawned = true;
                int prevLifeMax = NPC.lifeMax;
                if (FargoSoulsWorld.AngryMutant)
                {
                    NPC nPC = NPC;
                    nPC.lifeMax *= 100;
                    if (NPC.lifeMax < prevLifeMax)
                    {
                        NPC.lifeMax = int.MaxValue;
                    }
                }
                NPC.life = NPC.lifeMax;
            }
            //if (FargoSoulsWorld.MasochistModeReal && ((Entity)Main.LocalPlayer).active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost)
            //{
            //    Main.LocalPlayer.AddBuff(ModContent.BuffType<MutantPresence>(), 2, true, false);
            //}
            if (NPC.localAI[3] == 0f)
            {
                NPC.TargetClosest(true);
                if (NPC.timeLeft < 30)
                {
                    NPC.timeLeft = 30;
                }
                if (((Entity)NPC).Distance(((Entity)Main.player[NPC.target]).Center) < 1500f)
                {
                    NPC.localAI[3] = 1f;
                    SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient && FargoSoulsWorld.AngryMutant && FargoSoulsWorld.MasochistModeReal)
                    {
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<BossRush>(), 0, 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 0f);
                    }
                }
            }
            else if (NPC.localAI[3] == 1f)
            {
                ShouldDrawAura = true;
                EModeGlobalNPC.Aura(NPC, 2000f, true, -1, default(Color), ModContent.BuffType<GodEater>(), ModContent.BuffType<MutantFang>());
            }
            else
            {
                if (!((Entity)Main.LocalPlayer).active || !(((Entity)NPC).Distance(((Entity)Main.LocalPlayer).Center) < 3000f))
                {
                    return;
                }
                if (Main.expertMode)
                {
                    //Main.LocalPlayer.AddBuff(ModContent.BuffType<MutantPresence>(), 2, true, false);
                }
                if (FargoSoulsWorld.EternityMode && NPC.ai[0] < 0f && NPC.ai[0] > -6f)
                {
                    Main.LocalPlayer.AddBuff(ModContent.BuffType<GoldenStasisCD>(), 2, true, false);
                    if (FargoSoulsWorld.MasochistModeReal)
                    {
                        Main.LocalPlayer.AddBuff(ModContent.BuffType<TimeStopCD>(), 2, true, false);
                    }
                }
            }
        }
        private void ManageNeededProjectiles()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (FargoSoulsWorld.EternityMode && NPC.ai[0] != -7f && (NPC.ai[0] < 0f || (NPC.ai[0] > 10f && NPC.ai[0] < 100f)) && YharimEXUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<MutantRitual>()) == null)
            {
                ritualProj = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantRitual>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, (float)((Entity)NPC).whoAmI);
            }
            if (YharimEXUtil.ProjectileExists(ringProj, ModContent.ProjectileType<MutantRitual5>()) == null)
            {
                ringProj = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantRitual5>(), 0, 0f, Main.myPlayer, 0f, (float)((Entity)NPC).whoAmI);
            }
            if (YharimEXUtil.ProjectileExists(spriteProj, ModContent.ProjectileType<global::FargowiltasSouls.Projectiles.MutantBoss.MutantBoss>()) != null)
            {
                return;
            }
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                int number = 0;
                for (int index = 999; index >= 0; index--)
                {
                    if (!((Entity)Main.projectile[index]).active)
                    {
                        number = index;
                        break;
                    }
                }
                if (number >= 0)
                {
                    Projectile obj = Main.projectile[number];
                    obj.SetDefaults(ModContent.ProjectileType<global::FargowiltasSouls.Projectiles.MutantBoss.MutantBoss>());
                    ((Entity)obj).Center = ((Entity)NPC).Center;
                    obj.owner = Main.myPlayer;
                    ((Entity)obj).velocity.X = 0f;
                    ((Entity)obj).velocity.Y = 0f;
                    obj.damage = 0;
                    obj.knockBack = 0f;
                    obj.identity = number;
                    obj.gfxOffY = 0f;
                    obj.stepSpeed = 1f;
                    obj.ai[1] = ((Entity)NPC).whoAmI;
                    spriteProj = number;
                }
            }
            else
            {
                spriteProj = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<global::FargowiltasSouls.Projectiles.MutantBoss.MutantBoss>(), 0, 0f, Main.myPlayer, 0f, (float)((Entity)NPC).whoAmI);
            }
        }
        private void ChooseNextAttack(params int[] args)
        {
            ShouldDoSword++;
            float buffer = ((NPC.ai[0] > 100f) ? HistoryAttack1 : NPC.ai[0]);
            NPC.ai[0] = 48f;
            NPC.ai[1] = 0f;
            NPC.ai[2] = buffer - 1f;
            NPC.ai[3] = 0f;
            NPC.localAI[0] = 0f;
            NPC.localAI[1] = 0f;
            NPC.localAI[2] = 0f;
            NPC.netUpdate = true;
            if (ShouldDoSword == 4)
            {
                HistoryAttack1 = buffer;
                Projectile[] projectile = Main.projectile;
                foreach (Projectile p in projectile)
                {
                    if (p.type == ModContent.ProjectileType<MutantDeathray2>() || p.type == ModContent.ProjectileType<MutantDeathraySmall>() || p.type == ModContent.ProjectileType<MutantDeathrayAim>() || p.type == ModContent.ProjectileType<MutantMark1>() || p.type == ModContent.ProjectileType<MutantDeathray3>() || p.type == ModContent.ProjectileType<MutantFragment>() || p.type == ModContent.ProjectileType<MutantTrueEyeDeathray>() || p.type == ModContent.ProjectileType<MutantTrueEyeL>() || p.type == ModContent.ProjectileType<MutantTrueEyeR>() || p.type == ModContent.ProjectileType<MutantTrueEyeS>())
                    {
                        ((Entity)p).active = false;
                    }
                }
                NPC.ai[0] = 114514f;
                return;
            }
            if (ShouldDoSword == 8)
            {
                HistoryAttack1 = buffer;
                Projectile[] projectile = Main.projectile;
                foreach (Projectile p2 in projectile)
                {
                    if (p2.type == ModContent.ProjectileType<MutantDeathray2>() || p2.type == ModContent.ProjectileType<MutantDeathraySmall>() || p2.type == ModContent.ProjectileType<MutantDeathrayAim>() || p2.type == ModContent.ProjectileType<MutantDeathray3>() || p2.type == ModContent.ProjectileType<MutantMark1>() || p2.type == ModContent.ProjectileType<MutantFragment>() || p2.type == ModContent.ProjectileType<MutantTrueEyeDeathray>() || p2.type == ModContent.ProjectileType<MutantTrueEyeL>() || p2.type == ModContent.ProjectileType<MutantTrueEyeR>() || p2.type == ModContent.ProjectileType<MutantTrueEyeS>())
                    {
                        ((Entity)p2).active = false;
                    }
                }
                ClearNewAI();
                NPC.ai[0] = 888f;
                return;
            }
            if (ShouldDoSword == 11)
            {
                HistoryAttack1 = buffer;
                ClearNewAI();
                NPC.ai[0] = 1919f;
                return;
            }
            if (ShouldDoSword == 14)
            {
                HistoryAttack1 = buffer;
                Projectile[] projectile = Main.projectile;
                foreach (Projectile p3 in projectile)
                {
                    if (p3.type == ModContent.ProjectileType<MutantDeathray2>() || p3.type == ModContent.ProjectileType<MutantDeathraySmall>() || p3.type == ModContent.ProjectileType<MutantDeathrayAim>() || p3.type == ModContent.ProjectileType<MutantMark1>() || p3.type == ModContent.ProjectileType<MutantDeathray3>() || p3.type == ModContent.ProjectileType<MutantFragment>() || p3.type == ModContent.ProjectileType<MutantTrueEyeDeathray>() || p3.type == ModContent.ProjectileType<MutantTrueEyeL>() || p3.type == ModContent.ProjectileType<MutantTrueEyeR>() || p3.type == ModContent.ProjectileType<MutantTrueEyeS>())
                    {
                        ((Entity)p3).active = false;
                    }
                }
                NPC.ai[0] = 132f;
                return;
            }
            if (FargoSoulsWorld.EternityMode)
            {
                bool useRandomizer = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Queue<float> recentAttacks = new Queue<float>(attackHistory);
                    if (useRandomizer)
                    {
                        NPC.ai[2] = Utils.Next<int>(Main.rand, args);
                    }
                    while (recentAttacks.Count > 0)
                    {
                        bool foundAttackToUse = false;
                        for (int j = 0; j < 5; j++)
                        {
                            if (!recentAttacks.Contains(NPC.ai[2]))
                            {
                                foundAttackToUse = true;
                                break;
                            }
                            NPC.ai[2] = Utils.Next<int>(Main.rand, args);
                        }
                        if (foundAttackToUse)
                        {
                            break;
                        }
                        recentAttacks.Dequeue();
                    }
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int maxMemory = (FargoSoulsWorld.MasochistModeReal ? 10 : 16);
                if ((double)attackCount++ > (double)maxMemory * 1.25)
                {
                    attackCount = 0;
                    maxMemory /= 4;
                }
                attackHistory.Enqueue(NPC.ai[2]);
                while (attackHistory.Count > maxMemory)
                {
                    attackHistory.Dequeue();
                }
            }
            endTimeVariance = (FargoSoulsWorld.MasochistModeReal ? Utils.NextFloat(Main.rand) : 0f);
        }

        private void P1NextAttackOrMasoOptions(float sourceAI)
        {
            ShouldChampion++;
            if (ShouldChampion == 3)
            {
                HistoryAttack1 = NPC.ai[0];
                NPC.ai[0] = 1919810f;
                return;
            }
            if (ShouldChampion == 6)
            {
                HistoryAttack1 = NPC.ai[0];
                for (int i = 0; i < 4; i++)
                {
                    NPC.localAI[i] = 0f;
                }
                NPC.ai[0] = 1919811f;
                return;
            }
            if (ShouldChampion == 9)
            {
                HistoryAttack1 = NPC.ai[0];
                for (int j = 0; j < 4; j++)
                {
                    NPC.localAI[j] = 0f;
                }
                NPC.ai[0] = 1919812f;
                return;
            }
            if (ShouldChampion == 12)
            {
                HistoryAttack1 = NPC.ai[0];
                for (int k = 0; k < 4; k++)
                {
                    NPC.localAI[k] = 0f;
                }
                NPC.ai[0] = 1919813f;
                return;
            }
            if (ShouldChampion == 15)
            {
                HistoryAttack1 = NPC.ai[0];
                for (int l = 0; l < 4; l++)
                {
                    NPC.localAI[l] = 0f;
                }
                NPC.ai[0] = 415411f;
                ShouldChampion = 0;
                return;
            }
            if (FargoSoulsWorld.MasochistModeReal && Utils.NextBool(Main.rand, 3))
            {
                int[] options = new int[7] { 0, 1, 2, 4, 7, 9, 9 };
                NPC.ai[0] = Utils.Next<int>(Main.rand, options);
                if (NPC.ai[0] == sourceAI)
                {
                    NPC.ai[0] = ((sourceAI != 9f) ? 9 : 0);
                }
                bool badCombo = false;
                if (NPC.ai[0] == 9f && (sourceAI == 1f || sourceAI == 2f || sourceAI == 7f))
                {
                    badCombo = true;
                }
                if ((NPC.ai[0] == 0f || NPC.ai[0] == 7f) && sourceAI == 2f)
                {
                    badCombo = true;
                }
                if (badCombo)
                {
                    NPC.ai[0] = 4f;
                }
                else if (NPC.ai[0] == 9f && Utils.NextBool(Main.rand))
                {
                    NPC.localAI[2] = 1f;
                }
                else
                {
                    NPC.localAI[2] = 0f;
                }
            }
            else if (NPC.ai[0] == 9f && NPC.localAI[2] == 0f)
            {
                NPC.localAI[2] = 1f;
            }
            else
            {
                NPC.ai[0] += 1f;
                NPC.localAI[2] = 0f;
            }
            if (NPC.ai[0] >= 10f)
            {
                NPC.ai[0] = ((!Utils.NextBool(Main.rand, 2)) ? 2 : 0);
            }
            NPC.ai[1] = 0f;
            NPC.ai[2] = 0f;
            NPC.ai[3] = 0f;
            NPC.localAI[0] = 0f;
            NPC.localAI[1] = 0f;
            NPC.netUpdate = true;
        }

        private void SpawnSphereRing(int max, float speed, int damage, float rotationModifier, float offset = 0f)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float rotation = (float)Math.PI * 2f / (float)max;
                int type = ModContent.ProjectileType<MutantSphereRing>();
                for (int i = 0; i < max; i++)
                {
                    Vector2 vel = speed * Utils.RotatedBy(Vector2.UnitY, (double)(rotation * (float)i + offset), default(Vector2));
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel, type, damage, 0f, Main.myPlayer, rotationModifier * (float)NPC.spriteDirection, speed);
                }
                SoundEngine.PlaySound(SoundID.Item84, (Vector2?)((Entity)NPC).Center);
            }
        }

        private bool AliveCheck(Player p, bool forceDespawn = false)
        {
            if (FargoSoulsWorld.SwarmActive || forceDespawn || ((!((Entity)p).active || p.dead || Vector2.Distance(((Entity)NPC).Center, ((Entity)p).Center) > 5000f) && NPC.localAI[3] > 0f))
            {
                NPC.TargetClosest(true);
                p = Main.player[NPC.target];
                if (FargoSoulsWorld.SwarmActive || forceDespawn || !((Entity)p).active || p.dead || Vector2.Distance(((Entity)NPC).Center, ((Entity)p).Center) > 5000f)
                {
                    if (NPC.timeLeft > 30)
                    {
                        NPC.timeLeft = 30;
                    }
                    ((Entity)NPC).velocity.Y -= 1f;
                    if (NPC.timeLeft == 1)
                    {
                        if (((Entity)NPC).position.Y < 0f)
                        {
                            ((Entity)NPC).position.Y = 0f;
                        }
                        ModNPC modNPC = default(ModNPC);
                        if (Main.netMode != NetmodeID.MultiplayerClient && ModContent.TryFind<ModNPC>("YharimEX", "Yharim", out modNPC) && !NPC.AnyNPCs(modNPC.Type)) //this looks for the NPC, which will be added in soon.
                        {
                            YharimEXUtil.ClearHostileProjectiles(2, ((Entity)NPC).whoAmI);
                            int n = NPC.NewNPC(((Entity)NPC).GetSource_FromAI((string)null), (int)((Entity)NPC).Center.X, (int)((Entity)NPC).Center.Y, modNPC.Type, 0, 0f, 0f, 0f, 0f, 255);
                            if (n != 200)
                            {
                                Main.npc[n].homeless = true;
                                if (Main.netMode == NetmodeID.Server)
                                {
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, (NetworkText)null, n, 0f, 0f, 0f, 0, 0, 0);
                                }
                            }
                        }
                    }
                    return false;
                }
            }
            if (NPC.timeLeft < 3600)
            {
                NPC.timeLeft = 3600;
            }
            if (NPC.ai[0] >= 132f && NPC.ai[0] <= 136f)
            {
                return true;
            }
            if ((double)(((Entity)Player).Center.Y / 16f) > Main.worldSurface)
            {
                ((Entity)NPC).velocity.X *= 0.95f;
                ((Entity)NPC).velocity.Y -= 1f;
                if (((Entity)NPC).velocity.Y < -32f)
                {
                    ((Entity)NPC).velocity.Y = -32f;
                }
                return false;
            }
            return true;
        }

        private bool Phase2Check()
        {
            if (Main.expertMode && NPC.life < NPC.lifeMax / 2)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.ai[0] = 10f;
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.ai[3] = 0f;
                    NPC.netUpdate = true;
                    YharimEXUtil.ClearHostileProjectiles(1, ((Entity)NPC).whoAmI);
                }
                return true;
            }
            return false;
        }

        private void Movement(Vector2 target, float speed, bool fastX = true, bool obeySpeedCap = true)
        {
            float turnaroundModifier = 1f;
            float maxSpeed = 24f;
            speed *= 2f;
            turnaroundModifier *= 2f;
            maxSpeed *= 1.5f;
            if (Math.Abs(((Entity)NPC).Center.X - target.X) > 10f)
            {
                if (((Entity)NPC).Center.X < target.X)
                {
                    ((Entity)NPC).velocity.X += speed;
                    if (((Entity)NPC).velocity.X < 0f)
                    {
                        ((Entity)NPC).velocity.X += speed * (float)((!fastX) ? 1 : 2) * turnaroundModifier;
                    }
                }
                else
                {
                    ((Entity)NPC).velocity.X -= speed;
                    if (((Entity)NPC).velocity.X > 0f)
                    {
                        ((Entity)NPC).velocity.X -= speed * (float)((!fastX) ? 1 : 2) * turnaroundModifier;
                    }
                }
            }
            if (((Entity)NPC).Center.Y < target.Y)
            {
                ((Entity)NPC).velocity.Y += speed;
                if (((Entity)NPC).velocity.Y < 0f)
                {
                    ((Entity)NPC).velocity.Y += speed * 2f * turnaroundModifier;
                }
            }
            else
            {
                ((Entity)NPC).velocity.Y -= speed;
                if (((Entity)NPC).velocity.Y > 0f)
                {
                    ((Entity)NPC).velocity.Y -= speed * 2f * turnaroundModifier;
                }
            }
            if (obeySpeedCap)
            {
                if (Math.Abs(((Entity)NPC).velocity.X) > maxSpeed)
                {
                    ((Entity)NPC).velocity.X = maxSpeed * (float)Math.Sign(((Entity)NPC).velocity.X);
                }
                if (Math.Abs(((Entity)NPC).velocity.Y) > maxSpeed)
                {
                    ((Entity)NPC).velocity.Y = maxSpeed * (float)Math.Sign(((Entity)NPC).velocity.Y);
                }
            }
        }

        private void DramaticTransition(bool fightIsOver, bool normalAnimation = true)
        {
            ((Entity)NPC).velocity = Vector2.Zero;
            if (fightIsOver)
            {
                Main.player[NPC.target].ClearBuff(ModContent.BuffType<MutantFang>());
                Main.player[NPC.target].ClearBuff(ModContent.BuffType<AbomRebirth>());
            }
            SoundStyle item = SoundID.Item27;
            item.Volume = 1.5f;
            SoundEngine.PlaySound(item, (Vector2?)((Entity)NPC).Center);
            if (normalAnimation && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantBomb>(), 0, 0f, Main.myPlayer, 0f, 0f);
            }
            float totalAmountToHeal = (fightIsOver ? ((float)Main.player[NPC.target].statLifeMax2 / 4f) : ((float)(NPC.lifeMax - NPC.life) + (float)NPC.lifeMax * 0.1f));
            for (int i = 0; i < 40; i++)
            {
                int heal = (int)(Utils.NextFloat(Main.rand, 0.9f, 1.1f) * totalAmountToHeal / 40f);
                Vector2 vel = (normalAnimation ? (Utils.NextFloat(Main.rand, 2f, 18f) * -Utils.RotatedByRandom(Vector2.UnitY, 6.2831854820251465)) : (0.1f * -Utils.RotatedBy(Vector2.UnitY, (double)((float)Math.PI / 20f * (float)i), default(Vector2))));
                float ai0 = (fightIsOver ? (-((Entity)Main.player[NPC.target]).whoAmI - 1) : ((Entity)NPC).whoAmI);
                float ai1 = ((Vector2)(vel)).Length() / (float)Main.rand.Next(fightIsOver ? 90 : 150, 180);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel, ModContent.ProjectileType<MutantHeal>(), heal, 0f, Main.myPlayer, ai0, ai1);
                }
            }
        }

        private void EModeSpecialEffects()
        {
            if (!FargoSoulsWorld.EternityMode)
            {
                return;
            }
            GameModeData gameModeInfo = Main.GameModeInfo;
            if (((GameModeData)(gameModeInfo)).IsJourneyMode && ((ASharedTogglePower)CreativePowerManager.Instance.GetPower<FreezeTime>()).Enabled)
            {
                ((ASharedTogglePower)CreativePowerManager.Instance.GetPower<FreezeTime>()).SetPowerInfo(false);
            }
            if (!((EffectManager<CustomSky>)(object)SkyManager.Instance)["FargowiltasSouls:MutantBoss"].IsActive())
            {
                ((EffectManager<CustomSky>)(object)SkyManager.Instance).Activate("FargowiltasSouls:MutantBoss", default(Vector2), Array.Empty<object>());
            }
            Mod musicMod = default(Mod);
            if (ModLoader.TryGetMod("FargowiltasMusic", out musicMod))
            {
                if (FargoSoulsWorld.MasochistModeReal && musicMod.Version >= Version.Parse("0.1.1"))
                {
                    Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/Storia");
                }
                else
                {
                    Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/rePrologue");
                }
            }
        }

        private void TryMasoP3Theme()
        {
            Mod musicMod = default(Mod);
            if (FargoSoulsWorld.MasochistModeReal && ModLoader.TryGetMod("FargowiltasMusic", out musicMod) && musicMod.Version >= Version.Parse("0.1.1.3"))
            {
                Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/StoriaShort");
            }
        }

        private void FancyFireballs(int repeats)
        {
            float modifier = 0f;
            for (int i = 0; i < repeats; i++)
            {
                modifier = MathHelper.Lerp(modifier, 1f, 0.08f);
            }
            float distance = 1600f * (1f - modifier);
            float rotation = (float)Math.PI * 2f * modifier;
            for (int j = 0; j < 6; j++)
            {
                int d = Dust.NewDust(((Entity)NPC).Center + distance * Utils.RotatedBy(Vector2.UnitX, (double)(rotation + (float)Math.PI / 3f * (float)j), default(Vector2)), 0, 0, DustID.SolarFlare, ((Entity)NPC).velocity.X * 0.3f, ((Entity)NPC).velocity.Y * 0.3f, 0, Color.White, 1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = 6f - 4f * modifier;
            }
        }

        private void SpearTossDirectP1AndChecks()
        {
            if (!AliveCheck(Player) || Phase2Check())
            {
                return;
            }
            NPC.localAI[2] = 0f;
            Vector2 targetPos = ((Entity)Player).Center;
            targetPos.X += 500 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
            if (((Entity)NPC).Distance(targetPos) > 50f)
            {
                Movement(targetPos, (NPC.localAI[3] > 0f) ? 0.5f : 2f, fastX: true, NPC.localAI[3] > 0f);
            }
            if (NPC.ai[3] == 0f)
            {
                NPC.ai[3] = (FargoSoulsWorld.MasochistModeReal ? Main.rand.Next(2, 8) : 5);
                NPC.netUpdate = true;
            }
            if (NPC.localAI[3] > 0f)
            {
                NPC.ai[1] += 1f;
            }
            if (NPC.ai[1] < 145f)
            {
                NPC.localAI[0] = Utils.ToRotation(((Entity)NPC).DirectionTo(((Entity)Player).Center + ((Entity)Player).velocity * 30f));
            }
            if (NPC.ai[1] > 150f)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 60f;
                if ((NPC.ai[2] += 1f) > NPC.ai[3])
                {
                    P1NextAttackOrMasoOptions(NPC.ai[0]);
                    ((Entity)NPC).velocity = ((Entity)NPC).DirectionTo(((Entity)Player).Center) * 2f;
                }
                else if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vel = Utils.ToRotationVector2(NPC.localAI[0]) * 25f;
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel, ModContent.ProjectileType<MutantSpearThrown>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)NPC.target, 0f);
                    if (FargoSoulsWorld.MasochistModeReal)
                    {
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Normalize(vel), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, -Vector2.Normalize(vel), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                    }
                }
                NPC.localAI[0] = 0f;
            }
            else
            {
                if (NPC.ai[1] != 61f || !(NPC.ai[2] < NPC.ai[3]) || Main.netMode == NetmodeID.MultiplayerClient)
                {
                    return;
                }
                if (FargoSoulsWorld.MasochistModeReal && NPC.ai[2] == 0f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath13, (Vector2?)((Entity)NPC).Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            Vector2 vel2 = Utils.RotatedByRandom(((Entity)NPC).DirectionFrom(((Entity)Player).Center), (double)MathHelper.ToRadians(120f)) * 10f;
                            float ai1 = 0.8f + 0.4f * (float)j / 5f;
                            int current = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel2, ModContent.ProjectileType<MutantDestroyerHead>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)NPC.target, ai1);
                            Main.projectile[current].timeLeft = 90 * ((int)NPC.ai[3] + 1) + 30 + j * 6;
                            int max = Main.rand.Next(8, 19);
                            for (int i = 0; i < max; i++)
                            {
                                current = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel2, ModContent.ProjectileType<MutantDestroyerBody>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)Main.projectile[current].identity, 0f);
                            }
                            int previous = current;
                            current = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel2, ModContent.ProjectileType<MutantDestroyerTail>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)Main.projectile[current].identity, 0f);
                            Main.projectile[previous].localAI[1] = Main.projectile[current].identity;
                            Main.projectile[previous].netUpdate = true;
                        }
                    }
                }
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, ((Entity)NPC).DirectionTo(((Entity)Player).Center + ((Entity)Player).velocity * 30f), ModContent.ProjectileType<MutantDeathrayAim>(), 0, 0f, Main.myPlayer, 85f, (float)((Entity)NPC).whoAmI);
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearAim>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 3f);
            }
        }

        private void OkuuSpheresP1()
        {
            if (Phase2Check())
            {
                return;
            }
            if (FargoSoulsWorld.MasochistModeReal)
            {
                ((Entity)NPC).velocity = Vector2.Zero;
            }
            if ((NPC.ai[1] -= 1f) < 0f)
            {
                NPC.netUpdate = true;
                float modifier = ((!FargoSoulsWorld.MasochistModeReal) ? 1 : 3);
                NPC.ai[1] = 90f / modifier;
                if ((NPC.ai[2] += 1f) > 4f * modifier)
                {
                    P1NextAttackOrMasoOptions(NPC.ai[0]);
                    return;
                }
                int max = (FargoSoulsWorld.MasochistModeReal ? 9 : 6);
                float speed = (FargoSoulsWorld.MasochistModeReal ? 12 : 9);
                int sign = ((!FargoSoulsWorld.MasochistModeReal) ? 1 : ((NPC.ai[2] % 2f == 0f) ? 1 : (-1)));
                SpawnSphereRing(max, speed, (int)(0.8 * (double)YharimEXUtil.ScaledProjectileDamage(NPC.damage)), 1f * (float)sign);
                SpawnSphereRing(max, speed, (int)(0.8 * (double)YharimEXUtil.ScaledProjectileDamage(NPC.damage)), -0.5f * (float)sign);
            }
        }

        private void PrepareTrueEyeDiveP1()
        {
            if (!AliveCheck(Player) || Phase2Check())
            {
                return;
            }
            Vector2 targetPos = ((Entity)Player).Center;
            targetPos.X += 700 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
            targetPos.Y -= 400f;
            Movement(targetPos, 0.6f);
            if (((Entity)NPC).Distance(targetPos) < 50f || (NPC.ai[1] += 1f) > 180f)
            {
                ((Entity)NPC).velocity.X = 35f * (float)((((Entity)NPC).position.X < ((Entity)Player).position.X) ? 1 : (-1));
                if (((Entity)NPC).velocity.Y < 0f)
                {
                    ((Entity)NPC).velocity.Y *= -1f;
                }
                ((Entity)NPC).velocity.Y *= 0.3f;
                NPC.ai[0] += 1f;
                NPC.ai[1] = 0f;
                NPC.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
            }
        }

        private void TrueEyeDive()
        {
            if (NPC.ai[3] == 0f)
            {
                NPC.ai[3] = Math.Sign(((Entity)NPC).Center.X - ((Entity)Player).Center.X);
            }
            if (NPC.ai[2] > 3f)
            {
                Vector2 targetPos = ((Entity)Player).Center;
                targetPos.X += ((((Entity)NPC).Center.X < ((Entity)Player).Center.X) ? (-500) : 500);
                if (((Entity)NPC).Distance(targetPos) > 50f)
                {
                    Movement(targetPos, 0.3f);
                }
            }
            else
            {
                NPC nPC = NPC;
                ((Entity)nPC).velocity = ((Entity)nPC).velocity * 0.99f;
            }
            if (!((NPC.ai[1] -= 1f) < 0f))
            {
                return;
            }
            NPC.ai[1] = 15f;
            int maxEyeThreshold = 6;
            int endlag = 3;
            if ((NPC.ai[2] += 1f) > (float)(maxEyeThreshold + endlag))
            {
                if (NPC.ai[0] == 3f)
                {
                    P1NextAttackOrMasoOptions(2f);
                    return;
                }
                ChooseNextAttack(13, 19, 21, 24, 33, 33, 33, 39, 41, 44);
            }
            else
            {
                if (!(NPC.ai[2] <= (float)maxEyeThreshold))
                {
                    return;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float ratio = NPC.ai[2] / (float)maxEyeThreshold * 3f;
                    int type = ((ratio <= 1f) ? ModContent.ProjectileType<MutantTrueEyeL>() : ((!(ratio <= 2f)) ? ModContent.ProjectileType<MutantTrueEyeR>() : ModContent.ProjectileType<MutantTrueEyeS>()));
                    int p = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, type, YharimEXUtil.ScaledProjectileDamage(NPC.damage, 0.8f), 0f, Main.myPlayer, (float)NPC.target, 0f);
                    if (p != 1000)
                    {
                        Main.projectile[p].localAI[1] = NPC.ai[3];
                        Main.projectile[p].netUpdate = true;
                    }
                }
                SoundEngine.PlaySound(SoundID.Item92, (Vector2?)((Entity)NPC).Center);
                for (int i = 0; i < 30; i++)
                {
                    int d = Dust.NewDust(((Entity)NPC).position, ((Entity)NPC).width, ((Entity)NPC).height, DustID.IceTorch, 0f, 0f, 0, default(Color), 3f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Dust obj = Main.dust[d];
                    obj.velocity *= 12f;
                }
            }
        }

        private void PrepareSpearDashDirectP1()
        {
            if (Phase2Check())
            {
                return;
            }
            if (NPC.ai[3] == 0f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                NPC.ai[3] = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearSpin>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 240f);
                }
            }
            if ((NPC.ai[1] += 1f) > 240f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                NPC.ai[0] += 1f;
                NPC.ai[3] = 0f;
                NPC.netUpdate = true;
            }
            Vector2 targetPos = ((Entity)Player).Center;
            if (((Entity)NPC).Top.Y < ((Entity)Player).Bottom.Y)
            {
                targetPos.X += 600f * (float)Math.Sign(((Entity)NPC).Center.X - ((Entity)Player).Center.X);
            }
            targetPos.Y += 400f;
            Movement(targetPos, 0.7f, fastX: false);
        }

        private void SpearDashDirectP1()
        {
            if (Phase2Check())
            {
                return;
            }
            NPC nPC = NPC;
            ((Entity)nPC).velocity = ((Entity)nPC).velocity * 0.9f;
            if (NPC.ai[3] == 0f)
            {
                NPC.ai[3] = (FargoSoulsWorld.MasochistModeReal ? Main.rand.Next(3, 15) : 10);
            }
            if (!((NPC.ai[1] += 1f) > NPC.ai[3]))
            {
                return;
            }
            NPC.netUpdate = true;
            NPC.ai[0] += 1f;
            NPC.ai[1] = 0f;
            if ((NPC.ai[2] += 1f) > 5f)
            {
                P1NextAttackOrMasoOptions(4f);
                return;
            }
            float speed = (FargoSoulsWorld.MasochistModeReal ? 45f : 30f);
            ((Entity)NPC).velocity = speed * ((Entity)NPC).DirectionTo(((Entity)Player).Center + ((Entity)Player).velocity);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearDash>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 0f);
                if (FargoSoulsWorld.MasochistModeReal)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Normalize(((Entity)NPC).velocity), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, -Vector2.Normalize(((Entity)NPC).velocity), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                }
            }
        }

        private void WhileDashingP1()
        {
            ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(((Entity)NPC).velocity.X));
            if ((NPC.ai[1] += 1f) > 30f && AliveCheck(Player))
            {
                NPC.netUpdate = true;
                NPC.ai[0] -= 1f;
                NPC.ai[1] = 0f;
            }
        }

        private void ApproachForNextAttackP1()
        {
            if (!AliveCheck(Player) || Phase2Check())
            {
                return;
            }
            Vector2 targetPos = ((Entity)Player).Center + ((Entity)Player).DirectionTo(((Entity)NPC).Center) * 250f;
            if (((Entity)NPC).Distance(targetPos) > 50f && (NPC.ai[2] += 1f) < 180f)
            {
                Movement(targetPos, 0.5f);
                return;
            }
            NPC.netUpdate = true;
            NPC.ai[0] += 1f;
            NPC.ai[1] = 0f;
            NPC.ai[2] = Utils.ToRotation(((Entity)Player).DirectionTo(((Entity)NPC).Center));
            NPC.ai[3] = (float)Math.PI / 10f;
            if (((Entity)Player).Center.X < ((Entity)NPC).Center.X)
            {
                NPC.ai[3] *= -1f;
            }
        }

        private void VoidRaysP1()
        {
            if (Phase2Check())
            {
                return;
            }
            ((Entity)NPC).velocity = Vector2.Zero;
            if ((NPC.ai[1] -= 1f) < 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(new Vector2(2f, 0f), (double)NPC.ai[2], default(Vector2)), ModContent.ProjectileType<MutantMark1>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                }
                NPC.ai[1] = (FargoSoulsWorld.MasochistModeReal ? 3 : 5);
                NPC.ai[2] += NPC.ai[3];
                if (NPC.localAI[0]++ == 20f || NPC.localAI[0] == 40f)
                {
                    NPC.netUpdate = true;
                    NPC.ai[2] -= NPC.ai[3] / (float)(FargoSoulsWorld.MasochistModeReal ? 3 : 2);
                }
                else if (NPC.localAI[0] >= (float)(FargoSoulsWorld.MasochistModeReal ? 60 : 40))
                {
                    P1NextAttackOrMasoOptions(7f);
                }
            }
        }

        private void BoundaryBulletHellAndSwordP1()
        {
            switch ((int)NPC.localAI[2])
            {
                case 0:
                    if (NPC.ai[3] == 0f)
                    {
                        if (!AliveCheck(Player))
                        {
                            break;
                        }
                        NPC.ai[3] = 1f;
                        NPC.localAI[0] = Math.Sign(((Entity)NPC).Center.X - ((Entity)Player).Center.X);
                    }
                    if (Phase2Check())
                    {
                        break;
                    }
                ((Entity)NPC).velocity = Vector2.Zero;
                    if ((NPC.ai[1] += 1f) > 2f)
                    {
                        SoundEngine.PlaySound(SoundID.Item12, (Vector2?)((Entity)NPC).Center);
                        NPC.ai[1] = 0f;
                        NPC.ai[2] += (FargoSoulsWorld.MasochistModeReal ? (0.0008181231f * (NPC.ai[3] - 300f) * NPC.localAI[0]) : ((float)Math.PI / 77f));
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int max = (FargoSoulsWorld.MasochistModeReal ? 5 : 4);
                            for (int i = 0; i < max; i++)
                            {
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(new Vector2(0f, -9f), (double)(NPC.ai[2] + (float)Math.PI * 2f / (float)max * (float)i), default(Vector2)), ModContent.ProjectileType<global::FargowiltasSouls.Projectiles.MutantBoss.MutantEye>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                            }
                        }
                    }
                    if ((NPC.ai[3] += 1f) > (float)(FargoSoulsWorld.MasochistModeReal ? 360 : 240))
                    {
                        P1NextAttackOrMasoOptions(NPC.ai[0]);
                    }
                    break;
                case 1:
                    PrepareMutantSword();
                    break;
                case 2:
                    MutantSword();
                    break;
            }
        }

        private void PrepareMutantSword()
        {
            if (NPC.ai[0] == 9f && ((Entity)Main.LocalPlayer).active && ((Entity)NPC).Distance(((Entity)Main.LocalPlayer).Center) < 3000f && Main.expertMode)
            {
                Main.LocalPlayer.AddBuff(ModContent.BuffType<Purged>(), 2, true, false);
            }
            int sign = ((NPC.ai[0] == 9f || NPC.localAI[2] % 2f != 1f) ? 1 : (-1));
            if (NPC.ai[2] == 0f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                Vector2 targetPos = ((Entity)Player).Center;
                targetPos.X += 420 * Math.Sign(((Entity)NPC).Center.X - ((Entity)Player).Center.X);
                targetPos.Y -= 210 * sign;
                Movement(targetPos, 2f);
                if ((!((NPC.localAI[0] += 1f) > 30f) && !FargoSoulsWorld.MasochistModeReal) || !(((Entity)NPC).Distance(targetPos) < 64f))
                {
                    return;
                }
                ((Entity)NPC).velocity = Vector2.Zero;
                NPC.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                NPC.localAI[1] = Math.Sign(((Entity)Player).Center.X - ((Entity)NPC).Center.X);
                float startAngle = (float)Math.PI / 4f * (0f - NPC.localAI[1]);
                NPC.ai[2] = startAngle * -4f / 20f * (float)sign;
                if (sign < 0)
                {
                    startAngle += (float)Math.PI / 2f * (0f - NPC.localAI[1]);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 offset = Utils.RotatedBy(Vector2.UnitY, (double)startAngle, default(Vector2)) * -80f;
                    for (int i = 0; i < 12; i++)
                    {
                        MakeSword(offset * (float)i, 80 * i);
                    }
                    for (int j = -1; j <= 1; j += 2)
                    {
                        MakeSword(Utils.RotatedBy(offset, (double)MathHelper.ToRadians(26.5f * (float)j), default(Vector2)), 180f);
                        MakeSword(Utils.RotatedBy(offset, (double)MathHelper.ToRadians((float)(40 * j)), default(Vector2)), 240f);
                    }
                }
                return;
            }
            if (((Entity)NPC).Distance(swordTarget) > 75f && makedSword)
            {
                Movement(swordTarget, 2.25f);
            }
            else
            {
                NPC nPC = NPC;
                ((Entity)nPC).velocity = ((Entity)nPC).velocity * 0f;
            }
            FancyFireballs((int)(NPC.ai[1] / 60f * 60f));
            if ((NPC.ai[1] += 1f) > 60f)
            {
                if (NPC.ai[0] != 9f)
                {
                    NPC.ai[0] += 1f;
                }
                makedSword = false;
                NPC.localAI[2] += 1f;
                Vector2 targetPos2 = ((Entity)Player).Center;
                targetPos2.X -= 300f * NPC.ai[2];
                ((Entity)NPC).velocity = (targetPos2 - ((Entity)NPC).Center) / 20f;
                NPC.ai[1] = 0f;
                NPC.netUpdate = true;
            }
            ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(NPC.localAI[1]));
            void MakeSword(Vector2 pos, float spacing, float rotation = 0f)
            {
                Projectile.NewProjectileDirect(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center + pos, Vector2.Zero, ModContent.ProjectileType<MutantSword>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, spacing);
            }
        }

        private void MutantSword()
        {
            if (NPC.ai[0] == 9f && ((Entity)Main.LocalPlayer).active && ((Entity)NPC).Distance(((Entity)Main.LocalPlayer).Center) < 3000f && Main.expertMode)
            {
                Main.LocalPlayer.AddBuff(ModContent.BuffType<Purged>(), 2, true, false);
            }
            NPC.ai[3] += NPC.ai[2];
            ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(NPC.localAI[1]));
            Vector2 val2;
            if (NPC.ai[1] == 15f)
            {
                if (!Main.dedServ && ((Entity)Main.LocalPlayer).active)
                {
                    Main.LocalPlayer.GetModPlayer<FargoSoulsPlayer>().Screenshake = 30;
                }
                if ((FargoSoulsWorld.EternityMode && NPC.ai[0] != 9f) || FargoSoulsWorld.MasochistModeReal)
                {
                    if (!Main.dedServ)
                    {
                        SoundStyle val = new SoundStyle("FargowiltasSouls/Sounds/Thunder", SoundType.Sound);
                        val.Pitch = -0.5f;
                        SoundEngine.PlaySound(val, (Vector2?)((Entity)NPC).Center);
                    }
                    float num = Math.Sign(NPC.localAI[1]);
                    float arcSign = Math.Sign(NPC.ai[2]);
                    Vector2 unitX = Vector2.UnitX;
                    double num2 = (float)Math.PI / 4f * arcSign;
                    val2 = default(Vector2);
                    Vector2 offset = num * Utils.RotatedBy(unitX, num2, val2);
                    Vector2 spawnPos = ((Entity)NPC).Center + 480f * offset;
                    Vector2 baseDirection = ((Entity)Player).DirectionFrom(spawnPos);
                    for (int i = 0; i < 8; i++)
                    {
                        double num3 = (float)Math.PI / 4f * (float)i;
                        val2 = default(Vector2);
                        Vector2 angle = Utils.RotatedBy(baseDirection, num3, val2);
                        float ai1 = ((i <= 2 || i == 6) ? 48 : 24);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectileDirect(((Entity)NPC).GetSource_FromThis((string)null), spawnPos + Utils.NextVector2Circular(Main.rand, (float)(((Entity)NPC).width / 2), (float)(((Entity)NPC).height / 2)), Vector2.Zero, ModContent.ProjectileType<MoonLordMoonBlast>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f), 0f, Main.myPlayer, MathHelper.WrapAngle(Utils.ToRotation(angle)), ai1).extraUpdates = 1;
                        }
                    }
                }
            }
            if (!((NPC.ai[1] += 1f) > 18f))
            {
                return;
            }
            if (NPC.ai[0] == 9f)
            {
                P1NextAttackOrMasoOptions(NPC.ai[0]);
            }
            else if (FargoSoulsWorld.MasochistModeReal && NPC.localAI[2] < 5f * endTimeVariance + 2f)
            {
                if (Utils.NextBool(Main.rand, 2))
                {
                    makedSword = true;
                    val2 = ((Entity)Player).Center - ((Entity)NPC).Center;
                    float leng = MathHelper.Clamp(((Vector2)(val2)).Length(), 460f, 600f);
                    for (int j = 0; j < 77; j++)
                    {
                        Dust.NewDust(((Entity)NPC).Center + Utils.NextVector2Circular(Main.rand, 450f, 450f), 0, 0, DustID.SolarFlare, 0f, 0f, 0, default(Color), 1.2f);
                    }
                    swordTarget = ((Entity)Player).Center + Utils.SafeNormalize(((Entity)Player).Center - ((Entity)NPC).Center, Vector2.Zero) * 2.48f * leng;
                    ((Entity)NPC).Center = ((Entity)Player).Center + Utils.SafeNormalize(((Entity)Player).Center - ((Entity)NPC).Center, Vector2.Zero) * leng;
                    for (int k = 0; k < 77; k++)
                    {
                        Dust.NewDust(((Entity)NPC).Center + Utils.NextVector2Circular(Main.rand, 450f, 450f), 0, 0, DustID.ShadowbeamStaff, 0f, 0f, 0, default(Color), 1.2f);
                    }
                }
                else
                {
                    makedSword = false;
                }
                NPC.ai[0] -= 1f;
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                NPC.ai[3] = 0f;
                NPC.localAI[1] = 0f;
                NPC.netUpdate = true;
            }
            else
            {
                ChooseNextAttack(13, 21, 24, 29, 31, 33, 37, 41, 42, 44);
            }
        }

        private void Phase2Transition()
        {
            NPC nPC = NPC;
            ((Entity)nPC).velocity = ((Entity)nPC).velocity * 0.9f;
            NPC.dontTakeDamage = true;
            ClearNewAI();
            if (NPC.buffType[0] != 0)
            {
                NPC.DelBuff(0);
            }
            EModeSpecialEffects();
            if (NPC.ai[2] == 0f)
            {
                if (NPC.ai[1] < 60f && !Main.dedServ && ((Entity)Main.LocalPlayer).active)
                {
                    Main.LocalPlayer.GetModPlayer<FargoSoulsPlayer>().Screenshake = 2;
                }
            }
            else
            {
                ((Entity)NPC).velocity = Vector2.Zero;
            }
            if (NPC.ai[1] < 240f && ((Entity)Main.LocalPlayer).active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost && ((Entity)NPC).Distance(((Entity)Main.LocalPlayer).Center) < 3000f)
            {
                Main.LocalPlayer.controlUseItem = false;
                Main.LocalPlayer.controlUseTile = false;
                Main.LocalPlayer.GetModPlayer<FargoSoulsPlayer>().NoUsingItems = true;
            }
            if (NPC.ai[1] == 0f)
            {
                YharimEXUtil.ClearAllProjectiles(2, ((Entity)NPC).whoAmI);
                if (FargoSoulsWorld.EternityMode)
                {
                    DramaticTransition(fightIsOver: false, NPC.ai[2] == 0f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        ritualProj = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantRitual>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, (float)((Entity)NPC).whoAmI);
                        if (FargoSoulsWorld.MasochistModeReal)
                        {
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantRitual2>(), 0, 0f, Main.myPlayer, 0f, (float)((Entity)NPC).whoAmI);
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantRitual3>(), 0, 0f, Main.myPlayer, 0f, (float)((Entity)NPC).whoAmI);
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantRitual4>(), 0, 0f, Main.myPlayer, 0f, (float)((Entity)NPC).whoAmI);
                        }
                    }
                }
            }
            else if (NPC.ai[1] == 150f)
            {
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), 0, 0f, Main.myPlayer, 5f, 0f);
                }
                for (int i = 0; i < 50; i++)
                {
                    int d = Dust.NewDust(((Entity)Main.LocalPlayer).position, ((Entity)Main.LocalPlayer).width, ((Entity)Main.LocalPlayer).height, DustID.RedTorch, 0f, 0f, 0, default(Color), 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Dust obj = Main.dust[d];
                    obj.velocity *= 9f;
                }
            }
            else if (NPC.ai[1] > 150f)
            {
                NPC.localAI[3] = 3f;
            }
            if ((NPC.ai[1] += 1f) > 270f)
            {
                if (FargoSoulsWorld.EternityMode)
                {
                    NPC.life = NPC.lifeMax;
                    NPC.ai[0] = Utils.Next<int>(Main.rand, new int[13]
                    {
                    11, 13, 16, 19, 20, 21, 24, 26, 29, 35,
                    37, 39, 42
                    });
                }
                else
                {
                    NPC.ai[0] += 1f;
                }
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                NPC.netUpdate = true;
                attackHistory.Enqueue(NPC.ai[0]);
            }
        }

        private void ApproachForNextAttackP2()
        {
            if (!AliveCheck(Player))
            {
                return;
            }
            Vector2 targetPos = ((Entity)Player).Center + ((Entity)Player).DirectionTo(((Entity)NPC).Center) * 300f;
            if (((Entity)NPC).Distance(targetPos) > 50f && (NPC.ai[2] += 1f) < 180f)
            {
                Movement(targetPos, 0.8f);
                return;
            }
            NPC.netUpdate = true;
            NPC.ai[0] += 1f;
            NPC.ai[1] = 0f;
            NPC.ai[2] = Utils.ToRotation(((Entity)Player).DirectionTo(((Entity)NPC).Center));
            NPC.ai[3] = (float)Math.PI / 10f;
            NPC.localAI[0] = 0f;
            SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
            if (((Entity)Player).Center.X < ((Entity)NPC).Center.X)
            {
                NPC.ai[3] *= -1f;
            }
        }

        private void VoidRaysP2()
        {
            ((Entity)NPC).velocity = Vector2.Zero;
            if (!((NPC.ai[1] -= 1f) < 0f))
            {
                return;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(new Vector2(2f, 0f), (double)NPC.ai[2], default(Vector2)), ModContent.ProjectileType<MutantMark1>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
            }
            NPC.ai[1] = 3f;
            NPC.ai[2] += NPC.ai[3];
            if (NPC.localAI[0]++ == 20f || NPC.localAI[0] == 40f)
            {
                NPC.netUpdate = true;
                NPC.ai[2] -= NPC.ai[3] / (float)(FargoSoulsWorld.MasochistModeReal ? 3 : 2);
                if ((NPC.localAI[0] == 21f && endTimeVariance > 0.75f) || (NPC.localAI[0] == 41f && endTimeVariance < 0.25f))
                {
                    NPC.localAI[0] = 60f;
                }
            }
            else if (NPC.localAI[0] >= 60f)
            {
                ChooseNextAttack(13, 19, 21, 24, 31, 39, 41, 42);
            }
        }

        private void PrepareSpearDashPredictiveP2()
        {
            if (NPC.ai[3] == 0f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                NPC.ai[3] = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearSpin>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 180f);
                }
            }
            if ((NPC.ai[1] += 1f) > 180f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                NPC.netUpdate = true;
                NPC.ai[0] += 1f;
                NPC.ai[1] = 0f;
                NPC.ai[3] = 0f;
            }
            Vector2 targetPos = ((Entity)Player).Center;
            targetPos.Y += 400f * (float)Math.Sign(((Entity)NPC).Center.Y - ((Entity)Player).Center.Y);
            Movement(targetPos, 0.7f, fastX: false);
            if (((Entity)NPC).Distance(((Entity)Player).Center) < 200f)
            {
                Movement(((Entity)NPC).Center + ((Entity)NPC).DirectionFrom(((Entity)Player).Center), 1.4f);
            }
        }

        private void SpearDashPredictiveP2()
        {
            if (NPC.localAI[1] == 0f)
            {
                if (FargoSoulsWorld.EternityMode)
                {
                    NPC.localAI[1] = Main.rand.Next(FargoSoulsWorld.MasochistModeReal ? 3 : 5, 9);
                }
                else
                {
                    NPC.localAI[1] = 5f;
                }
            }
            if (NPC.ai[1] == 0f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                if (NPC.ai[2] == NPC.localAI[1] - 1f)
                {
                    if (((Entity)NPC).Distance(((Entity)Player).Center) > 450f)
                    {
                        Movement(((Entity)Player).Center, 0.6f);
                        return;
                    }
                    NPC nPC = NPC;
                    ((Entity)nPC).velocity = ((Entity)nPC).velocity * 0.75f;
                }
                if (NPC.ai[2] < NPC.localAI[1])
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, ((Entity)NPC).DirectionTo(((Entity)Player).Center + ((Entity)Player).velocity * 30f), ModContent.ProjectileType<MutantDeathrayAim>(), 0, 0f, Main.myPlayer, 55f, (float)((Entity)NPC).whoAmI);
                    }
                    if (NPC.ai[2] == NPC.localAI[1] - 1f)
                    {
                        SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearAim>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 4f);
                        }
                    }
                }
            }
            NPC nPC2 = NPC;
            ((Entity)nPC2).velocity = ((Entity)nPC2).velocity * 0.9f;
            if (NPC.ai[1] < 55f)
            {
                NPC.localAI[0] = Utils.ToRotation(((Entity)NPC).DirectionTo(((Entity)Player).Center + ((Entity)Player).velocity * 30f));
            }
            int endTime = 60;
            if (NPC.ai[2] == NPC.localAI[1] - 1f)
            {
                endTime = 80;
            }
            if (FargoSoulsWorld.MasochistModeReal && (NPC.ai[2] == 0f || NPC.ai[2] >= NPC.localAI[1]))
            {
                endTime = 0;
            }
            if (!((NPC.ai[1] += 1f) > (float)endTime))
            {
                return;
            }
            NPC.netUpdate = true;
            NPC.ai[0] += 1f;
            NPC.ai[1] = 0f;
            NPC.ai[3] = 0f;
            if ((NPC.ai[2] += 1f) > NPC.localAI[1])
            {
                ChooseNextAttack(16, 19, 20, 26, 29, 31, 33, 39, 42, 44, 45);
            }
            else
            {
                ((Entity)NPC).velocity = Utils.ToRotationVector2(NPC.localAI[0]) * 45f;
                float spearAi = 0f;
                if (NPC.ai[2] == NPC.localAI[1])
                {
                    spearAi = -2f;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Normalize(((Entity)NPC).velocity), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, -Vector2.Normalize(((Entity)NPC).velocity), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearDash>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, spearAi);
                }
            }
            NPC.localAI[0] = 0f;
        }

        private void WhileDashingP2()
        {
            ((Entity)NPC).direction = (NPC.spriteDirection = Math.Sign(((Entity)NPC).velocity.X));
            if ((NPC.ai[1] += 1f) > 30f && AliveCheck(Player))
            {
                NPC.netUpdate = true;
                NPC.ai[0] -= 1f;
                NPC.ai[1] = 0f;
                if (NPC.ai[0] == 14f && NPC.ai[2] == NPC.localAI[1] - 1f && ((Entity)NPC).Distance(((Entity)Player).Center) > 450f)
                {
                    ((Entity)NPC).velocity = ((Entity)NPC).DirectionTo(((Entity)Player).Center) * 16f;
                }
            }
        }

        private void BoundaryBulletHellP2()
        {
            ((Entity)NPC).velocity = Vector2.Zero;
            if (NPC.localAI[0] == 0f)
            {
                NPC.localAI[0] = Math.Sign(((Entity)NPC).Center.X - ((Entity)Player).Center.X);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, -2f);
                }
            }
            if (NPC.ai[3] > 60f && (NPC.ai[1] += 1f) > 2f)
            {
                SoundEngine.PlaySound(SoundID.Item12, (Vector2?)((Entity)NPC).Center);
                NPC.ai[1] = 0f;
                NPC.ai[2] += 0.0008181231f * NPC.ai[3] * NPC.localAI[0];
                if (NPC.ai[2] > (float)Math.PI)
                {
                    NPC.ai[2] -= (float)Math.PI * 2f;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int max = 4;
                    if (FargoSoulsWorld.EternityMode)
                    {
                        max++;
                    }
                    if (FargoSoulsWorld.MasochistModeReal)
                    {
                        max++;
                    }
                    for (int i = 0; i < max; i++)
                    {
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(new Vector2(0f, -6f), (double)NPC.ai[2] + Math.PI * 2.0 / (double)max * (double)i, default(Vector2)), ModContent.ProjectileType<global::FargowiltasSouls.Projectiles.MutantBoss.MutantEye>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                    }
                }
            }
            int endTime = 420 + (int)(480f * (endTimeVariance - 0.33f));
            if ((NPC.ai[3] += 1f) > (float)endTime)
            {
                int[] obj = new int[10] { 11, 13, 19, 20, 21, 24, 0, 33, 41, 44 };
                obj[6] = (FargoSoulsWorld.MasochistModeReal ? 31 : 26);
                ChooseNextAttack(obj);
            }
        }

        private void PillarDunk()
        {
            if (!AliveCheck(Player))
            {
                return;
            }
            int pillarAttackDelay = 60;
            if (NPC.ai[2] == 0f && NPC.ai[3] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Clone(-1f, 1f, pillarAttackDelay * 4);
                    Clone(1f, -1f, pillarAttackDelay * 2);
                    Clone(1f, 1f, pillarAttackDelay * 3);
                    if (FargoSoulsWorld.MasochistModeReal)
                    {
                        Clone(1f, 1f, pillarAttackDelay * 6);
                    }
                }
                NPC.netUpdate = true;
                NPC.ai[2] = ((Entity)NPC).Center.X;
                NPC.ai[3] = ((Entity)NPC).Center.Y;
                for (int i = 0; i < 1000; i++)
                {
                    if (((Entity)Main.projectile[i]).active && Main.projectile[i].type == ModContent.ProjectileType<MutantRitual>() && Main.projectile[i].ai[1] == (float)((Entity)NPC).whoAmI)
                    {
                        NPC.ai[2] = ((Entity)Main.projectile[i]).Center.X;
                        NPC.ai[3] = ((Entity)Main.projectile[i]).Center.Y;
                        break;
                    }
                }
                Vector2 offset = 1000f * Utils.RotatedBy(Vector2.UnitX, (double)MathHelper.ToRadians(45f), default(Vector2));
                if (Utils.NextBool(Main.rand))
                {
                    if (((Entity)Player).Center.X > NPC.ai[2])
                    {
                        offset.X *= -1f;
                    }
                    if (Utils.NextBool(Main.rand))
                    {
                        offset.Y *= -1f;
                    }
                }
                else
                {
                    if (Utils.NextBool(Main.rand))
                    {
                        offset.X *= -1f;
                    }
                    if (((Entity)Player).Center.Y > NPC.ai[3])
                    {
                        offset.Y *= -1f;
                    }
                }
                NPC.localAI[1] = NPC.ai[2];
                NPC.localAI[2] = NPC.ai[3];
                NPC.ai[2] = offset.Length();
                NPC.ai[3] = Utils.ToRotation(offset);
            }
            Vector2 targetPos = ((Entity)Player).Center;
            targetPos.X += ((((Entity)NPC).Center.X < ((Entity)Player).Center.X) ? (-700) : 700);
            targetPos.Y += ((NPC.ai[1] < 240f) ? 400 : 150);
            if (((Entity)NPC).Distance(targetPos) > 50f)
            {
                Movement(targetPos, 1f);
            }
            int endTime = 240 + pillarAttackDelay * 4 + 60;
            if (FargoSoulsWorld.MasochistModeReal)
            {
                endTime += pillarAttackDelay * 2;
            }
            NPC.localAI[0] = (float)endTime - NPC.ai[1];
            NPC.localAI[0] += 60f + 60f * (1f - NPC.ai[1] / (float)endTime);
            if (NPC.ai[1] == 95f || NPC.ai[1] == 135f || NPC.ai[1] == (float)(endTime - 30))
            {
                for (int j = 0; j < 3; j++)
                {
                    Vector2 dir = ((Entity)Player).Center - ((Entity)NPC).Center;
                    float ai1New = (Utils.NextBool(Main.rand) ? 1 : (-1));
                    Vector2 vel = Vector2.Normalize(Utils.RotatedByRandom(dir, Math.PI / 4.0)) * 38f;
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel, ModContent.ProjectileType<HostileLightning>(), 30, 0f, Main.myPlayer, Utils.ToRotation(dir), ai1New);
                }
            }
            if ((NPC.ai[1] += 1f) > (float)endTime)
            {
                ChooseNextAttack(11, 13, 20, 21, 26, 33, 41, 44);
            }
            else if (NPC.ai[1] == (float)pillarAttackDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.UnitY * -5f, ModContent.ProjectileType<MutantPillar>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f), 0f, Main.myPlayer, 3f, (float)((Entity)NPC).whoAmI);
                }
            }
            else if (FargoSoulsWorld.MasochistModeReal && NPC.ai[1] == (float)(pillarAttackDelay * 5) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.UnitY * -5f, ModContent.ProjectileType<MutantPillar>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f), 0f, Main.myPlayer, 1f, (float)((Entity)NPC).whoAmI);
            }
            void Clone(float ai1, float ai2, float ai3)
            {
                YharimEXUtil.NewNPCEasy(((Entity)NPC).GetSource_FromAI((string)null), ((Entity)NPC).Center, ModContent.NPCType<MutantIllusion>(), ((Entity)NPC).whoAmI, ((Entity)NPC).whoAmI, ai1, ai2, ai3);
            }
        }

        private void EOCStarSickles()
        {
            if (!AliveCheck(Player))
            {
                return;
            }
            if (NPC.ai[1] == 0f)
            {
                float ai1 = 0f;
                if (FargoSoulsWorld.MasochistModeReal)
                {
                    ai1 = 30f;
                    NPC.ai[1] = 30f;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int p = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, -Vector2.UnitY, ModContent.ProjectileType<MutantEyeOfCthulhu>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)NPC.target, ai1);
                    if (FargoSoulsWorld.MasochistModeReal && p != 1000)
                    {
                        Projectile obj = Main.projectile[p];
                        obj.timeLeft -= 30;
                    }
                }
            }
            if (NPC.ai[1] < 120f)
            {
                NPC.ai[2] = ((Entity)Player).Center.X;
                NPC.ai[3] = ((Entity)Player).Center.Y;
            }
            if (NPC.ai[1] == 120f || NPC.ai[1] == 156f)
            {
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                Vector2 offset = ((Entity)NPC).Center - ((Entity)Player).Center;
                Vector2 spawnPos = ((Entity)Player).Center;
                switch (Main.rand.Next(4))
                {
                    case 0:
                        LaserSpread(new Vector2(spawnPos.X + offset.X, spawnPos.Y + offset.Y));
                        LaserSpread(new Vector2(spawnPos.X + offset.X, spawnPos.Y - offset.Y));
                        TelegraphConfusion(new Vector2(spawnPos.X + offset.X, spawnPos.Y - offset.Y));
                        TelegraphConfusion(new Vector2(spawnPos.X + offset.X, spawnPos.Y + offset.Y));
                        break;
                    case 1:
                        LaserSpread(new Vector2(spawnPos.X + offset.X, spawnPos.Y - offset.Y));
                        TelegraphConfusion(new Vector2(spawnPos.X + offset.X, spawnPos.Y - offset.Y));
                        LaserSpread(new Vector2(spawnPos.X - offset.X, spawnPos.Y + offset.Y));
                        TelegraphConfusion(new Vector2(spawnPos.X - offset.X, spawnPos.Y + offset.Y));
                        break;
                    case 2:
                        LaserSpread(new Vector2(spawnPos.X - offset.X, spawnPos.Y + offset.Y));
                        TelegraphConfusion(new Vector2(spawnPos.X - offset.X, spawnPos.Y + offset.Y));
                        LaserSpread(new Vector2(spawnPos.X - offset.X, spawnPos.Y - offset.Y));
                        TelegraphConfusion(new Vector2(spawnPos.X - offset.X, spawnPos.Y - offset.Y));
                        break;
                    case 3:
                        LaserSpread(new Vector2(spawnPos.X - offset.X, spawnPos.Y - offset.Y));
                        TelegraphConfusion(new Vector2(spawnPos.X - offset.X, spawnPos.Y - offset.Y));
                        LaserSpread(new Vector2(spawnPos.X + offset.X, spawnPos.Y + offset.Y));
                        TelegraphConfusion(new Vector2(spawnPos.X + offset.X, spawnPos.Y + offset.Y));
                        break;
                }
            }
            Vector2 targetPos = default(Vector2);
            targetPos = new Vector2(NPC.ai[2], NPC.ai[3]);
            targetPos += Utils.RotatedBy(((Entity)NPC).DirectionFrom(targetPos), (double)MathHelper.ToRadians(-5f), default(Vector2)) * 450f;
            if (((Entity)NPC).Distance(targetPos) > 50f)
            {
                Movement(targetPos, 0.25f);
            }
            if ((NPC.ai[1] += 1f) > 450f)
            {
                ChooseNextAttack(11, 13, 16, 21, 26, 29, 31, 33, 35, 37, 41, 44, 45);
            }
            void LaserSpread(Vector2 spawn)
            {
                int max = (FargoSoulsWorld.MasochistModeReal ? 3 : 3);
                int degree = 1;
                int laserDamage = YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f);
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawn, new Vector2(0f, -4f), ModContent.ProjectileType<BrainofConfusion>(), 0, 0f, Main.myPlayer, 0f, 0f);
                for (int i = -max; i <= max; i++)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawn, 0.2f * Utils.RotatedBy(((Entity)Player).DirectionFrom(spawn), (double)(MathHelper.ToRadians((float)degree) * (float)i), default(Vector2)), ModContent.ProjectileType<DestroyerLaser>(), laserDamage, 0f, Main.myPlayer, 0f, 0f);
                }
            }
            void TelegraphConfusion(Vector2 spawn)
            {
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawn, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), 0, 0f, Main.myPlayer, 8f, 180f);
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawn, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), 0, 0f, Main.myPlayer, 8f, 200f);
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawn, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), 0, 0f, Main.myPlayer, 8f, 220f);
            }
        }

        private void PrepareSpearDashDirectP2()
        {
            if (NPC.ai[3] == 0f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                NPC.ai[3] = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearSpin>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 180f);
                }
            }
            if ((NPC.ai[1] += 1f) > 180f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                NPC.netUpdate = true;
                NPC.ai[0] += 1f;
                NPC.ai[1] = 0f;
                NPC.ai[3] = 0f;
            }
            Vector2 targetPos = ((Entity)Player).Center;
            targetPos.Y += 450f * (float)Math.Sign(((Entity)NPC).Center.Y - ((Entity)Player).Center.Y);
            Movement(targetPos, 0.7f, fastX: false);
            if (((Entity)NPC).Distance(((Entity)Player).Center) < 200f)
            {
                Movement(((Entity)NPC).Center + ((Entity)NPC).DirectionFrom(((Entity)Player).Center), 1.4f);
            }
        }

        private void SpearDashDirectP2()
        {
            NPC nPC = NPC;
            ((Entity)nPC).velocity = ((Entity)nPC).velocity * 0.9f;
            if (NPC.localAI[1] == 0f)
            {
                if (FargoSoulsWorld.EternityMode)
                {
                    NPC.localAI[1] = Main.rand.Next(FargoSoulsWorld.MasochistModeReal ? 3 : 5, 9);
                }
                else
                {
                    NPC.localAI[1] = 5f;
                }
            }
            if (!((NPC.ai[1] += 1f) > (float)(FargoSoulsWorld.EternityMode ? 5 : 20)))
            {
                return;
            }
            NPC.netUpdate = true;
            NPC.ai[0] += 1f;
            NPC.ai[1] = 0f;
            if ((NPC.ai[2] += 1f) > NPC.localAI[1])
            {
                if (FargoSoulsWorld.MasochistModeReal)
                {
                    ChooseNextAttack(11, 13, 16, 19, 20, 31, 33, 35, 39, 42, 44);
                }
                else
                {
                    ChooseNextAttack(11, 16, 26, 29, 31, 35, 37, 39, 42, 44);
                }
            }
            else
            {
                ((Entity)NPC).velocity = ((Entity)NPC).DirectionTo(((Entity)Player).Center) * (FargoSoulsWorld.MasochistModeReal ? 60f : 45f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Normalize(((Entity)NPC).velocity), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 0.8f), 0f, Main.myPlayer, 0f, 0f);
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, -Vector2.Normalize(((Entity)NPC).velocity), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 0.8f), 0f, Main.myPlayer, 0f, 0f);
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearDash>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 0f);
                }
            }
        }

        private void SpawnDestroyersForPredictiveThrow()
        {
            if (!AliveCheck(Player))
            {
                return;
            }
            if (FargoSoulsWorld.EternityMode)
            {
                Vector2 targetPos = ((Entity)Player).Center + ((Entity)NPC).DirectionFrom(((Entity)Player).Center) * 500f;
                if (Math.Abs(targetPos.X - ((Entity)Player).Center.X) < 150f)
                {
                    targetPos.X = ((Entity)Player).Center.X + (float)(150 * Math.Sign(targetPos.X - ((Entity)Player).Center.X));
                    Movement(targetPos, 0.3f);
                }
                if (((Entity)NPC).Distance(targetPos) > 50f)
                {
                    Movement(targetPos, 0.9f);
                }
            }
            else
            {
                Vector2 targetPos2 = ((Entity)Player).Center;
                targetPos2.X += 500 * ((!(((Entity)NPC).Center.X < targetPos2.X)) ? 1 : (-1));
                if (((Entity)NPC).Distance(targetPos2) > 50f)
                {
                    Movement(targetPos2, 0.4f);
                }
            }
            if (NPC.localAI[1] == 0f)
            {
                if (FargoSoulsWorld.EternityMode)
                {
                    NPC.localAI[1] = Main.rand.Next(FargoSoulsWorld.MasochistModeReal ? 3 : 5, 9);
                }
                else
                {
                    NPC.localAI[1] = 5f;
                }
            }
            if (!((NPC.ai[1] += 1f) > 60f))
            {
                return;
            }
            NPC.netUpdate = true;
            NPC.ai[1] = 30f;
            int cap = 3;
            if (FargoSoulsWorld.EternityMode)
            {
                cap += 2;
            }
            if (FargoSoulsWorld.MasochistModeReal)
            {
                cap += 2;
                NPC.ai[1] += 15f;
            }
            if ((NPC.ai[2] += 1f) > (float)cap)
            {
                NPC.ai[0] += 1f;
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                return;
            }
            SoundEngine.PlaySound(SoundID.NPCDeath13, (Vector2?)((Entity)NPC).Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 vel = Utils.RotatedByRandom(((Entity)NPC).DirectionFrom(((Entity)Player).Center), (double)MathHelper.ToRadians(120f)) * 10f;
                float ai1 = 0.8f + 0.4f * NPC.ai[2] / 5f;
                if (FargoSoulsWorld.MasochistModeReal)
                {
                    ai1 += 0.4f;
                }
                int current = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel, ModContent.ProjectileType<MutantDestroyerHead>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)NPC.target, ai1);
                Main.projectile[current].timeLeft = 30 * (cap - (int)NPC.ai[2]) + 60 * (int)NPC.localAI[1] + 30 + (int)NPC.ai[2] * 6;
                int max = Main.rand.Next(8, 19);
                for (int i = 0; i < max; i++)
                {
                    current = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel, ModContent.ProjectileType<MutantDestroyerBody>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)Main.projectile[current].identity, 0f);
                }
                int previous = current;
                current = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel, ModContent.ProjectileType<MutantDestroyerTail>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)Main.projectile[current].identity, 0f);
                Main.projectile[previous].localAI[1] = Main.projectile[current].identity;
                Main.projectile[previous].netUpdate = true;
            }
        }

        private void SpearTossPredictiveP2()
        {
            if (!AliveCheck(Player))
            {
                return;
            }
            Vector2 targetPos = ((Entity)Player).Center;
            targetPos.X += 500 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
            if (((Entity)NPC).Distance(targetPos) > 25f)
            {
                Movement(targetPos, 0.8f);
            }
            if ((NPC.ai[1] += 1f) > 60f)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 0f;
                bool shouldAttack = true;
                if ((NPC.ai[2] += 1f) > NPC.localAI[1])
                {
                    shouldAttack = false;
                    if (FargoSoulsWorld.MasochistModeReal)
                    {
                        ChooseNextAttack(11, 19, 20, 29, 31, 33, 35, 37, 39, 42, 44, 45);
                    }
                    else
                    {
                        ChooseNextAttack(11, 19, 20, 26, 26, 26, 29, 31, 33, 35, 37, 39, 42, 44);
                    }
                }
                if ((shouldAttack || FargoSoulsWorld.MasochistModeReal) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vel = ((Entity)NPC).DirectionTo(((Entity)Player).Center + ((Entity)Player).velocity * 30f) * 30f;
                    for (int i = -1; i <= 1; i++)
                    {
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(Vector2.Normalize(vel), (double)((float)i * 0.12f), default(Vector2)), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 0.8f), 0f, Main.myPlayer, 0f, 0f);
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, -Utils.RotatedBy(Vector2.Normalize(vel), (double)((float)i * 0.12f), default(Vector2)), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 0.8f), 0f, Main.myPlayer, 0f, 0f);
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(vel, (double)((float)i * 0.12f), default(Vector2)), ModContent.ProjectileType<MutantSpearThrown>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)NPC.target, 1f);
                    }
                }
            }
            else if (NPC.ai[1] == 1f && (NPC.ai[2] < NPC.localAI[1] || FargoSoulsWorld.MasochistModeReal) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(((Entity)NPC).DirectionTo(((Entity)Player).Center + ((Entity)Player).velocity * 30f), (double)((float)j * 0.12f), default(Vector2)), ModContent.ProjectileType<MutantDeathrayAim>(), 0, 0f, Main.myPlayer, 60f, (float)((Entity)NPC).whoAmI);
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearAim>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 2f);
                }
            }
        }

        private void PrepareMechRayFan()
        {
            if (NPC.ai[1] == 0f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                if (FargoSoulsWorld.MasochistModeReal)
                {
                    NPC.ai[1] = 31f;
                }
            }
            if (NPC.ai[1] == 30f)
            {
                SoundEngine.PlaySound(SoundID.ForceRoarPitched, (Vector2?)((Entity)NPC).Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 125f);
                }
            }
            Vector2 targetPos;
            if (NPC.ai[1] < 30f)
            {
                targetPos = ((Entity)Player).Center + Utils.RotatedBy(((Entity)NPC).DirectionFrom(((Entity)Player).Center), (double)MathHelper.ToRadians(15f), default(Vector2)) * 500f;
                if (((Entity)NPC).Distance(targetPos) > 50f)
                {
                    Movement(targetPos, 0.3f);
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    int d = Dust.NewDust(((Entity)NPC).Center, 0, 0, DustID.Torch, 0f, 0f, 0, default(Color), 3f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Dust obj = Main.dust[d];
                    obj.velocity *= 12f;
                }
                targetPos = ((Entity)Player).Center;
                targetPos.X += 600 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
                Movement(targetPos, 1.2f, fastX: false);
            }
            if ((NPC.ai[1] += 1f) > 150f || (FargoSoulsWorld.MasochistModeReal && ((Entity)NPC).Distance(targetPos) < 64f))
            {
                NPC.netUpdate = true;
                NPC.ai[0] += 1f;
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                NPC.ai[3] = 0f;
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
            }
        }

        private void MechRayFan()
        {
            ((Entity)NPC).velocity = Vector2.Zero;
            if (NPC.ai[2] == 0f)
            {
                NPC.ai[2] = ((!Utils.NextBool(Main.rand)) ? 1 : (-1));
            }
            if (NPC.ai[3] == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int max = 7;
                for (int i = 0; i <= max; i++)
                {
                    Vector2 dir = Utils.RotatedBy(Vector2.UnitX, (double)(NPC.ai[2] * (float)i * (float)Math.PI / (float)max), default(Vector2)) * 6f;
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center + dir, Vector2.Zero, ModContent.ProjectileType<MutantGlowything>(), 0, 0f, Main.myPlayer, Utils.ToRotation(dir), (float)((Entity)NPC).whoAmI);
                }
            }
            int endTime = 390;
            int timeBeforeAttackEnds;
            if (NPC.ai[3] > (float)(FargoSoulsWorld.MasochistModeReal ? 45 : 60) && NPC.ai[3] < 240f && (NPC.ai[1] += 1f) > 10f)
            {
                NPC.ai[1] = 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float rotation = MathHelper.ToRadians(245f) * NPC.ai[2] / 80f;
                    timeBeforeAttackEnds = endTime - (int)NPC.ai[3];
                    SpawnRay(((Entity)NPC).Center, 8f * NPC.ai[2], rotation);
                    SpawnRay(((Entity)NPC).Center, -8f * NPC.ai[2] + 180f, 0f - rotation);
                    if (FargoSoulsWorld.MasochistModeReal)
                    {
                        Vector2 spawnPos = ((Entity)NPC).Center + NPC.ai[2] * -1200f * Vector2.UnitY;
                        SpawnRay(spawnPos, 8f * NPC.ai[2] + 180f, rotation);
                        SpawnRay(spawnPos, -8f * NPC.ai[2], 0f - rotation);
                    }
                }
            }
            if (NPC.ai[3] > 210f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float spawnOffset = (float)((!Utils.NextBool(Main.rand)) ? 1 : (-1)) * Utils.NextFloat(Main.rand, 1400f, 1800f);
                float maxVariance = MathHelper.ToRadians(16f);
                Vector2 aimPoint = ((Entity)NPC).Center - Vector2.UnitY * NPC.ai[2] * 600f;
                Vector2 spawnPos2 = aimPoint + spawnOffset * Utils.RotatedBy(Utils.RotatedByRandom(Vector2.UnitX, (double)maxVariance), (double)MathHelper.ToRadians(0f), default(Vector2));
                Vector2 vel = 32f * Vector2.Normalize(aimPoint - spawnPos2);
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos2, vel, ModContent.ProjectileType<MutantGuardian>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f), 0f, Main.myPlayer, 0f, 0f);
            }
            if (NPC.ai[3] < 180f && (NPC.localAI[0] += 1f) > 1f)
            {
                NPC.localAI[0] = 0f;
                SpawnPrime(15f, 0f);
            }
            if ((NPC.ai[3] += 1f) > (float)endTime)
            {
                if (FargoSoulsWorld.EternityMode)
                {
                    ChooseNextAttack(11, 13, 16, 19, 21, 24, 29, 31, 33, 35, 37, 39, 41, 42, 45);
                }
                else
                {
                    NPC.ai[0] = 11f;
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.ai[3] = 0f;
                }
                NPC.netUpdate = true;
            }
            void SpawnPrime(float varianceInDegrees, float rotationInDegrees)
            {
                SoundEngine.PlaySound(SoundID.Item21, (Vector2?)((Entity)NPC).Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float spawnOffset2 = (float)((!Utils.NextBool(Main.rand)) ? 1 : (-1)) * Utils.NextFloat(Main.rand, 1400f, 1800f);
                    float maxVariance2 = MathHelper.ToRadians(varianceInDegrees);
                    Vector2 aimPoint2 = ((Entity)NPC).Center - Vector2.UnitY * NPC.ai[2] * 600f;
                    Vector2 spawnPos3 = aimPoint2 + spawnOffset2 * Utils.RotatedBy(Utils.RotatedByRandom(Vector2.UnitY, (double)maxVariance2), (double)MathHelper.ToRadians(rotationInDegrees), default(Vector2));
                    Vector2 vel2 = 32f * Vector2.Normalize(aimPoint2 - spawnPos3);
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos3, vel2, ModContent.ProjectileType<MutantGuardian>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f), 0f, Main.myPlayer, 0f, 0f);
                }
            }
            void SpawnRay(Vector2 pos, float angleInDegrees, float turnRotation)
            {
                int p = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), pos, Utils.ToRotationVector2(MathHelper.ToRadians(angleInDegrees)), ModContent.ProjectileType<MutantDeathray3>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, turnRotation, (float)((Entity)NPC).whoAmI);
                if (p != 1000 && Main.projectile[p].timeLeft > timeBeforeAttackEnds)
                {
                    Main.projectile[p].timeLeft = timeBeforeAttackEnds;
                }
            }
        }

        private void PrepareFishron1()
        {
            if (AliveCheck(Player))
            {
                Vector2 targetPos = default(Vector2);
                targetPos = new Vector2(Player.Center.X, Player.Center.Y + 600f * Math.Sign(NPC.Center.Y - Player.Center.Y));
                Movement(targetPos, 1.4f, fastX: false);
                if (NPC.ai[1] == 0f)
                {
                    NPC.ai[2] = Math.Sign(((Entity)NPC).Center.X - ((Entity)Player).Center.X);
                }
                if ((NPC.ai[1] += 1f) > 60f || ((Entity)NPC).Distance(targetPos) < 64f)
                {
                    ((Entity)NPC).velocity.X = 30f * NPC.ai[2];
                    ((Entity)NPC).velocity.Y = 0f;
                    NPC.ai[0] += 1f;
                    NPC.ai[1] = 0f;
                    NPC.netUpdate = true;
                }
            }
        }

        private void SpawnFishrons()
        {
            NPC nPC = NPC;
            ((Entity)nPC).velocity = ((Entity)nPC).velocity * 0.97f;
            if (NPC.ai[1] == 0f)
            {
                NPC.ai[2] = (Utils.NextBool(Main.rand) ? 1 : 0);
            }
            int maxFishronSets = (FargoSoulsWorld.MasochistModeReal ? 3 : 2);
            if (NPC.ai[1] % 3f == 0f && NPC.ai[1] <= (float)(3 * maxFishronSets))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        int max = (int)NPC.ai[1] / 3;
                        for (int i = -max; i <= max; i++)
                        {
                            if (Math.Abs(i) == max)
                            {
                                float spread = 0.5711987f / (float)(maxFishronSets + 1);
                                Vector2 offset = ((NPC.ai[2] == 0f) ? (Utils.RotatedBy(Vector2.UnitY, (double)(spread * (float)i), default(Vector2)) * -450f * (float)j) : (Utils.RotatedBy(Vector2.UnitX, (double)(spread * (float)i), default(Vector2)) * 475f * (float)j));
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantFishron>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, offset.X, offset.Y);
                            }
                        }
                        for (int k = -max; k <= max; k++)
                        {
                            if (Math.Abs(k) == max)
                            {
                                float spread2 = (float)Math.PI / 36f / (float)(maxFishronSets + 1);
                                Vector2 offset2 = ((NPC.ai[2] == 0f) ? (Utils.RotatedBy(Vector2.UnitX, (double)(spread2 * (float)k), default(Vector2)) * -450f * (float)j) : (Utils.RotatedBy(Vector2.UnitY, (double)(spread2 * (float)k), default(Vector2)) * 615f * (float)j));
                                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantFishron>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, offset2.X, offset2.Y);
                            }
                        }
                    }
                }
                for (int l = 0; l < 30; l++)
                {
                    int d = Dust.NewDust(((Entity)NPC).position, ((Entity)NPC).width, ((Entity)NPC).height, DustID.IceTorch, 0f, 0f, 0, default(Color), 3f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Dust obj = Main.dust[d];
                    obj.velocity *= 12f;
                }
            }
            if ((NPC.ai[1] += 1f) > (float)(FargoSoulsWorld.MasochistModeReal ? 60 : 120))
            {
                int[] obj2 = new int[14]
                {
                13, 19, 20, 21, 0, 31, 31, 31, 33, 35,
                39, 41, 42, 44
                };
                obj2[4] = (FargoSoulsWorld.MasochistModeReal ? 44 : 26);
                ChooseNextAttack(obj2);
            }
        }

        private void PrepareTrueEyeDiveP2()
        {
            if (!AliveCheck(Player))
            {
                return;
            }
            Vector2 targetPos = ((Entity)Player).Center;
            targetPos.X += 400 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
            targetPos.Y += 400f;
            Movement(targetPos, 1.2f);
            if ((NPC.ai[1] += 1f) > 60f)
            {
                ((Entity)NPC).velocity.X = 30f * (float)((((Entity)NPC).position.X < ((Entity)Player).position.X) ? 1 : (-1));
                if (((Entity)NPC).velocity.Y > 0f)
                {
                    ((Entity)NPC).velocity.Y *= -1f;
                }
                ((Entity)NPC).velocity.Y *= 0.3f;
                NPC.ai[0] += 1f;
                NPC.ai[1] = 0f;
                NPC.netUpdate = true;
            }
        }

        private void PrepareNuke()
        {
            if (!AliveCheck(Player))
            {
                return;
            }
            Vector2 targetPos = ((Entity)Player).Center;
            targetPos.X += 400 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
            targetPos.Y -= 400f;
            Movement(targetPos, 1.2f, fastX: false);
            if ((NPC.ai[1] += 1f) > 60f)
            {
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float gravity = 0.2f;
                    float time = (FargoSoulsWorld.MasochistModeReal ? 120f : 180f);
                    Vector2 distance = ((Entity)Player).Center - ((Entity)NPC).Center;
                    distance.X /= time;
                    distance.Y = distance.Y / time - 0.5f * gravity * time;
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, distance, ModContent.ProjectileType<MutantNuke>(), FargoSoulsWorld.MasochistModeReal ? YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f) : 0, 0f, Main.myPlayer, gravity, 0f);
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantFishronRitual>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 0f);
                }
                NPC.ai[0] += 1f;
                NPC.ai[1] = 0f;
                if (Math.Sign(((Entity)Player).Center.X - ((Entity)NPC).Center.X) == Math.Sign(((Entity)NPC).velocity.X))
                {
                    ((Entity)NPC).velocity.X *= -1f;
                }
                if (((Entity)NPC).velocity.Y < 0f)
                {
                    ((Entity)NPC).velocity.Y *= -1f;
                }
                NPC.velocity = Utils.SafeNormalize(NPC.velocity, Vector2.Zero);
                NPC nPC = NPC;
                ((Entity)nPC).velocity = ((Entity)nPC).velocity * 3f;
                NPC.netUpdate = true;
            }
        }

        private void Nuke()
        {
            if (!AliveCheck(Player))
            {
                return;
            }
            Vector2 target = ((((Entity)NPC).Bottom.Y < ((Entity)Player).Top.Y) ? (((Entity)Player).Center + 300f * Vector2.UnitX * (float)Math.Sign(((Entity)NPC).Center.X - ((Entity)Player).Center.X)) : (((Entity)NPC).Center + 30f * Utils.RotatedBy(((Entity)NPC).DirectionFrom(((Entity)Player).Center), (double)(MathHelper.ToRadians(60f) * (float)Math.Sign(((Entity)Player).Center.X - ((Entity)NPC).Center.X)), default(Vector2))));
            Movement(target, 0.1f);
            if (NPC.velocity.LengthSquared() > 2f * 2f)
            {
                NPC.velocity = Vector2.Normalize(NPC.velocity) * 2f;
            }
            if (NPC.ai[1] > (float)(FargoSoulsWorld.MasochistModeReal ? 120 : 180))
            {
                if (!Main.dedServ && ((Entity)Main.LocalPlayer).active)
                {
                    Main.LocalPlayer.GetModPlayer<FargoSoulsPlayer>().Screenshake = 2;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 safeZone = ((Entity)NPC).Center;
                    safeZone.Y -= 100f;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 spawnPos = ((Entity)NPC).Center + Utils.NextVector2Circular(Main.rand, 1200f, 1200f);
                        if (Vector2.Distance(safeZone, spawnPos) < 350f)
                        {
                            Vector2 directionOut = Utils.SafeNormalize(spawnPos - safeZone, Vector2.UnitX);
                            spawnPos = safeZone + directionOut * Main.rand.NextFloat(350f, 1200f);
                        }
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos, Vector2.Zero, ModContent.ProjectileType<MutantBomb>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 1.3333334f), 0f, Main.myPlayer, 0f, 0f);
                    }
                }
            }
            if ((NPC.ai[1] += 1f) > 360f + 360f * endTimeVariance)
            {
                int[] obj = new int[12]
                {
                11, 13, 16, 19, 24, 0, 31, 35, 37, 39,
                41, 42
                };
                obj[5] = (FargoSoulsWorld.MasochistModeReal ? 26 : 29);
                ChooseNextAttack(obj);
            }
            if (!(NPC.ai[1] > 45f))
            {
                return;
            }
            for (int j = 0; j < 20; j++)
            {
                Vector2 offset = default(Vector2);
                offset.Y -= 100f;
                double angle = Main.rand.NextDouble() * 2.0 * Math.PI;
                offset.X += (float)(Math.Sin(angle) * 150.0);
                offset.Y += (float)(Math.Cos(angle) * 150.0);
                Dust dust = Main.dust[Dust.NewDust(((Entity)NPC).Center + offset - new Vector2(4f, 4f), 0, 0, DustID.RedTorch, 0f, 0f, 100, Color.White, 1.5f)];
                dust.velocity = ((Entity)NPC).velocity;
                if (Utils.NextBool(Main.rand, 3))
                {
                    dust.velocity += Vector2.Normalize(offset) * 5f;
                }
                dust.noGravity = true;
            }
        }

        private void PrepareSlimeRain()
        {
            if (AliveCheck(Player))
            {
                Vector2 targetPos = ((Entity)Player).Center;
                targetPos.X += 700 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
                targetPos.Y += 200f;
                Movement(targetPos, 2f);
                if ((NPC.ai[2] += 1f) > 30f || (FargoSoulsWorld.MasochistModeReal && ((Entity)NPC).Distance(targetPos) < 64f))
                {
                    NPC.ai[0] += 1f;
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.ai[3] = 0f;
                    NPC.netUpdate = true;
                }
            }
        }

        private void SlimeRain()
        {
            if (NPC.ai[3] == 0f)
            {
                NPC.ai[3] = 1f;
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSlimeRain>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 0f);
                }
            }
            if (NPC.ai[1] == 0f)
            {
                bool num = NPC.localAI[0] == 0f;
                NPC.localAI[0] = Main.rand.Next(5, 9) * 120;
                if (num)
                {
                    if (((Entity)Player).Center.X < ((Entity)NPC).Center.X && NPC.localAI[0] > 1200f)
                    {
                        NPC.localAI[0] -= 1200f;
                    }
                    else if (((Entity)Player).Center.X > ((Entity)NPC).Center.X && NPC.localAI[0] < 1200f)
                    {
                        NPC.localAI[0] += 1200f;
                    }
                }
                else if (((Entity)Player).Center.X < ((Entity)NPC).Center.X && NPC.localAI[0] < 1200f)
                {
                    NPC.localAI[0] += 1200f;
                }
                else if (((Entity)Player).Center.X > ((Entity)NPC).Center.X && NPC.localAI[0] > 1200f)
                {
                    NPC.localAI[0] -= 1200f;
                }
                NPC.localAI[0] += 60f;
                Vector2 basePos = ((Entity)NPC).Center;
                basePos.X -= 1200f;
                for (int i = -360; i <= 2760; i += 120)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient && i + 60 != (int)NPC.localAI[0])
                    {
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), basePos.X + (float)i + 60f, basePos.Y, 0f, 0f, ModContent.ProjectileType<MutantReticle>(), 0, 0f, Main.myPlayer, 0f, 0f);
                    }
                }
                if (FargoSoulsWorld.MasochistModeReal)
                {
                    NPC.ai[1] += 20f;
                    NPC.ai[2] += 20f;
                }
            }
            if (NPC.ai[1] > 120f && NPC.ai[1] % 5f == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item34, (Vector2?)((Entity)Player).Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 basePos2 = ((Entity)NPC).Center;
                    basePos2.X -= 1200f;
                    float yOffset = -1300f;
                    for (int j = -360; j <= 2760; j += 75)
                    {
                        float xOffset = j + Main.rand.Next(75);
                        if (!(Math.Abs(xOffset - NPC.localAI[0]) < 110f))
                        {
                            Vector2 spawnPos = basePos2;
                            spawnPos.X += xOffset;
                            Vector2 velocity = Vector2.UnitY * Utils.NextFloat(Main.rand, 15f, 20f);
                            Slime(spawnPos, yOffset, velocity);
                        }
                    }
                    Slime(basePos2 + Vector2.UnitX * (NPC.localAI[0] + 110f), yOffset, Vector2.UnitY * 20f);
                    Slime(basePos2 + Vector2.UnitX * (NPC.localAI[0] - 110f), yOffset, Vector2.UnitY * 20f);
                }
            }
            if ((NPC.ai[1] += 1f) > 180f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                NPC.ai[1] = 0f;
            }
            if (FargoSoulsWorld.MasochistModeReal && NPC.ai[1] == 120f && NPC.ai[2] < 480f && Utils.NextBool(Main.rand, 3))
            {
                NPC.ai[2] = 480f;
            }
            ((Entity)NPC).velocity = Vector2.Zero;
            if (!FargoSoulsWorld.MasochistModeReal)
            {
                return;
            }
            if (NPC.ai[2] == 480f)
            {
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
            }
            int endTime = 540;
            if (FargoSoulsWorld.MasochistModeReal)
            {
                endTime += 240 + (int)(120f * endTimeVariance) - 50;
            }
            if ((NPC.ai[2] += 1f) > (float)endTime)
            {
                int[] obj = new int[12]
                {
                11, 16, 19, 20, 0, 31, 33, 37, 39, 41,
                42, 45
                };
                obj[4] = (FargoSoulsWorld.MasochistModeReal ? 26 : 29);
                ChooseNextAttack(obj);
            }
            if (NPC.ai[2] > 510f)
            {
                if (NPC.ai[2] % 3f == 1f && NPC.ai[2] < (float)(endTime - 80))
                {
                    Vector2 range = ((Entity)Player).Center + new Vector2(((float)Main.rand.Next(2) - 0.5f) * 2200f, 0f);
                    Vector2 vel = Utils.SafeNormalize(((Entity)Player).Center - range, Vector2.Zero) * (15f + ((float)Main.rand.Next(2) - 0.5f) * 4f);
                    Projectile obj2 = Projectile.NewProjectileDirect(((Entity)NPC).GetSource_FromAI((string)null), range + new Vector2(0f, ((float)Main.rand.Next(2) - 0.5f) * 12f), vel, ModContent.ProjectileType<BigSting22>(), 50, 0f, 0, 256f, 0f);
                    obj2.hostile = true;
                    obj2.friendly = false;
                }
                if (NPC.ai[1] > 170f)
                {
                    NPC.ai[1] -= 30f;
                }
                if (NPC.localAI[1] == 0f)
                {
                    float safespotX = ((Entity)NPC).Center.X - 1200f + NPC.localAI[0];
                    NPC.localAI[1] = Math.Sign(((Entity)NPC).Center.X - safespotX);
                }
                NPC.localAI[0] += 4.1666665f * NPC.localAI[1];
            }
            void Slime(Vector2 pos, float off, Vector2 val)
            {
                int flip = ((!FargoSoulsWorld.MasochistModeReal || !(NPC.ai[2] < 360f) || !Utils.NextBool(Main.rand)) ? 1 : (-1));
                Vector2 spawnPos2 = pos + off * Vector2.UnitY * (float)flip;
                float ai0 = ((YharimEXUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<MutantRitual>()) == null) ? 0f : ((Entity)NPC).Distance(((Entity)Main.projectile[ritualProj]).Center));
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos2, val * (float)flip, ModContent.ProjectileType<MutantSlimeBall>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, ai0, 0f);
            }
        }

        private void PrepareFishron2()
        {
            if (AliveCheck(Player))
            {
                Vector2 targetPos = ((Entity)Player).Center;
                targetPos.X += 400 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
                targetPos.Y -= 400f;
                Movement(targetPos, 0.9f);
                if ((NPC.ai[1] += 1f) > 60f || (FargoSoulsWorld.MasochistModeReal && ((Entity)NPC).Distance(targetPos) < 32f))
                {
                    ((Entity)NPC).velocity.X = 35f * (float)((((Entity)NPC).position.X < ((Entity)Player).position.X) ? 1 : (-1));
                    ((Entity)NPC).velocity.Y = 10f;
                    NPC.ai[0] += 1f;
                    NPC.ai[1] = 0f;
                    NPC.netUpdate = true;
                }
            }
        }

        private void PrepareOkuuSpheresP2()
        {
            if (AliveCheck(Player))
            {
                Vector2 targetPos = ((Entity)Player).Center + ((Entity)Player).DirectionTo(((Entity)NPC).Center) * 450f;
                if ((NPC.ai[1] += 1f) < 180f && ((Entity)NPC).Distance(targetPos) > 50f)
                {
                    Movement(targetPos, 0.8f);
                    return;
                }
                NPC.netUpdate = true;
                NPC.ai[0] += 1f;
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                NPC.ai[3] = 0f;
            }
        }

        private void OkuuSpheresP2()
        {
            ((Entity)NPC).velocity = Vector2.Zero;
            int endTime = 420 + (int)(360f * (endTimeVariance - 0.33f));
            if ((NPC.ai[1] += 1f) > 10f && NPC.ai[3] > 60f && NPC.ai[3] < (float)(endTime - 60))
            {
                NPC.ai[1] = 0f;
                float rotation = MathHelper.ToRadians(60f) * (NPC.ai[3] - 45f) / 240f * NPC.ai[2];
                int max = (FargoSoulsWorld.MasochistModeReal ? 10 : 9);
                float speed = (FargoSoulsWorld.MasochistModeReal ? 11f : 10f);
                SpawnSphereRing(max, speed, YharimEXUtil.ScaledProjectileDamage(NPC.damage), -1f, rotation);
                SpawnSphereRing(max, speed, YharimEXUtil.ScaledProjectileDamage(NPC.damage), 1f, rotation);
            }
            if (NPC.ai[2] == 0f)
            {
                NPC.ai[2] = ((!Utils.NextBool(Main.rand)) ? 1 : (-1));
                NPC.ai[3] = Utils.NextFloat(Main.rand, (float)Math.PI * 2f);
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, -2f);
                }
            }
            if ((NPC.ai[3] += 1f) > (float)endTime)
            {
                int[] obj = new int[7] { 13, 19, 20, 0, 0, 41, 44 };
                obj[3] = (FargoSoulsWorld.MasochistModeReal ? 13 : 26);
                obj[4] = (FargoSoulsWorld.MasochistModeReal ? 44 : 33);
                ChooseNextAttack(obj);
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(((Entity)NPC).position, ((Entity)NPC).width, ((Entity)NPC).height, DustID.RedTorch, 0f, 0f, 0, default(Color), 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Dust obj2 = Main.dust[d];
                obj2.velocity *= 4f;
            }
        }

        private void SpearTossDirectP2()
        {
            if (!AliveCheck(Player))
            {
                return;
            }
            if (NPC.ai[1] == 0f)
            {
                NPC.localAI[0] = MathHelper.WrapAngle(Utils.ToRotation(((Entity)NPC).Center - ((Entity)Player).Center));
                if (FargoSoulsWorld.EternityMode)
                {
                    NPC.localAI[1] = Main.rand.Next(FargoSoulsWorld.MasochistModeReal ? 3 : 5, 9);
                }
                else
                {
                    NPC.localAI[1] = 5f;
                }
                if (FargoSoulsWorld.MasochistModeReal)
                {
                    NPC.localAI[1] += 3f;
                }
                NPC.localAI[2] = ((!Utils.NextBool(Main.rand)) ? 1 : (-1));
                NPC.netUpdate = true;
            }
            Vector2 targetPos = ((Entity)Player).Center + 500f * Utils.RotatedBy(Vector2.UnitX, (double)((float)Math.PI / 150f * NPC.ai[3] * NPC.localAI[2] + NPC.localAI[0]), default(Vector2));
            if (((Entity)NPC).Distance(targetPos) > 25f)
            {
                Movement(targetPos, 0.6f);
            }
            NPC.ai[3] += 1f;
            if ((NPC.ai[1] += 1f) > 180f)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 150f;
                bool shouldAttack = true;
                if ((NPC.ai[2] += 1f) > NPC.localAI[1])
                {
                    int[] obj = new int[11]
                    {
                    11, 16, 19, 20, 0, 31, 33, 35, 42, 44,
                    45
                    };
                    obj[4] = (FargoSoulsWorld.MasochistModeReal ? 44 : 26);
                    ChooseNextAttack(obj);
                    shouldAttack = false;
                }
                if (shouldAttack || FargoSoulsWorld.MasochistModeReal)
                {
                    Attack();
                }
            }
            else if (FargoSoulsWorld.MasochistModeReal && NPC.ai[1] == 160f)
            {
                Attack();
            }
            else if (FargoSoulsWorld.MasochistModeReal && NPC.ai[1] == 165f)
            {
                Attack();
            }
            else if (FargoSoulsWorld.MasochistModeReal && NPC.ai[1] == 170f)
            {
                Attack();
            }
            else if (FargoSoulsWorld.MasochistModeReal && NPC.ai[1] == 175f)
            {
                Attack();
            }
            else if (NPC.ai[1] == 151f)
            {
                if (NPC.ai[2] > 0f && (NPC.ai[2] < NPC.localAI[1] || FargoSoulsWorld.MasochistModeReal) && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearAim>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, 1f);
                }
            }
            else if (NPC.ai[1] == 1f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Zero, ModContent.ProjectileType<MutantSpearAim>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)((Entity)NPC).whoAmI, -1f);
            }
            void Attack()
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vel = ((Entity)NPC).DirectionTo(((Entity)Player).Center) * 30f;
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.Normalize(vel), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 0.8f), 0f, Main.myPlayer, 0f, 0f);
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, -Vector2.Normalize(vel), ModContent.ProjectileType<MutantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 0.8f), 0f, Main.myPlayer, 0f, 0f);
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, vel, ModContent.ProjectileType<MutantSpearThrown>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)NPC.target, 0f);
                }
            }
        }

        private void PrepareTwinRangsAndCrystals()
        {
            if (AliveCheck(Player))
            {
                Vector2 targetPos = ((Entity)Player).Center;
                targetPos.X += 500 * ((!(((Entity)NPC).Center.X < targetPos.X)) ? 1 : (-1));
                if (((Entity)NPC).Distance(targetPos) > 50f)
                {
                    Movement(targetPos, 0.8f);
                }
                if ((NPC.ai[1] += 1f) > 45f)
                {
                    NPC.netUpdate = true;
                    NPC.ai[0] += 1f;
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.ai[3] = 0f;
                }
            }
        }

        private void TwinRangsAndCrystals()
        {
            ((Entity)NPC).velocity = Vector2.Zero;
            if (NPC.ai[3] == 0f)
            {
                NPC.localAI[0] = Utils.ToRotation(((Entity)NPC).DirectionFrom(((Entity)Player).Center));
                if (!FargoSoulsWorld.MasochistModeReal && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center + Utils.RotatedBy(Vector2.UnitX, Math.PI / 2.0 * (double)i, default(Vector2)) * 525f, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 1f, 0f);
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center + Utils.RotatedBy(Vector2.UnitX, Math.PI / 2.0 * (double)i + Math.PI / 4.0, default(Vector2)) * 350f, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 2f, 0f);
                    }
                }
            }
            int ringDelay = (FargoSoulsWorld.MasochistModeReal ? 12 : 15);
            int ringMax = (FargoSoulsWorld.MasochistModeReal ? 5 : 4);
            if (NPC.ai[3] % (float)ringDelay == 0f && NPC.ai[3] < (float)(ringDelay * ringMax) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float rotationOffset = (float)Math.PI * 2f / (float)ringMax * NPC.ai[3] / (float)ringDelay + NPC.localAI[0];
                int baseDelay = 60;
                float flyDelay = 120f + NPC.ai[3] / (float)ringDelay * (float)(FargoSoulsWorld.MasochistModeReal ? 40 : 50);
                int p = Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, 300f / (float)baseDelay * Utils.RotatedBy(Vector2.UnitX, (double)rotationOffset, default(Vector2)), ModContent.ProjectileType<MutantMark2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)baseDelay, (float)baseDelay + flyDelay);
                if (p != 1000)
                {
                    float rotation = (float)Math.PI * 2f / 5f;
                    for (int j = 0; j < 5; j++)
                    {
                        float myRot = rotation * (float)j + rotationOffset;
                        Vector2 spawnPos = ((Entity)NPC).Center + Utils.RotatedBy(new Vector2(125f, 0f), (double)myRot, default(Vector2));
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos, Vector2.Zero, ModContent.ProjectileType<MutantCrystalLeaf>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, (float)Main.projectile[p].identity, myRot);
                    }
                }
            }
            if (NPC.ai[3] > 45f && (NPC.ai[1] -= 1f) < 0f)
            {
                NPC.netUpdate = true;
                NPC.ai[1] = 20f;
                NPC.ai[2] = ((!(NPC.ai[2] > 0f)) ? 1 : (-1));
                SoundEngine.PlaySound(SoundID.Item92, (Vector2?)((Entity)NPC).Center);
                if (Main.netMode != NetmodeID.MultiplayerClient && NPC.ai[3] < 330f)
                {
                    float retiSpeed = 10.995575f;
                    float spazSpeed = 12.217305f;
                    float retiAcc = retiSpeed * retiSpeed / 525f * NPC.ai[2];
                    float spazAcc = spazSpeed * spazSpeed / 350f * (0f - NPC.ai[2]);
                    float rotationOffset2 = (FargoSoulsWorld.MasochistModeReal ? ((float)Math.PI / 4f) : 0f);
                    for (int k = 0; k < 4; k++)
                    {
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(Vector2.UnitX, Math.PI / 2.0 * (double)k + (double)rotationOffset2, default(Vector2)) * retiSpeed, ModContent.ProjectileType<MutantRetirang>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, retiAcc, 300f);
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(Vector2.UnitX, Math.PI / 2.0 * (double)k + Math.PI / 4.0 + (double)rotationOffset2, default(Vector2)) * spazSpeed, ModContent.ProjectileType<MutantSpazmarang>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, spazAcc, 180f);
                    }
                }
            }
            if (NPC.ai[3] > 350f && NPC.ai[3] < 450f)
            {
                Vector2 v = ((Entity)NPC).DirectionTo(((Entity)Player).Center);
                for (int l = -1; l <= 1; l += 2)
                {
                    Projectile.NewProjectile((IEntitySource)null, ((Entity)NPC).Center, 21f * Utils.RotatedBy(v, (double)((float)l * (0.734f - (NPC.ai[3] - 360f) / 150f) + Utils.NextFloat(Main.rand, -0.05f, 0.05f)), default(Vector2)), 259, 66, 0f, 0, 0f, 0f);
                }
            }
            if ((NPC.ai[3] += 1f) > 450f)
            {
                ChooseNextAttack(11, 13, 16, 21, 24, 26, 29, 31, 33, 35, 39, 41, 44, 45);
            }
        }

        private void EmpressSwordWave()
        {
            if (!AliveCheck(Player))
            {
                return;
            }
            if (!FargoSoulsWorld.EternityMode)
            {
                NPC.ai[0] += 1f;
                return;
            }
            ((Entity)NPC).velocity = Vector2.Zero;
            int attackThreshold = (FargoSoulsWorld.MasochistModeReal ? 48 : 60);
            int timesToAttack = (FargoSoulsWorld.MasochistModeReal ? (3 + (int)(endTimeVariance * 5f)) : 4);
            int startup = 90;
            if (NPC.ai[1] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                NPC.ai[3] = Utils.NextFloat(Main.rand, (float)Math.PI * 2f);
            }
            if (NPC.ai[1] >= (float)startup && NPC.ai[1] < (float)(startup + attackThreshold * timesToAttack) && (NPC.ai[2] -= 1f) < 0f)
            {
                NPC.ai[2] = attackThreshold - 15;
                float gap = 220f;
                SoundEngine.PlaySound(SoundID.Item163, (Vector2?)((Entity)Player).Center);
                float randomrot = Utils.NextFloat(Main.rand, 6.283f);
                Vector2 RandomOffset2 = Utils.NextVector2Circular(Main.rand, 75f, 75f);
                Vector2 RandomOffset3 = Utils.NextVector2Circular(Main.rand, 120f, 120f);
                for (int i = 0; i < 3; i++)
                {
                    float rot = randomrot + (float)i * ((float)Math.PI * 2f) / 3f;
                    for (int j = -12; j <= 12; j++)
                    {
                        Vector2 targetpos = ((Entity)NPC).Center + RandomOffset3 + Utils.ToRotationVector2(rot) * 1200f + Utils.ToRotationVector2(rot + 1.5707f) * gap * (float)j;
                        Sword(targetpos + RandomOffset2, rot + 3.1416f, Utils.NextFloat(Main.rand, 0f, 1f), -RandomOffset2 / 60f, shouldUpdate: true);
                    }
                }
                NPC.netUpdate = true;
            }
            int swordSwarmTime = startup + attackThreshold * timesToAttack + 40;
            if (NPC.ai[1] == (float)swordSwarmTime)
            {
                MegaSwordSwarm(((Entity)Player).Center);
                NPC.localAI[0] = ((Entity)Player).Center.X;
                NPC.localAI[1] = ((Entity)Player).Center.Y;
            }
            if (FargoSoulsWorld.MasochistModeReal && NPC.ai[1] == (float)(swordSwarmTime + 30))
            {
                for (int k = -1; k <= 1; k += 2)
                {
                    MegaSwordSwarm(new Vector2(NPC.localAI[0], NPC.localAI[1]) + (float)(600 * k) * Utils.ToRotationVector2(NPC.ai[3]));
                }
            }
            if ((NPC.ai[1] += 1f) > (float)(swordSwarmTime + (FargoSoulsWorld.MasochistModeReal ? 60 : 30)))
            {
                int[] obj = new int[12]
                {
                11, 13, 16, 21, 0, 29, 31, 35, 37, 39,
                41, 45
                };
                obj[4] = (FargoSoulsWorld.MasochistModeReal ? 26 : 24);
                ChooseNextAttack(obj);
            }
            void MegaSwordSwarm(Vector2 target)
            {
                SoundEngine.PlaySound(SoundID.Item164, (Vector2?)((Entity)Player).Center);
                float safeAngle = NPC.ai[3];
                float safeRange = MathHelper.ToRadians(10f);
                int max = 60;
                for (int l = 0; l < max; l++)
                {
                    float rotationOffset = Utils.NextFloat(Main.rand, safeRange, (float)Math.PI - safeRange);
                    Vector2 offset = Utils.NextFloat(Main.rand, 600f, 2400f) * Utils.ToRotationVector2(safeAngle + rotationOffset);
                    if (Utils.NextBool(Main.rand))
                    {
                        offset *= -1f;
                    }
                    Vector2 spawnPos = target + offset;
                    Vector2 vel = (target - spawnPos) / 60f;
                    Sword(spawnPos, Utils.ToRotation(vel), (float)l / (float)max, -vel * 0.75f, shouldUpdate: false);
                }
            }
            void Sword(Vector2 pos, float ai0, float ai1, Vector2 vel, bool shouldUpdate)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(((Entity)NPC).GetSource_FromThis((string)null), pos - vel * 60f, vel, 919, YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, ai0, ai1).extraUpdates = (shouldUpdate ? 1 : 0);
                }
            }
        }

        private void P2NextAttackPause()
        {
            if (AliveCheck(Player))
            {
                EModeSpecialEffects();
                Vector2 targetPos = ((Entity)Player).Center + ((Entity)NPC).DirectionFrom(((Entity)Player).Center) * 400f;
                Movement(targetPos, 0.3f);
                if (((Entity)NPC).Distance(targetPos) > 200f)
                {
                    Movement(targetPos, 0.3f);
                }
                if ((NPC.ai[1] += 1f) > 60f || (((Entity)NPC).Distance(targetPos) < 200f && NPC.ai[1] > (float)((NPC.localAI[3] >= 3f) ? 15 : 30)))
                {
                    NPC nPC = NPC;
                    ((Entity)nPC).velocity = ((Entity)nPC).velocity * (FargoSoulsWorld.MasochistModeReal ? 0.25f : 0.75f);
                    NPC.ai[0] = NPC.ai[2];
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.netUpdate = true;
                }
            }
        }

        private bool Phase3Transition()
        {
            bool retval = true;
            NPC.localAI[3] = 3f;
            EModeSpecialEffects();
            if (NPC.buffType[0] != 0)
            {
                NPC.DelBuff(0);
            }
            if (NPC.ai[1] == 0f)
            {
                NPC.life = NPC.lifeMax;
                DramaticTransition(fightIsOver: true);
            }
            if (NPC.ai[1] < 60f && !Main.dedServ && ((Entity)Main.LocalPlayer).active)
            {
                Main.LocalPlayer.GetModPlayer<FargoSoulsPlayer>().Screenshake = 2;
            }
            if (NPC.ai[1] == 360f)
            {
                SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
            }
            if ((NPC.ai[1] += 1f) > 480f)
            {
                retval = false;
                if (!AliveCheck(Player))
                {
                    return retval;
                }
                Vector2 targetPos = ((Entity)Player).Center;
                targetPos.Y -= 300f;
                Movement(targetPos, 1f, fastX: true, obeySpeedCap: false);
                if (((Entity)NPC).Distance(targetPos) < 50f || NPC.ai[1] > 720f)
                {
                    NPC.netUpdate = true;
                    ((Entity)NPC).velocity = Vector2.Zero;
                    NPC.localAI[0] = 0f;
                    NPC.ai[0] -= 1f;
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = Utils.ToRotation(((Entity)NPC).DirectionFrom(((Entity)Player).Center));
                    NPC.ai[3] = (float)Math.PI / 20f;
                    SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                    if (((Entity)Player).Center.X < ((Entity)NPC).Center.X)
                    {
                        NPC.ai[3] *= -1f;
                    }
                }
            }
            else
            {
                NPC nPC = NPC;
                ((Entity)nPC).velocity = ((Entity)nPC).velocity * 0.9f;
                if (((Entity)Main.LocalPlayer).active && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost && ((Entity)NPC).Distance(((Entity)Main.LocalPlayer).Center) < 3000f)
                {
                    Main.LocalPlayer.controlUseItem = false;
                    Main.LocalPlayer.controlUseTile = false;
                    Main.LocalPlayer.GetModPlayer<FargoSoulsPlayer>().NoUsingItems = true;
                }
                if ((NPC.localAI[0] -= 1f) < 0f)
                {
                    NPC.localAI[0] = Main.rand.Next(15);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 spawnPos = ((Entity)NPC).position + new Vector2((float)Main.rand.Next(((Entity)NPC).width), (float)Main.rand.Next(((Entity)NPC).height));
                        int type = ModContent.ProjectileType<PhantasmalBlast>();
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer, 0f, 0f);
                    }
                }
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(((Entity)NPC).position, ((Entity)NPC).width, ((Entity)NPC).height, DustID.RedTorch, 0f, 0f, 0, default(Color), 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Dust obj = Main.dust[d];
                obj.velocity *= 4f;
            }
            return retval;
        }

        private void VoidRaysP3()
        {
            if ((NPC.ai[1] -= 1f) < 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float speed = ((FargoSoulsWorld.MasochistModeReal && NPC.localAI[0] <= 40f) ? 4f : 2f);
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, speed * Utils.RotatedBy(Vector2.UnitX, (double)NPC.ai[2], default(Vector2)), ModContent.ProjectileType<MutantMark1>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                }
                NPC.ai[1] = 1f;
                NPC.ai[2] += NPC.ai[3];
                if (NPC.localAI[0] < 30f)
                {
                    EModeSpecialEffects();
                    TryMasoP3Theme();
                }
                if (NPC.localAI[0]++ == 40f || NPC.localAI[0] == 80f || NPC.localAI[0] == 120f)
                {
                    NPC.netUpdate = true;
                    NPC.ai[2] -= NPC.ai[3] / (float)(FargoSoulsWorld.MasochistModeReal ? 3 : 2);
                }
                else if (NPC.localAI[0] >= (float)(FargoSoulsWorld.MasochistModeReal ? 160 : 120))
                {
                    NPC.netUpdate = true;
                    NPC.ai[0] -= 1f;
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.ai[3] = 0f;
                    NPC.localAI[0] = 0f;
                }
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(((Entity)NPC).position, ((Entity)NPC).width, ((Entity)NPC).height, DustID.RedTorch, 0f, 0f, 0, default(Color), 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Dust obj = Main.dust[d];
                obj.velocity *= 4f;
            }
            ((Entity)NPC).velocity = Vector2.Zero;
        }

        private void OkuuSpheresP3()
        {
            if (NPC.ai[2] == 0f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                NPC.ai[2] = ((!Utils.NextBool(Main.rand)) ? 1 : (-1));
                NPC.ai[3] = Utils.NextFloat(Main.rand, (float)Math.PI * 2f);
            }
            int endTime = 480;
            if (FargoSoulsWorld.MasochistModeReal)
            {
                endTime += 360;
            }
            if ((NPC.ai[1] += 1f) > 10f && NPC.ai[3] > 60f && NPC.ai[3] < (float)(endTime - 120))
            {
                NPC.ai[1] = 0f;
                float rotation = MathHelper.ToRadians(45f) * (NPC.ai[3] - 60f) / 240f * NPC.ai[2];
                int max = (FargoSoulsWorld.MasochistModeReal ? 11 : 10);
                float speed = (FargoSoulsWorld.MasochistModeReal ? 11f : 10f);
                SpawnSphereRing(max, speed, YharimEXUtil.ScaledProjectileDamage(NPC.damage), -0.75f, rotation);
                SpawnSphereRing(max, speed, YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0.75f, rotation);
            }
            if (NPC.ai[3] < 30f)
            {
                EModeSpecialEffects();
                TryMasoP3Theme();
            }
            if ((NPC.ai[3] += 1f) > (float)endTime)
            {
                NPC.netUpdate = true;
                NPC.ai[0] -= 1f;
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                NPC.ai[3] = 0f;
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(((Entity)NPC).position, ((Entity)NPC).width, ((Entity)NPC).height, DustID.RedTorch, 0f, 0f, 0, default(Color), 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Dust obj = Main.dust[d];
                obj.velocity *= 4f;
            }
            ((Entity)NPC).velocity = Vector2.Zero;
        }

        private void BoundaryBulletHellP3()
        {
            if (NPC.localAI[0] == 0f)
            {
                if (!AliveCheck(Player))
                {
                    return;
                }
                NPC.localAI[0] = Math.Sign(((Entity)NPC).Center.X - ((Entity)Player).Center.X);
            }
            if ((NPC.ai[1] += 1f) > 3f)
            {
                SoundEngine.PlaySound(SoundID.Item12, (Vector2?)((Entity)NPC).Center);
                NPC.ai[1] = 0f;
                NPC.ai[2] += 0.0014959965f * NPC.ai[3] * NPC.localAI[0] * (FargoSoulsWorld.MasochistModeReal ? 2f : 1f);
                if (NPC.ai[2] > (float)Math.PI)
                {
                    NPC.ai[2] -= (float)Math.PI * 2f;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int max = (FargoSoulsWorld.MasochistModeReal ? 10 : 8);
                    for (int i = 0; i < max; i++)
                    {
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(new Vector2(0f, -6f), (double)(NPC.ai[2] + (float)Math.PI * 2f / (float)max * (float)i), default(Vector2)), ModContent.ProjectileType<global::FargowiltasSouls.Projectiles.MutantBoss.MutantEye>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage), 0f, Main.myPlayer, 0f, 0f);
                    }
                }
            }
            if (NPC.ai[3] < 30f)
            {
                EModeSpecialEffects();
                TryMasoP3Theme();
            }
            int endTime = 360;
            if (FargoSoulsWorld.MasochistModeReal)
            {
                endTime += 360;
            }
            if ((NPC.ai[3] += 1f) > (float)endTime)
            {
                NPC.ai[0] -= 1f;
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                NPC.ai[3] = 0f;
                NPC.localAI[0] = 0f;
                NPC.netUpdate = true;
            }
            for (int j = 0; j < 5; j++)
            {
                int d = Dust.NewDust(((Entity)NPC).position, ((Entity)NPC).width, ((Entity)NPC).height, DustID.RedTorch, 0f, 0f, 0, default(Color), 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Dust obj = Main.dust[d];
                obj.velocity *= 4f;
            }
            ((Entity)NPC).velocity = Vector2.Zero;
        }

        private void FinalSpark()
        {
            if (NPC.localAI[2] > 30f)
            {
                NPC.localAI[2] += 1f;
                if (NPC.localAI[2] > 120f)
                {
                    AliveCheck(Player, forceDespawn: true);
                }
                return;
            }
            if ((NPC.localAI[0] -= 1f) < 0f)
            {
                NPC.localAI[0] = Main.rand.Next(30);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPos = ((Entity)NPC).position + new Vector2((float)Main.rand.Next(((Entity)NPC).width), (float)Main.rand.Next(((Entity)NPC).height));
                    int type = ModContent.ProjectileType<PhantasmalBlast>();
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer, 0f, 0f);
                }
            }
            int ringTime = ((FargoSoulsWorld.MasochistModeReal && NPC.ai[2] >= 330f) ? 100 : 120);
            if ((NPC.ai[1] += 1f) > (float)ringTime)
            {
                NPC.ai[1] = 0f;
                EModeSpecialEffects();
                TryMasoP3Theme();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int max = 10;
                    int damage = YharimEXUtil.ScaledProjectileDamage(NPC.damage);
                    SpawnSphereRing(max, 6f, damage, 0.5f);
                    SpawnSphereRing(max, 6f, damage, -0.5f);
                }
            }
            if (NPC.ai[2] == 0f)
            {
                if (!FargoSoulsWorld.MasochistModeReal)
                {
                    NPC.localAI[1] = 1f;
                }
            }
            else if (NPC.ai[2] == 330f)
            {
                if (NPC.localAI[1] == 0f)
                {
                    NPC.localAI[1] = 1f;
                    NPC.ai[2] -= 780f;
                    NPC.ai[3] -= MathHelper.ToRadians(20f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(Vector2.UnitX, (double)NPC.ai[3], default(Vector2)), ModContent.ProjectileType<MutantGiantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 0.5f), 0f, Main.myPlayer, 0f, (float)((Entity)NPC).whoAmI);
                    }
                    NPC.netUpdate = true;
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.Roar, (Vector2?)((Entity)NPC).Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            float offset = (float)i - 0.5f;
                            Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.ToRotationVector2(NPC.ai[3] + (float)Math.PI / 4f * offset), ModContent.ProjectileType<GlowLine>(), 0, 0f, Main.myPlayer, 13f, (float)((Entity)NPC).whoAmI);
                        }
                    }
                }
            }
            if (NPC.ai[2] < 420f)
            {
                if (NPC.localAI[1] == 0f || NPC.ai[2] > 330f)
                {
                    NPC.ai[3] = Utils.ToRotation(((Entity)NPC).DirectionFrom(((Entity)Player).Center));
                }
            }
            else
            {
                if (!Main.dedServ)
                {
                    ((EffectManager<Filter>)(object)Terraria.Graphics.Effects.Filters.Scene)["FargowiltasSouls:FinalSpark"].IsActive();
                }
                if (NPC.ai[1] % 3f == 0f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, 24f * Utils.RotatedBy(Vector2.UnitX, (double)NPC.ai[3], default(Vector2)), ModContent.ProjectileType<MutantEyeWavy>(), 0, 0f, Main.myPlayer, Utils.NextFloat(Main.rand, 0.5f, 1.25f) * (float)((!Utils.NextBool(Main.rand)) ? 1 : (-1)), (float)Main.rand.Next(10, 60));
                }
            }
            int endTime = 1020;
            endTime += 180;
            if ((NPC.ai[2] += 1f) > (float)endTime)
            {
                NPC.netUpdate = true;
                NPC.ai[0] -= 1f;
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                YharimEXUtil.ClearAllProjectiles(2, ((Entity)NPC).whoAmI);
            }
            else if (NPC.ai[2] == 420f)
            {
                NPC.netUpdate = true;
                NPC.ai[3] += MathHelper.ToRadians(20f) * 1;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Utils.RotatedBy(Vector2.UnitX, (double)NPC.ai[3], default(Vector2)), ModContent.ProjectileType<MutantGiantDeathray2>(), YharimEXUtil.ScaledProjectileDamage(NPC.damage, 0.5f), 0f, Main.myPlayer, 0f, (float)((Entity)NPC).whoAmI);
                }
            }
            else if (NPC.ai[2] < 300f && NPC.localAI[1] != 0f)
            {
                float num1 = 0.99f;
                if (NPC.ai[2] >= 60f)
                {
                    num1 = 0.79f;
                }
                if (NPC.ai[2] >= 120f)
                {
                    num1 = 0.58f;
                }
                if (NPC.ai[2] >= 180f)
                {
                    num1 = 0.43f;
                }
                if (NPC.ai[2] >= 240f)
                {
                    num1 = 0.33f;
                }
                for (int j = 0; j < 9; j++)
                {
                    if (Utils.NextFloat(Main.rand) >= num1)
                    {
                        float f = Utils.NextFloat(Main.rand) * 6.283185f;
                        float num2 = Utils.NextFloat(Main.rand);
                        Dust obj = Dust.NewDustPerfect(((Entity)NPC).Center + Utils.ToRotationVector2(f) * (110f + 600f * num2), 60, (Vector2?)(Utils.ToRotationVector2(f - 3.141593f) * (14f + 8f * num2)), 0, default(Color), 1f);
                        obj.scale = 0.9f;
                        obj.fadeIn = 1.15f + num2 * 0.3f;
                        obj.noGravity = true;
                    }
                }
            }
            SpinLaser(FargoSoulsWorld.MasochistModeReal && NPC.ai[2] >= 420f);
            if (AliveCheck(Player))
            {
                NPC.localAI[2] = 0f;
            }
            else
            {
                NPC.localAI[2] += 1f;
            }
            ((Entity)NPC).velocity = Vector2.Zero;
            void SpinLaser(bool useMasoSpeed)
            {
                float newRotation = Utils.ToRotation(((Entity)NPC).DirectionTo(((Entity)Main.player[NPC.target]).Center));
                float difference = MathHelper.WrapAngle(newRotation - NPC.ai[3]);
                float rotationDirection = (float)Math.PI / 180f;
                rotationDirection *= (useMasoSpeed ? 0.525f : 1f);
                NPC.ai[3] += Math.Min(rotationDirection, Math.Abs(difference)) * (float)Math.Sign(difference);
                if (useMasoSpeed)
                {
                    NPC.ai[3] = Utils.AngleLerp(NPC.ai[3], newRotation, 0.015f);
                }
            }
        }

        private void DyingDramaticPause()
        {
            if (!AliveCheck(Player))
            {
                return;
            }
            NPC.ai[3] -= (float)Math.PI / 360f;
            ((Entity)NPC).velocity = Vector2.Zero;
            if ((NPC.ai[1] += 1f) > 120f)
            {
                NPC.netUpdate = true;
                NPC.ai[0] -= 1f;
                NPC.ai[1] = 0f;
                NPC.ai[3] = -(float)Math.PI / 2f;
                NPC.netUpdate = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, Vector2.UnitY * -1f, ModContent.ProjectileType<MutantGiantDeathray2>(), 0, 0f, Main.myPlayer, 1f, (float)((Entity)NPC).whoAmI);
                }
            }
            if ((NPC.localAI[0] -= 1f) < 0f)
            {
                NPC.localAI[0] = Main.rand.Next(15);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPos = ((Entity)NPC).position + new Vector2((float)Main.rand.Next(((Entity)NPC).width), (float)Main.rand.Next(((Entity)NPC).height));
                    int type = ModContent.ProjectileType<PhantasmalBlast>();
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer, 0f, 0f);
                }
            }
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(((Entity)NPC).position, ((Entity)NPC).width, ((Entity)NPC).height, DustID.RedTorch, 0f, 0f, 0, default(Color), 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Dust obj = Main.dust[d];
                obj.velocity *= 4f;
            }
        }

        private void DyingAnimationAndHandling()
        {
            ((Entity)NPC).velocity = Vector2.Zero;
            for (int i = 0; i < 5; i++)
            {
                int d = Dust.NewDust(((Entity)NPC).position, ((Entity)NPC).width, ((Entity)NPC).height, DustID.RedTorch, 0f, 0f, 0, default(Color), 2.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Dust obj = Main.dust[d];
                obj.velocity *= 12f;
            }
            if ((NPC.localAI[0] -= 1f) < 0f)
            {
                NPC.localAI[0] = Main.rand.Next(5);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPos = ((Entity)NPC).Center + Utils.NextVector2Circular(Main.rand, 240f, 240f);
                    int type = ModContent.ProjectileType<PhantasmalBlast>();
                    Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), spawnPos, Vector2.Zero, type, 0, 0f, Main.myPlayer, 0f, 0f);
                }
            }
            if ((NPC.ai[1] += 1f) % 3f == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(((Entity)NPC).GetSource_FromThis((string)null), ((Entity)NPC).Center, 24f * Utils.RotatedBy(Vector2.UnitX, (double)NPC.ai[3], default(Vector2)), ModContent.ProjectileType<MutantEyeWavy>(), 0, 0f, Main.myPlayer, Utils.NextFloat(Main.rand, 0.75f, 1.5f) * (float)((!Utils.NextBool(Main.rand)) ? 1 : (-1)), (float)Main.rand.Next(10, 90));
            }
            NPC nPC = NPC;
            if (++nPC.alpha <= 255)
            {
                return;
            }
            NPC.alpha = 255;
            NPC.life = 0;
            NPC.dontTakeDamage = false;
            NPC.checkDead();
            ModNPC modNPC = default(ModNPC);
            if (Main.netMode == 1 || !ModContent.TryFind<ModNPC>("Fargowiltas", "Mutant", ref modNPC) || NPC.AnyNPCs(modNPC.Type))
            {
                return;
            }
            int n = NPC.NewNPC(NPC.GetSource_FromAI((string)null), (int)NPC.Center.X, (int)NPC.Center.Y, modNPC.Type, 0, 0f, 0f, 0f, 0f, 255);
            if (n != 200)
            {
                Main.npc[n].homeless = true;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, (NetworkText)null, n, 0f, 0f, 0f, 0, 0, 0);
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (FargoSoulsWorld.EternityMode)
            {
                target.GetModPlayer<FargoSoulsPlayer>().MaxLifeReduction += 100;
                target.AddBuff(ModContent.BuffType<OceanicMaul>(), 5400, true, false);
                target.AddBuff(ModContent.BuffType<MutantFang>(), 180, true, false);
            }
            YharimBaseHitEffect(600);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 3; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.RedTorch, 0f, 0f, 0, default, 1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Dust obj = Main.dust[d];
                obj.velocity *= 3f;
            }
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers HitModifiers)
        {
            if (YharimEXWorld.YharimEXEnraged)
                HitModifiers.SourceDamage *= 0.8f;
        }

        public override bool CheckDead()
        {
            if (NPC.ai[0] == -7f)
            {
                return true;
            }
            NPC.life = 1;
            NPC.active = true;
            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.ai[0] > -1f)
            {
                NPC.ai[0] = ((!FargoSoulsWorld.EternityMode) ? (-6) : ((NPC.ai[0] >= 10f) ? (-1) : 10));
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                NPC.ai[3] = 0f;
                NPC.localAI[0] = 0f;
                NPC.localAI[1] = 0f;
                NPC.localAI[2] = 0f;
                NPC.dontTakeDamage = true;
                NPC.netUpdate = true;
                YharimEXUtil.ClearAllProjectiles(2, NPC.whoAmI, NPC.ai[0] < 0f);
            }
            return false;
        }

        public override void OnKill()
        {
            OnKill();
            NPC.SetEventFlagCleared(ref YharimEXWorld.YharimEXDowned, -1);
        }
        public override void ModifyNPCLoot(NPCLoot NPCLoot)
        {
            ModifyNPCLoot(NPCLoot);
            NPCLoot.Add(ItemDropRule.Common(ModContent.ItemType<Dirt>()));
        }
        public override void FindFrame(int frameHeight)
        {
            NPC nPC = NPC;
            if ((nPC.frameCounter += 1.0) > 4.0)
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= Main.npcFrameCount[NPC.type] * frameHeight)
                {
                    NPC.frame.Y = 0;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D value = TextureAssets.Npc[NPC.type].Value;
            Vector2 position = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);
            Rectangle rectangle = NPC.frame;
            Vector2 origin2 = Utils.Size(rectangle) / 2f;
            SpriteEffects effects = NPC.spriteDirection >= 0;
            Main.EntitySpriteDraw(value, position, (Rectangle?)rectangle, NPC.GetAlpha(drawColor), NPC.rotation, origin2, NPC.scale, effects, 0);
            if (ShouldDrawAura)
            {
                DrawAura(spriteBatch, position);
            }
            return false;
        }

        public void DrawAura(SpriteBatch spriteBatch, Vector2 position)
        {
            Color outerColor = Color.CadetBlue;
            outerColor.A = 0;
            spriteBatch.Draw(FargosTextureRegistry.SoftEdgeRing.Value, position, (Rectangle?)null, outerColor * 0.7f, 0f, Utils.Size(FargosTextureRegistry.SoftEdgeRing.Value) * 0.5f, 9.2f, (SpriteEffects)0, 0f);
        }
        */
    }
}