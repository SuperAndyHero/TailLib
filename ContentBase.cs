using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;
using Terraria.DataStructures;

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

        #region manual example
        //you can still manually set a tail active from anywhere via something like this

        //player.GetModPlayer<TailPlayer>().CurrentTailType = TailType;
        //player.GetModPlayer<TailPlayer>().DyeItemType = GetAppliedDyeItem(player);

        //public int GetAppliedDyeItem(Player player)
        //{
        //    int dyeType = -1;
        //    for (int i = 0; i < player.armor.Length; i++)
        //    {
        //        if (player.armor[i] == Item)
        //        {
        //            Item dyeitem = player.dye[i];

        //            if (dyeitem != null && !dyeitem.IsAir)
        //                dyeType = dyeitem.type;

        //            break;
        //        }
        //    }
        //    return dyeType;
        //}
        #endregion

        public virtual bool TailActive => true;
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

        public sealed override void OnSpawn(IEntitySource source)
        {
            NPC.SetTail(TailType);
            SafeOnSpawn();
        }

        /// <summary>
        /// Use this as you would normally use AI.
        /// Return false to disable tail.
        /// </summary>
        /// <returns></returns>
        public virtual bool SafeOnSpawn() => true;
    }
}
