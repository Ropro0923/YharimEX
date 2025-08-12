using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
//using FargowiltasSouls.Content.Items.Misc;
//using FargowiltasSouls.Content.Projectiles;
//using FargowiltasSouls.Core.AccessoryEffectSystem;
//using FargowiltasSouls.Core.Globals;
//using FargowiltasSouls.Core.ModPlayers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using YharimEX.Core.Globals;
using YharimEX.Core.Players;

namespace YharimEX
{
    public static class YharimEXExtentionMethods
    {
        private static readonly FieldInfo _damageFieldHitInfo = typeof(NPC.HitInfo).GetField("_damage", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo _damageFieldHurtInfo = typeof(Player.HurtInfo).GetField("_damage", BindingFlags.Instance | BindingFlags.NonPublic);

        public static TooltipLine ArticlePrefixAdjustment(this TooltipLine itemName, string[] localizationArticles)
        {
            List<string> list = itemName.Text.Split(' ').ToList();
            for (int i = 0; i < localizationArticles.Length; i++)
            {
                if (list.Remove(localizationArticles[i]))
                {
                    list.Insert(0, localizationArticles[i]);
                    break;
                }
            }

            itemName.Text = string.Join(" ", list);
            return itemName;
        }

        public static string ArticlePrefixAdjustmentString(this string itemName, string[] localizationArticles)
        {
            List<string> list = itemName.Split(' ').ToList();
            for (int i = 0; i < localizationArticles.Length; i++)
            {
                if (list.Remove(localizationArticles[i]))
                {
                    list.Insert(0, localizationArticles[i]);
                    break;
                }
            }

            itemName = string.Join(" ", list);
            return itemName;
        }

        public static bool TryFindTooltipLine(this List<TooltipLine> tooltips, string tooltipName, out TooltipLine tooltipLine)
        {
            tooltips.TryFindTooltipLine(tooltipName, "Terraria", out tooltipLine);
            return tooltipLine != null;
        }

        public static bool TryFindTooltipLine(this List<TooltipLine> tooltips, string tooltipName, string tooltipMod, out TooltipLine tooltipLine)
        {
            tooltipLine = tooltips.First((TooltipLine line) => line.Name == tooltipName && line.Mod == tooltipMod);
            return tooltipLine != null;
        }

        public static void Null(this ref NPC.HitInfo hitInfo)
        {
            object obj = hitInfo;
            _damageFieldHitInfo.SetValue(obj, 0);
            hitInfo = (NPC.HitInfo)obj;
            hitInfo.Knockback = 0f;
            hitInfo.Crit = false;
            hitInfo.InstantKill = false;
        }

        public static void Null(this ref NPC.HitModifiers hitModifiers)
        {
            hitModifiers.ModifyHitInfo += delegate (ref NPC.HitInfo hitInfo)
            {
                hitInfo.Null();
            };
        }

        public static void Null(this ref Player.HurtInfo hurtInfo)
        {
            object obj = hurtInfo;
            _damageFieldHurtInfo.SetValue(obj, 0);
            hurtInfo = (Player.HurtInfo)obj;
            hurtInfo.Knockback = 0f;
        }

        public static void Null(this ref Player.HurtModifiers hurtModifiers)
        {
            hurtModifiers.ModifyHurtInfo += delegate (ref Player.HurtInfo hurtInfo)
            {
                hurtInfo.Null();
            };
        }

        public static void AddDebuffImmunities(this NPC npc, List<int> debuffs)
        {
            foreach (int debuff in debuffs)
            {
                NPCID.Sets.SpecificDebuffImmunity[npc.type][debuff] = true;
            }
        }

        public static YharimEXGlobalNPC YharimEX(this NPC npc)
        {
            return npc.GetGlobalNPC<YharimEXGlobalNPC>();
        }

        //public static EModeGlobalNPC Eternity(this NPC npc)
        //{
        //    return npc.GetGlobalNPC<EModeGlobalNPC>();
        //}

        //public static FargoSoulsGlobalProjectile FargoSouls(this Projectile projectile)
        //{
        //    return projectile.GetGlobalProjectile<FargoSoulsGlobalProjectile>();
        //}

        //public static EModeGlobalProjectile Eternity(this Projectile projectile)
        //{
        //    return projectile.GetGlobalProjectile<EModeGlobalProjectile>();
        //}

        public static YharimEXPlayer YharimPlayer(this Player player)
        {
            return player.GetModPlayer<YharimEXPlayer>();
        }

        //public static EModePlayer Eternity(this Player player)
        //{
        //    return player.GetModPlayer<EModePlayer>();
        //}

        //public static AccessoryEffectPlayer AccessoryEffects(this Player player)
        //{
        //    return player.GetModPlayer<AccessoryEffectPlayer>();
        //}

        public static bool Alive(this Player player)
        {
            if (player != null && player.active && !player.dead)
            {
                return !player.ghost;
            }

            return false;
        }

        public static bool Alive(this Projectile projectile)
        {
            return projectile?.active ?? false;
        }

        public static bool Alive(this NPC npc)
        {
            return npc?.active ?? false;
        }

        public static bool TypeAlive(this Projectile projectile, int type)
        {
            if (projectile.Alive())
            {
                return projectile.type == type;
            }

            return false;
        }

        public static bool TypeAlive<T>(this Projectile projectile) where T : ModProjectile
        {
            if (projectile.Alive())
            {
                return projectile.type == ModContent.ProjectileType<T>();
            }

            return false;
        }

        public static bool TypeAlive(this NPC npc, int type)
        {
            if (npc.Alive())
            {
                return npc.type == type;
            }

            return false;
        }

        public static bool TypeAlive<T>(this NPC npc) where T : ModNPC
        {
            if (npc.Alive())
            {
                return npc.type == ModContent.NPCType<T>();
            }

            return false;
        }

        //public static NPC GetSourceNPC(this Projectile projectile)
        //{
        //    return projectile.GetGlobalProjectile<A_SourceNPCGlobalProjectile>().sourceNPC;
        //}

        //public static void SetSourceNPC(this Projectile projectile, NPC npc)
        //{
        //    projectile.GetGlobalProjectile<A_SourceNPCGlobalProjectile>().sourceNPC = npc;
        //}

        public static float ActualClassDamage(this Player player, DamageClass damageClass)
        {
            return player.GetTotalDamage(damageClass).Additive * player.GetTotalDamage(damageClass).Multiplicative;
        }

        public static bool IsWeapon(this Item item)
        {
            if (item.damage <= 0 || item.pick != 0 || item.axe != 0 || item.hammer != 0)
            {
                return item.type == 905;
            }

            return true;
        }

        public static bool IsWeaponWithDamageClass(this Item item)
        {
            if (item.damage <= 0 || item.DamageType == DamageClass.Default || item.pick != 0 || item.axe != 0 || item.hammer != 0)
            {
                return item.type == 905;
            }

            return true;
        }

        public static bool IsWithinBounds(this int index, int cap)
        {
            if (index >= 0)
            {
                return index < cap;
            }

            return false;
        }

        public static bool IsWithinBounds(this int index, int lowerBound, int higherBound)
        {
            if (index >= lowerBound)
            {
                return index < higherBound;
            }

            return false;
        }

        public static Vector2 SetMagnitude(this Vector2 vector, float magnitude)
        {
            return vector.SafeNormalize(Vector2.UnitY) * magnitude;
        }

        //public static float ActualClassCrit(this Player player, DamageClass damageClass)
        //{
        //    if ((damageClass != DamageClass.Summon && damageClass != DamageClass.SummonMeleeSpeed) || player.FargoSouls().MinionCrits)
        //    {
        //        return player.GetTotalCritChance(damageClass);
        //    }

        //    return 0f;
        //}

        public static bool FeralGloveReuse(this Player player, Item item)
        {
            if (player.autoReuseGlove)
            {
                if (!item.CountsAsClass(DamageClass.Melee))
                {
                    return item.CountsAsClass(DamageClass.SummonMeleeSpeed);
                }

                return true;
            }

            return false;
        }

        //public static bool CannotUseItems(this Player player)
        //{
        //    if (!player.CCed && !player.noItems && player.FargoSouls().NoUsingItems <= 0)
        //    {
        //        if (player.HeldItem != null)
        //        {
        //            if (ItemLoader.CanUseItem(player.HeldItem, player))
        //            {
        //                return !PlayerLoader.CanUseItem(player, player.HeldItem);
        //            }

        //            return true;
        //        }

        //        return false;
        //    }

        //    return true;
        //}

        //public static void Incapacitate(this Player player, bool preventDashing = true)
        //{
        //    player.controlLeft = false;
        //    player.controlRight = false;
        //    player.controlJump = false;
        //    player.controlDown = false;
        //    player.controlUseItem = false;
        //    player.controlUseTile = false;
        //    player.controlHook = false;
        //    player.releaseHook = true;
        //    if (player.grapCount > 0)
        //    {
        //        player.RemoveAllGrapplingHooks();
        //    }

        //    if (player.mount.Active)
        //    {
        //        player.mount.Dismount(player);
        //    }

        //    player.FargoSouls().NoUsingItems = 2;
        //    if (preventDashing)
        //    {
        //        for (int i = 0; i < 4; i++)
        //        {
        //            player.doubleTapCardinalTimer[i] = 0;
        //            player.holdDownCardinalTimer[i] = 0;
        //        }
        //    }

        //    if (player.dashDelay < 10 && preventDashing)
        //    {
        //        player.dashDelay = 10;
        //    }
        //}

        public static bool CountsAsClass(this DamageClass damageClass, DamageClass intendedClass)
        {
            if (damageClass != intendedClass)
            {
                return damageClass.GetEffectInheritance(intendedClass);
            }

            return true;
        }

        public static DamageClass ProcessDamageTypeFromHeldItem(this Player player)
        {
            if (player.HeldItem.damage <= 0 || player.HeldItem.pick > 0 || player.HeldItem.axe > 0 || player.HeldItem.hammer > 0)
            {
                return DamageClass.Summon;
            }

            if (player.HeldItem.DamageType.CountsAsClass(DamageClass.Melee))
            {
                return DamageClass.Melee;
            }

            if (player.HeldItem.DamageType.CountsAsClass(DamageClass.Ranged))
            {
                return DamageClass.Ranged;
            }

            if (player.HeldItem.DamageType.CountsAsClass(DamageClass.Magic))
            {
                return DamageClass.Magic;
            }

            if (player.HeldItem.DamageType.CountsAsClass(DamageClass.Summon))
            {
                return DamageClass.Summon;
            }

            if (player.HeldItem.DamageType != DamageClass.Generic && player.HeldItem.DamageType != DamageClass.Default)
            {
                return player.HeldItem.DamageType;
            }

            return DamageClass.Summon;
        }

        public static void Animate(this Projectile proj, int ticksPerFrame, int startFrame = 0, int? frames = null)
        {
            int valueOrDefault = frames.GetValueOrDefault();
            if (!frames.HasValue)
            {
                valueOrDefault = Main.projFrames[proj.type];
                frames = valueOrDefault;
            }

            if (++proj.frameCounter >= ticksPerFrame)
            {
                if (++proj.frame >= startFrame + frames)
                {
                    proj.frame = startFrame;
                }

                proj.frameCounter = 0;
            }
        }

        public static Rectangle ToWorldCoords(this Rectangle rectangle)
        {
            return new Rectangle(rectangle.X * 16, rectangle.Y * 16, rectangle.Width * 16, rectangle.Height * 16);
        }

        public static Rectangle ToTileCoords(this Rectangle rectangle)
        {
            return new Rectangle(rectangle.X / 16, rectangle.Y / 16, rectangle.Width / 16, rectangle.Height / 16);
        }
    }
}
