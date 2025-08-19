﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalamityMod.Items;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs.Yharon;
using Microsoft.VisualBasic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.ModLoader;
using YharimEX.Content.Items;
using YharimEX.Core.Systems;

namespace YharimEX.Content.NPCs.Town
{
    public class TheGodseeker : ModNPC
    {
        public override string Texture => "YharimEX/Assets/NPCs/Town/TheGodseeker";     
        public override string HeadTexture => "YharimEX/Assets/NPCs/Town/TheGodseeker_Head";

        internal bool spawned;
        private bool canSayDefeatQuote = true;
        private int defeatQuoteTimer = 900;

        private static int ShimmerHeadIndex;
        private static Profiles.StackedNPCProfile NPCProfile;
        private bool otherShop;
        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.aiStyle = 7;
            NPC.lifeMax = 77000;
            NPC.defense = 360;
            NPC.knockBackResist = 0.2f;
            NPC.friendly = true;
            NPC.damage = 125;
            NPC.width = 44;
            NPC.height = 62;
            NPC.lavaImmune = true;
            NPC.buffImmune[67] = true;
            NPC.HitSound = SoundID.NPCHit48;
            NPC.DeathSound = SoundID.NPCDeath62;
            AnimationType = 20;
            Main.npcFrameCount[Type] = 21;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 7;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 700;
            NPCID.Sets.AttackType[Type] = 2;
            NPCID.Sets.AttackTime[Type] = 50;
            NPCID.Sets.AttackAverageChance[Type] = 20;
            NPCID.Sets.HatOffsetY[Type] = 6;
            NPCID.Sets.ShimmerTownTransform[Type] = true;

            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                if (FargosNPC.CanSetCatchable())
                {
                    Main.npcCatchable[Type] = true;
                }
            }
        }
        public virtual void Load()
        {
            ShimmerHeadIndex = Mod.AddNPCHeadTexture(Type, Texture + "_Shimmer_Head");
            NPCHappiness npcHappiness = NPC.Happiness;
            npcHappiness = npcHappiness.SetBiomeAffection<SnowBiome>((AffectionLevel)50);
            npcHappiness = npcHappiness.SetBiomeAffection<DesertBiome>((AffectionLevel)(-50));
            npcHappiness = npcHappiness.SetBiomeAffection<JungleBiome>((AffectionLevel)100);
            npcHappiness = npcHappiness.SetNPCAffection(663, (AffectionLevel)100);
            npcHappiness = npcHappiness.SetNPCAffection(353, (AffectionLevel)50);
            npcHappiness = npcHappiness.SetNPCAffection(54, (AffectionLevel)50);
            npcHappiness = npcHappiness.SetNPCAffection(17, (AffectionLevel)(-50));
            npcHappiness.SetNPCAffection(38, (AffectionLevel)(-100));
            NPCProfile = new Profiles.StackedNPCProfile(new ITownNPCProfile[2]
            {
                new Profiles.DefaultNPCProfile(Texture, NPCHeadLoader.GetHeadSlot(HeadTexture), Texture + "_Party"),
                new Profiles.DefaultNPCProfile(Texture + "_Shimmer", ShimmerHeadIndex, Texture + "_Shimmer_Party")
            });
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange((IEnumerable<IBestiaryInfoElement>)new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Jungle,
            });
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            if (YharimEXWorldFlags.DownedYharimEX) return true;
            return false;
        }

        public override bool CanGoToStatue(bool toKingStatue) => true;

        public override void AI()
        {
            NPC.breath = 200;
            if (defeatQuoteTimer > 0)
                defeatQuoteTimer--;
            // else
            //    canSayDefeatQuote = false;

            if (!spawned)
            {
                spawned = true;
                if (YharimEXWorldFlags.DownedYharimEX)
                {
                    NPC.lifeMax = 77000;
                    NPC.life = NPC.lifeMax;
                    NPC.defense = 360;
                }
            }
            //AnimationType = NPC.IsShimmerVariant ? -1 : NPCID.Guide;
            //NPCID.Sets.CannotSitOnFurniture[NPC.type] = NPC.ShimmeredTownNPCs[NPC.type];
        }

        public override List<string> SetNPCNameList()
        {
            string[] names = ["Yharim"];

            return new List<string>(names);
        }

        public override string GetChat()
        {
            //Ill do this later
            switch (Main.rand.Next(22))
            {
                default:
                    return $"The world is in your hands now, {Main.LocalPlayer.name}";
            }
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = "Shop";
            if (!Main.LocalPlayer.HasItem(149))
                return;
            button = "Exchange books?";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shop)
        {
            if (!firstButton)
                return;
            if (Main.LocalPlayer.HasItem(149))
            {
                int index = Main.LocalPlayer.FindItem(149);
                IEntitySource sourceGiftOrReward = NPC.GetSource_GiftOrReward(null);
                Main.LocalPlayer.ConsumeItem(149);
                Main.LocalPlayer.QuickSpawnItem(sourceGiftOrReward, ModContent.ItemType<YharimsJournal>(), 1);
            }
            else
                shop = "Shop";
        }

        public override void AddShops()
        {
            var npcShop1 = new NPCShop(Type)
                .Add(ModContent.ItemType<YharonEgg>());
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 146;
            knockback = 4f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 4;
            randExtraCooldown = 10;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = 636;
            attackDelay = 1;
        }

        public override void TownNPCAttackProjSpeed(
            ref float multiplier,
            ref float gravityCorrection,
            ref float randomOffset)
        {
            multiplier = 7f;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<YharimsJournal>(), 1, 1, 1));
        }
    }
}
