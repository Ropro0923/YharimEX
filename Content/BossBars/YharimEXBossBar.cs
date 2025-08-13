using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace YharimEX.Content.BossBars
{
    public class YharimEXBossBar : ModBossBar
    {
        private Asset<Texture2D> forcedHead;

        public override void Load()
        {
            forcedHead = ModContent.Request<Texture2D>("YharimEX/Assets/NPCs/YharimEXIllusion_Head_Boss");
        }

        public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
        {
            return forcedHead;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
        {
            if (npc.ai[0] <= -1 && npc.ai[0] >= -7)
            {
                drawParams.ShowText = false;
                drawParams.BarCenter += Main.rand.NextVector2Circular(0.2f, 0.2f) * 5f;
            }
            return true;
        }

        public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
        {
            NPC npc = Main.npc[info.npcIndexToAimAt];
            if (npc.townNPC || !npc.active)
                return false;

            life = npc.life;
            lifeMax = npc.lifeMax;
            return true;
        }
    }
}
