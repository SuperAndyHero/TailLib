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

        public override void PreUpdate()
        {
            if (currentlyActive)
            {
                if (tail != null)
                {
                    if (_currentTailType != null && (tail.tailBase.GetType() != _currentTailType || (!previouslyActive && currentlyActive)))
                    {
                        tail.Remove();
                        tail = new TailInstance(_currentTailType, Player.Center, Layer.Player, Player.FacingDirection());
                    }

                    tail.tailBones.Active = !Player.dead && currentlyActive;

                    tail.Update(Player.Center + new Vector2(0, Player.gfxOffY), Player.FacingDirection());
                    tail.alpha = (255 - Player.immuneAlpha) * 0.003921568627f;//for the blinking when damaged
                }
                else if (_currentTailType != null)
                    tail = new TailInstance(_currentTailType, Player.Center, Layer.Player, Player.FacingDirection());
            }
        }

        public string playerMenuTexture = null;
        public Vector2 playerMenuTexOffset = Vector2.Zero;
        public override void ResetEffects()
        {
            if (Main.gameMenu)
            {
                Type curType = null;

                foreach (Item item in Player.armor)
                    if (item.ModItem is TailItem)
                        curType = (item.ModItem as TailItem).TailType;

                Tailbase curBase = null;
                if(curType != null)
                    curBase = (Tailbase)Activator.CreateInstance(curType);

                if (curBase != null)
                {
                    playerMenuTexture = curBase.Texture;
                    Texture2D texture = ModContent.Request<Texture2D>(playerMenuTexture).Value;
                    playerMenuTexOffset = new Vector2(2 * curBase.WorldOffset.X, (curBase.WorldOffset.Y - (curBase.TexPosOffset.Y)) - texture.Height) - new Vector2(1, -1);
                }
            }
            

            if (previouslyActive && !currentlyActive /*&& null check*/)
                tail.Remove();

            previouslyActive = currentlyActive;
            currentlyActive = false;
        }

        /// <summary>
        /// singleplayer and client only, convenient place to clear all lists
        /// </summary>
        /// <param name="player"></param>
        public override void OnEnterWorld(Player player)
        {
            TailHandler.PlayerTailList.Clear();
            TailHandler.NpcTailList.Clear();
        }

        public override void PlayerDisconnect(Player player)
        {
            TailPlayer tailPlayer = player.GetModPlayer<TailPlayer>();
            if (tailPlayer.tail != null)
                tailPlayer.tail.Remove();
        }

        //public override void ModifyDrawLayers(List<PlayerLayer> layers)
        //{
        //    curMenuTexture = playerMenuTexture;
        //    curMenuTexOffset = playerMenuTexOffset;
        //    layers.Insert(0, TailPlayerLayer);
        //}
    }
} 