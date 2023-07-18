using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.DataStructures;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Collections.Generic;
using TailLib;
using Terraria.ModLoader.IO;
using static TailLib.TailHandler;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using static Terraria.GameContent.Animations.Actions;
using Microsoft.VisualBasic;

namespace TailLib
{
    /// <summary>
    /// This mod's modplayer for handling tails on the player
    /// </summary>
    public class TailPlayer : ModPlayer
    {
        /// <summary>
        /// The instance of the current tail, is null unless set
        /// </summary>
        private TailInstance tail = null;

        /// <summary>
        /// If the tail is currently active
        /// </summary>
        public bool currentlyActive = false;
        /// <summary>
        /// If the tail is previously active
        /// </summary>
        public bool previouslyActive = false;


        private Type _currentTailType = null;
        /// <summary>
        /// The current active tail, if you want to set the player's tail just set this every update, like UpdateAccessory in ModItem
        /// </summary>
        public Type CurrentTailType 
        {
            get => _currentTailType;
            set {
                _currentTailType = value;
                currentlyActive = true; }
        }

        public int DyeItemType = -1;

        public override void PreUpdate()
        {
            if (currentlyActive)
            {
                if (tail != null)
                {
                    //if current tailType is different than the type of the active tail
                    if (_currentTailType != null && (tail.tailBase.GetType() != _currentTailType || (!previouslyActive && currentlyActive)))
                    {
                        tail.Remove();
                        tail = new TailInstance(_currentTailType, Player.Center, Layer.Player, Player, (int)Player.gravDir);
                    }

                    tail.tailBones.Active = !Player.dead && currentlyActive;

                    tail.Update(Player.MountedCenter + new Vector2(/*fixes centering issue*/ -0.5f, Player.gfxOffY), Player.FacingDirection());
                    tail.alpha = (255 - Player.immuneAlpha) * 0.003921568627f;//for the blinking when damaged
                    tail.armorShaderData = GameShaders.Armor.GetShaderFromItemId(DyeItemType);//GameShaders.Armor.GetShaderFromItemId(ItemID.MushroomDye);
                }
                else if (_currentTailType != null)
                    tail = new TailInstance(_currentTailType, Player.Center, Layer.Player, Player, (int)Player.gravDir);
            }
        }

        public Tailbase currentMenuBase = null;
        public override void ResetEffects()
        {

            if (previouslyActive && !currentlyActive /*&& null check*/ && tail != null)
                tail.Remove();

            previouslyActive = currentlyActive;
            currentlyActive = false;


            Type newType = null;

            for (int i = 0; i < Player.armor.Length; i++)
            {
                if (Player.armor[i].ModItem is TailItem)
                {

                    TailItem tailitem = Player.armor[i].ModItem as TailItem;

                    if (!tailitem.TailActive)
                        continue;

                    newType = tailitem.TailType;

                    Item dyeitem = Player.dye[i % Player.dye.Length];
                    if (dyeitem != null)
                        DyeItemType = dyeitem.type;

                    break;
                }
            }


            if (Main.gameMenu)
            {
                if (newType != null && (currentMenuBase == null || currentMenuBase.GetType() != newType))
                    currentMenuBase = (Tailbase)Activator.CreateInstance(newType);
            }
            else
            {
                currentMenuBase = null;

                if (newType != null)
                    CurrentTailType = newType;
            }
        }

        /// <summary>
        /// singleplayer and client only, convenient place to clear all lists
        /// </summary>
        /// <param name="player"></param>
        public override void OnEnterWorld()
        {
            TailHandler.PlayerTailList.Clear();
            TailHandler.NpcTailList.Clear();
        }

        public override void PlayerDisconnect()
        {
            TailPlayer tailPlayer = Player.GetModPlayer<TailPlayer>();
            if (tailPlayer.tail != null)
                tailPlayer.tail.Remove();
        }

        public override void Unload()
        {
            tail = null;
        }

        //public override void ModifyDrawLayers(List<PlayerLayer> layers)
        //{
        //    curMenuTexture = playerMenuTexture;
        //    curMenuTexOffset = playerMenuTexOffset;
        //    layers.Insert(0, TailPlayerLayer);
        //}
    }
} 