using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Core.ItemDropRules.Conditions;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Graphics.Capture;
using FargowiltasSouls;
using YharimEX.Core.Systems;

namespace YharimEX.Core.Globals
{
    public static class YharimEXGlobalUtilities
    {

        public static bool WorldIsExpertOrHarder() => Main.expertMode || (Main.GameModeInfo.IsJourneyMode && CreativePowerManager.Instance.GetPower<CreativePowers.DifficultySliderPower>().StrengthMultiplierToGiveNPCs >= 2);

        public static bool HostCheck => Main.netMode != NetmodeID.MultiplayerClient;

        public static void AddDebuffFixedDuration(Player player, int buffID, int intendedTime, bool quiet = true)
        {
            if (WorldIsExpertOrHarder() && BuffID.Sets.LongerExpertDebuff[buffID])
            {
                float debuffTimeMultiplier = Main.GameModeInfo.DebuffTimeMultiplier;
                if (Main.GameModeInfo.IsJourneyMode)
                {
                    if (Main.masterMode)
                        debuffTimeMultiplier = Main.RegisteredGameModes[2].DebuffTimeMultiplier;
                    else if (Main.expertMode)
                        debuffTimeMultiplier = Main.RegisteredGameModes[1].DebuffTimeMultiplier;
                }
                player.AddBuff(buffID, (int)Math.Round(intendedTime / debuffTimeMultiplier, MidpointRounding.ToEven), quiet);
            }
            else
            {
                player.AddBuff(buffID, intendedTime, quiet);
            }
        }

        public static float ProjWorldDamage => Main.GameModeInfo.IsJourneyMode
            ? CreativePowerManager.Instance.GetPower<CreativePowers.DifficultySliderPower>().StrengthMultiplierToGiveNPCs
            : Main.GameModeInfo.EnemyDamageMultiplier;

        public static int ScaledProjectileDamage(int npcDamage, float modifier = 1, int npcDamageCalculationsOffset = 2)
        {
            const float inherentHostileProjMultiplier = 2;
            float worldDamage = ProjWorldDamage;
            return (int)(modifier * npcDamage / inherentHostileProjMultiplier / Math.Max(npcDamageCalculationsOffset, worldDamage));
        }

        public static bool IsSummonDamage(Projectile projectile, bool includeMinionShot = true, bool includeWhips = true)
        {
            if (!includeWhips && ProjectileID.Sets.IsAWhip[projectile.type])
                return false;

            if (!includeMinionShot && (ProjectileID.Sets.MinionShot[projectile.type] || ProjectileID.Sets.SentryShot[projectile.type]))
                return false;

            return projectile.CountsAsClass(DamageClass.Summon) || projectile.minion || projectile.sentry || projectile.minionSlots > 0 || ProjectileID.Sets.MinionSacrificable[projectile.type]
                || (includeMinionShot && (ProjectileID.Sets.MinionShot[projectile.type] || ProjectileID.Sets.SentryShot[projectile.type]))
                || (includeWhips && ProjectileID.Sets.IsAWhip[projectile.type]);
        }

        public static bool CanDeleteProjectile(Projectile projectile, int deletionRank = 0, bool clearSummonProjs = false)
        {
            if (!projectile.active)
                return false;
            if (projectile.damage <= 0)
                return false;
            if (YharimEXCrossmodSystem.FargowiltasSouls.Loaded)
            {
                if (projectile.FargoSouls().DeletionImmuneRank > deletionRank)
                    return false;
            }
            if (projectile.friendly)
                {
                    if (projectile.whoAmI == Main.player[projectile.owner].heldProj)
                        return false;
                    if (IsSummonDamage(projectile, false) && !clearSummonProjs)
                        return false;
                }
            return true;
        }

        public static Projectile ProjectileExists(int whoAmI, params int[] types)
        {
            return whoAmI > -1 && whoAmI < Main.maxProjectiles && Main.projectile[whoAmI].active && (types.Length == 0 || types.Contains(Main.projectile[whoAmI].type)) ? Main.projectile[whoAmI] : null;
        }

        public static Projectile ProjectileExists(float whoAmI, params int[] types)
        {
            return ProjectileExists((int)whoAmI, types);
        }

