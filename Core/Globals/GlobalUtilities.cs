using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using CalamityMod.World;
using FargowiltasSouls.Core.Systems;

namespace YharimEX.Core.Globals
{
    public static class YharimEXGlobalUtilities
    {
        public static bool HostCheck => Main.netMode != NetmodeID.MultiplayerClient;
        public static bool HasRevengeanceMode => CalamityWorld.revenge;
        public static bool HasDeathMode => CalamityWorld.death;
        public static bool HasEternityMode => WorldSavingSystem.EternityMode;

        public static void SpawnBossNetcoded(Player player, int bossType, bool obeyLocalPlayerCheck = true)
        {
            if (player.whoAmI == Main.myPlayer || !obeyLocalPlayerCheck)
            {
                SoundEngine.PlaySound(SoundID.Roar, player.position);

                if (HostCheck)
                {
                    NPC.SpawnOnPlayer(player.whoAmI, bossType);
                }
                else
                {
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: bossType);
                }
            }
        }
        public static int NewNPCEasy(IEntitySource source, Vector2 spawnPos, int type, int start = 0, float ai0 = 0, float ai1 = 0, float ai2 = 0, float ai3 = 0, int target = 255, Vector2 velocity = default)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return Main.maxNPCs;

            int n = NPC.NewNPC(source, (int)spawnPos.X, (int)spawnPos.Y, type, start, ai0, ai1, ai2, ai3, target);
            if (n != Main.maxNPCs)
            {
                if (velocity != default)
                {
                    Main.npc[n].velocity = velocity;
                }

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
            }
            return n;
        }
    }
}