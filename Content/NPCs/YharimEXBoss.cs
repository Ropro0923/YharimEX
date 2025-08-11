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
            /*    if (ModLoader.TryGetMod("FargowiltasMusic", ref musicMod))
                {
                    Music = MusicLoader.GetMusicSlot(musicMod, FargoSoulsWorld.MasochistModeReal ? "Assets/Music/rePrologue" : "Assets/Music/SteelRed");
                }
                else
                {
                    Music = MusicLoader.GetMusicSlot(((ModType)this).Mod, "Sounds/Music/P1");
                }
                SceneEffectPriority = (SceneEffectPriority)8;
            */
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
            /*
                if (!FargoSoulsWorld.MasochistModeReal)
                {
                    return false;
                }
                if (NPC.Distance(FargoSoulsUtil.ClosestPointInHitbox((Entity)(object)target, NPC.Center)) < 42f)
                {
                    return NPC.ai[0] > -1f;
                }
            */
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
            if (Main.netMode != 1)
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
        private void GoNextAI0()
        {
            NPC.ai[0] += 1f;
        }
    }
}