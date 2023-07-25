using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using static Terraria.ModLoader.PlayerDrawLayer;

namespace TailLib
{
    /// <summary>
    /// This mod's globalNpc for handling tails on npcs
    /// </summary>
    public class TailGlobalNPC : GlobalNPC
	{

        public static List<NPC> RemovalQueue = new List<NPC>();
        public static Dictionary<NPC, TailInstance> ActiveTailNpcsList = new Dictionary<NPC, TailInstance>();

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
            //npcs dont support having their tail type changed automatically (likely not needed)
            //only runs when on screen
            if (tailActive && (TailSystem.CullRect.Contains(npc.Center.ToPoint()) || TailSystem.CullRect.Contains(tail.tailBones.startPoint.ToPoint())))
            {
                tail.tailBones.Active = npc.active && tailActive;
                tail.Update(npc.Center + new Vector2(0, npc.gfxOffY), new Vector2(npc.direction, 1));
                tail.alpha = npc.Opacity;//so tail matches npc opacity
            }

            return base.PreAI(npc);
        }

        public override bool CheckDead(NPC npc)
        {
            if (tailActive)
                tail.Remove();
            return base.CheckDead(npc);
        }

        public override void Unload()//is not per npc instance
        {
            RemovalQueue = null;
            ActiveTailNpcsList = null;
        }
    }
}
