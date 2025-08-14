using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfernumMode.Core.Netcode.Packets;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using YharimEX.Core.Systems;

namespace YharimEX.Content.NPCs.Bosses
{
    public abstract class CalDLCEmodeAttacksBase : GlobalNPC
    {
        public NPCMatcher Matcher;

        public override bool InstancePerEntity => true;

        public sealed override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return lateInstantiation && Matcher.Satisfies(entity.type) && (YharimEXWorldFlags.DeathMode || YharimEXWorldFlags.InfernumMode || YharimEXWorldFlags.EternityMode || YharimEXWorldFlags.MasochistModeReal);
        }

        public override void Load()
        {
            Matcher = CreateMatcher();
            base.Load();
        }

        public abstract NPCMatcher CreateMatcher();
        public override GlobalNPC NewInstance(NPC target)
        {
            return ExtraRequirements() ? base.NewInstance(target) : null;
        }

        public virtual bool ExtraRequirements() { return true; }

        public bool FirstTick = true;
        public virtual void OnFirstTick(NPC npc) { }

        public virtual bool SafePreAI(NPC npc) => base.PreAI(npc);

        public sealed override bool PreAI(NPC npc)
        {
            if (!(ExtraRequirements()))
            {
                return true;
            }
            if (FirstTick)
            {
                FirstTick = false;

                OnFirstTick(npc);
            }
            return SafePreAI(npc);
        }
        public virtual void SafePostAI(NPC npc) => base.PostAI(npc);
        public sealed override void PostAI(NPC npc)
        {
            if (!(ExtraRequirements()))
            {
                return;
            }
            SafePostAI(npc);
            return;
        }

        protected static void NetSync(NPC npc, bool onlySendFromServer = true)
        {
            if (onlySendFromServer && Main.netMode != NetmodeID.Server)
                return;

            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
        }

    }
}
