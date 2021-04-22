using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace TailLib
{
    /// <summary>
    /// This mod's globalNpc for handling tails on npcs
    /// </summary>
    public class TailGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

        /// <summary>
        /// The instance of the current tail, is null unless set
        /// </summary>
        public TailInstance tail = null;

        /// <summary>
        /// If the tail is currently active
        /// </summary>
        public bool tailActive = false;

        public override bool PreAI(NPC npc)
        {
            if (tailActive)
            {
                tail.tailBones.Active = npc.active && tailActive;
                tail.Update(npc.Center + new Vector2(0, npc.gfxOffY), new Vector2(npc.direction, 1));
            }

            return base.PreAI(npc);
        }

        public override bool CheckDead(NPC npc)
        {
            if (tailActive)
                tail.Remove();
            return base.CheckDead(npc);
        }
    }
}
