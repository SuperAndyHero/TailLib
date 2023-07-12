using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;

namespace TailLib
{
    /// <summary>
    ///A helper for items that give the player a tail
    /// Internally this is done with:  <c>player.GetModPlayer<TailPlayer>().CurrentTailType = TailType</c>
    /// UpdateAccessory is used for this so use SafeUpdateAccessory instead
    /// </summary>
    public abstract class TailItem : ModItem
    {
        public readonly Type TailType;
        protected TailItem(Type tailType) =>
            TailType = tailType;

        public sealed override void UpdateEquip(Player player)
        {
            if (SafeUpdateEquip(player))
            {
                player.GetModPlayer<TailPlayer>().CurrentTailType = TailType;
                player.GetModPlayer<TailPlayer>().DyeItemType = GetAppliedDyeItem(player);
            }
        }

        public int GetAppliedDyeItem(Player player)
        {
            int dyeType = -1;
            for (int i = 0; i < 5; i++)
            {
                if (player.armor[i + 3] == Item)
                {
                    Item dyeitem = player.dye[i + 3];

                    if (dyeitem != null && !dyeitem.IsAir)
                        dyeType = dyeitem.type;

                    break;
                }
            }
            return dyeType;
        }

        /// <summary>
        /// Use this as you would normally use UpdateAccessory.
        /// Return false to disable tail.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="hideVisual"></param>
        /// <returns></returns>
        public virtual bool SafeUpdateEquip(Player player) => true;
    }

    /// <summary>
    /// A helper for npcs with tail
    /// Internally this is done with:  <c>npc.SetTail(TailType)</c>
    /// AI is used for this so use SafeAI instead
    /// </summary>
    public abstract class TailNpc : ModNPC
    {
        public readonly Type TailType;
        protected TailNpc(Type tailType) =>
            TailType = tailType;

        private bool hasSpawned = false;
        public sealed override void AI()
        {
            if (!hasSpawned)
            {
                NPC.SetTail(TailType);
                hasSpawned = true;
            }

            //npc.TailActive(SafeAI());
        }

        /// <summary>
        /// Use this as you would normally use AI.
        /// Return false to disable tail.
        /// </summary>
        /// <returns></returns>
        public virtual bool SafeAI() => true;
    }
}