        public static bool OtherBossAlive(int npcID)
        {
            if (npcID > -1 && npcID < Main.maxNPCs)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].boss && i != npcID)
                        return true;
                }
            }
            return false;
        }

        public static void ClearFriendlyProjectiles(int deletionRank = 0, int bossNpc = -1, bool clearSummonProjs = false)
        {
            ClearProjectiles(false, true, deletionRank, bossNpc, clearSummonProjs);
        }

        public static void ClearHostileProjectiles(int deletionRank = 0, int bossNpc = -1)
        {
            ClearProjectiles(true, false, deletionRank, bossNpc);
        }

        public static void ClearAllProjectiles(int deletionRank = 0, int bossNpc = -1, bool clearSummonProjs = false)
        {
            ClearProjectiles(true, true, deletionRank, bossNpc, clearSummonProjs);
        }

        private static void ClearProjectiles(bool clearHostile, bool clearFriendly, int deletionRank = 0, int bossNpc = -1, bool clearSummonProjs = false)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (OtherBossAlive(bossNpc))
                clearHostile = false;

            for (int j = 0; j < 2; j++) //do twice to wipe out projectiles spawned by projectiles
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && ((projectile.hostile && clearHostile) || (projectile.friendly && clearFriendly)) && CanDeleteProjectile(projectile, deletionRank, clearSummonProjs))
                    {
                        projectile.Kill();
                    }
                }
            }
        }

        public static void PrintText(string text)
        {
            PrintText(text, Color.White);
        }

        public static void PrintLocalization(string localizationKey, Color color)
        {
            PrintText(Language.GetTextValue(localizationKey), color);
        }

        public static void PrintLocalization(string localizationKey, int r, int g, int b) => PrintLocalization(localizationKey, new Color(r, g, b));

        public static void PrintLocalization(string localizationKey, Color color, params object[] args) => PrintText(Language.GetTextValue(localizationKey, args), color);

        public static void PrintText(string text, Color color)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(text, color);
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
            }
        }

        public static void PrintText(string text, int r, int g, int b) => PrintText(text, new Color(r, g, b));

        public static Vector2 ClosestPointInHitbox(Rectangle hitboxOfTarget, Vector2 desiredLocation)
        {
            Vector2 offset = desiredLocation - hitboxOfTarget.Center.ToVector2();
            offset.X = Math.Min(Math.Abs(offset.X), hitboxOfTarget.Width / 2) * Math.Sign(offset.X);
            offset.Y = Math.Min(Math.Abs(offset.Y), hitboxOfTarget.Height / 2) * Math.Sign(offset.Y);
            return hitboxOfTarget.Center.ToVector2() + offset;
        }

        public static Vector2 ClosestPointInHitbox(Entity entity, Vector2 desiredLocation)
        {
            return ClosestPointInHitbox(entity.Hitbox, desiredLocation);
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

        public static bool IsProjSourceItemUseReal(Projectile proj, IEntitySource source)
        {
            return source is EntitySource_ItemUse parent && parent.Item.type == Main.player[proj.owner].HeldItem.type;
        }

        public static bool AprilFools => DateTime.Today.Month == 4 && DateTime.Today.Day <= 7;

        public static void ScreenshakeRumble(float strength)
        {
            if (ScreenShakeSystem.OverallShakeIntensity < strength)
            {
                ScreenShakeSystem.SetUniversalRumble(strength, MathF.Tau, null, 0.2f);
            }
        }
        public static Color BuffEffects(Entity codable, Color lightColor, float shadow = 0f, bool effects = true, bool poisoned = false, bool onFire = false, bool onFire2 = false, bool hunter = false, bool noItems = false, bool blind = false, bool bleed = false, bool venom = false, bool midas = false, bool ichor = false, bool onFrostBurn = false, bool burned = false, bool honey = false, bool dripping = false, bool drippingSlime = false, bool loveStruck = false, bool stinky = false)
        {
            float cr = 1f; float cg = 1f; float cb = 1f; float ca = 1f;
            if (effects && honey && Main.rand.NextBool(30))
            {
                int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.Honey, 0f, 0f, 150, default, 1f);
                Main.dust[dustID].velocity.Y = 0.3f;
                Main.dust[dustID].velocity.X *= 0.1f;
                Main.dust[dustID].scale += Main.rand.Next(3, 4) * 0.1f;
                Main.dust[dustID].alpha = 100;
                Main.dust[dustID].noGravity = true;
                Main.dust[dustID].velocity += codable.velocity * 0.1f;
                //if (codable is Player) Main.playerDrawDust.Add(dustID);
            }
            if (poisoned)
            {
                if (effects && Main.rand.NextBool(30))
                {
                    int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.Poisoned, 0f, 0f, 120, default, 0.2f);
                    Main.dust[dustID].noGravity = true;
                    Main.dust[dustID].fadeIn = 1.9f;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                cr *= 0.65f;
                cb *= 0.75f;
            }
            if (venom)
            {
                if (effects && Main.rand.NextBool(10))
                {
                    int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.Venom, 0f, 0f, 100, default, 0.5f);
                    Main.dust[dustID].noGravity = true;
                    Main.dust[dustID].fadeIn = 1.5f;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                cg *= 0.45f;
                cr *= 0.75f;
            }
            if (midas)
            {
                cb *= 0.3f;
                cr *= 0.85f;
            }
            if (ichor)
            {
                if (codable is NPC) { lightColor = new Color(255, 255, 0, 255); } else { cb = 0f; }
            }
            if (burned)
            {
                if (effects)
                {
                    int dustID = Dust.NewDust(new Vector2(codable.position.X - 2f, codable.position.Y - 2f), codable.width + 4, codable.height + 4, DustID.Torch, codable.velocity.X * 0.4f, codable.velocity.Y * 0.4f, 100, default, 2f);
                    Main.dust[dustID].noGravity = true;
                    Main.dust[dustID].velocity *= 1.8f;
                    Main.dust[dustID].velocity.Y -= 0.75f;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                if (codable is Player)
                {
                    cr = 1f;
                    cb *= 0.6f;
                    cg *= 0.7f;
                }
            }
            if (onFrostBurn)
            {
                if (effects)
                {
                    if (Main.rand.Next(4) < 3)
                    {
                        int dustID = Dust.NewDust(new Vector2(codable.position.X - 2f, codable.position.Y - 2f), codable.width + 4, codable.height + 4, DustID.IceTorch, codable.velocity.X * 0.4f, codable.velocity.Y * 0.4f, 100, default, 3.5f);
                        Main.dust[dustID].noGravity = true;
                        Main.dust[dustID].velocity *= 1.8f;
                        Main.dust[dustID].velocity.Y -= 0.5f;
                        if (Main.rand.NextBool(4))
                        {
                            Main.dust[dustID].noGravity = false;
                            Main.dust[dustID].scale *= 0.5f;
                        }
                        //if (codable is Player) Main.playerDrawDust.Add(dustID);
                    }
                    Lighting.AddLight((int)(codable.position.X / 16f), (int)(codable.position.Y / 16f + 1f), 0.1f, 0.6f, 1f);
                }
                if (codable is Player)
                {
                    cr *= 0.5f;
                    cg *= 0.7f;
                }
            }
            if (onFire)
            {
                if (effects)
                {
                    if (!Main.rand.NextBool(4))
                    {
                        int dustID = Dust.NewDust(codable.position - new Vector2(2f, 2f), codable.width + 4, codable.height + 4, DustID.Torch, codable.velocity.X * 0.4f, codable.velocity.Y * 0.4f, 100, default, 3.5f);
                        Main.dust[dustID].noGravity = true;
                        Main.dust[dustID].velocity *= 1.8f;
                        Main.dust[dustID].velocity.Y -= 0.5f;
                        if (Main.rand.NextBool(4))
                        {
                            Main.dust[dustID].noGravity = false;
                            Main.dust[dustID].scale *= 0.5f;
                        }
                        //if (codable is Player) Main.playerDrawDust.Add(dustID);
                    }
                    Lighting.AddLight((int)(codable.position.X / 16f), (int)(codable.position.Y / 16f + 1f), 1f, 0.3f, 0.1f);
                }
                if (codable is Player)
                {
                    cb *= 0.6f;
                    cg *= 0.7f;
                }
            }
            if (dripping && shadow == 0f && !Main.rand.NextBool(4))
            {
                Vector2 position = codable.position;
                position.X -= 2f; position.Y -= 2f;
                if (Main.rand.NextBool())
                {
                    int dustID = Dust.NewDust(position, codable.width + 4, codable.height + 2, DustID.Wet, 0f, 0f, 50, default, 0.8f);
                    if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                    if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                    Main.dust[dustID].noLight = true;
                    Main.dust[dustID].velocity *= 0.2f;
                    Main.dust[dustID].velocity.Y += 0.2f;
                    Main.dust[dustID].velocity += codable.velocity;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                else
                {
                    int dustID = Dust.NewDust(position, codable.width + 8, codable.height + 8, DustID.Wet, 0f, 0f, 50, default, 1.1f);
                    if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                    if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                    Main.dust[dustID].noLight = true;
                    Main.dust[dustID].noGravity = true;
                    Main.dust[dustID].velocity *= 0.2f;
                    Main.dust[dustID].velocity.Y += 1f;
                    Main.dust[dustID].velocity += codable.velocity;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
            }
            if (drippingSlime && shadow == 0f)
            {
                int alpha = 175;
                Color newColor = new(0, 80, 255, 100);
                if (!Main.rand.NextBool(4))
                {
                    if (Main.rand.NextBool())
                    {
                        Vector2 position2 = codable.position;
                        position2.X -= 2f; position2.Y -= 2f;
                        int dustID = Dust.NewDust(position2, codable.width + 4, codable.height + 2, DustID.TintableDust, 0f, 0f, alpha, newColor, 1.4f);
                        if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                        if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                        Main.dust[dustID].noLight = true;
                        Main.dust[dustID].velocity *= 0.2f;
                        Main.dust[dustID].velocity.Y += 0.2f;
                        Main.dust[dustID].velocity += codable.velocity;
                        //if (codable is Player) Main.playerDrawDust.Add(dustID);
                    }
                }
                cr *= 0.8f;
                cg *= 0.8f;
            }
            if (onFire2)
            {
                if (effects)
                {
                    if (!Main.rand.NextBool(4))
                    {
                        int dustID = Dust.NewDust(codable.position - new Vector2(2f, 2f), codable.width + 4, codable.height + 4, DustID.CursedTorch, codable.velocity.X * 0.4f, codable.velocity.Y * 0.4f, 100, default, 3.5f);
                        Main.dust[dustID].noGravity = true;
                        Main.dust[dustID].velocity *= 1.8f;
                        Main.dust[dustID].velocity.Y -= 0.5f;
                        if (Main.rand.NextBool(4))
                        {
                            Main.dust[dustID].noGravity = false;
                            Main.dust[dustID].scale *= 0.5f;
                        }
                        //if (codable is Player) Main.playerDrawDust.Add(dustID);
                    }
                    Lighting.AddLight((int)(codable.position.X / 16f), (int)(codable.position.Y / 16f + 1f), 1f, 0.3f, 0.1f);
                }
                if (codable is Player)
                {
                    cb *= 0.6f;
                    cg *= 0.7f;
                }
            }
            if (noItems)
            {
                cr *= 0.65f;
                cg *= 0.8f;
            }
            if (blind)
            {
                cr *= 0.7f;
                cg *= 0.65f;
            }
            if (bleed)
            {
                bool dead = codable is Player player ? player.dead : codable is NPC nPC && nPC.life <= 0;
                if (effects && !dead && Main.rand.NextBool(30))
                {
                    int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.Blood, 0f, 0f, 0, default, 1f);
                    Main.dust[dustID].velocity.Y += 0.5f;
                    Main.dust[dustID].velocity *= 0.25f;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                cg *= 0.9f;
                cb *= 0.9f;
            }
            if (loveStruck && effects && shadow == 0f && Main.instance.IsActive && !Main.gamePaused && Main.rand.NextBool(5))
            {
                Vector2 value = new(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11));
                value.Normalize();
                value.X *= 0.66f;
                int goreID = Gore.NewGore(codable.GetSource_FromThis(), codable.position + new Vector2(Main.rand.Next(codable.width + 1), Main.rand.Next(codable.height + 1)), value * Main.rand.Next(3, 6) * 0.33f, 331, Main.rand.Next(40, 121) * 0.01f);
                Main.gore[goreID].sticky = false;
                Main.gore[goreID].velocity *= 0.4f;
                Main.gore[goreID].velocity.Y -= 0.6f;
                //if (codable is Player) Main.playerDrawGore.Add(goreID);
            }
            if (stinky && shadow == 0f)
            {
                cr *= 0.7f;
                cb *= 0.55f;
                if (effects && Main.rand.NextBool(5) && Main.instance.IsActive && !Main.gamePaused)
                {
                    Vector2 value2 = new(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11));
                    value2.Normalize(); value2.X *= 0.66f; value2.Y = Math.Abs(value2.Y);
                    Vector2 vector = value2 * Main.rand.Next(3, 5) * 0.25f;
                    int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.FartInAJar, vector.X, vector.Y * 0.5f, 100, default, 1.5f);
                    Main.dust[dustID].velocity *= 0.1f;
                    Main.dust[dustID].velocity.Y -= 0.5f;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
            }
            lightColor.R = (byte)(lightColor.R * cr);
            lightColor.G = (byte)(lightColor.G * cg);
            lightColor.B = (byte)(lightColor.B * cb);
            lightColor.A = (byte)(lightColor.A * ca);

            if (hunter && (codable is not NPC || ((NPC)codable).lifeMax > 1))
            {
                if (effects && !Main.gamePaused && Main.instance.IsActive && Main.rand.NextBool(50))
                {
                    int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.MagicMirror, 0f, 0f, 150, default, 0.8f);
                    Main.dust[dustID].velocity *= 0.1f;
                    Main.dust[dustID].noLight = true;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                byte colorR = 50, colorG = 255, colorB = 50;
                if (codable is NPC nPC && !(nPC.friendly || nPC.catchItem > 0 || (nPC.damage == 0 && nPC.lifeMax == 5)))
                {
                    colorR = 255; colorG = 50;
                }
                if (codable is not NPC && lightColor.R < 150) { lightColor.A = Main.mouseTextColor; }
                if (lightColor.R < colorR) { lightColor.R = colorR; }
                if (lightColor.G < colorG) { lightColor.G = colorG; }
                if (lightColor.B < colorB) { lightColor.B = colorB; }
            }
            return lightColor;
        }
    }
}
