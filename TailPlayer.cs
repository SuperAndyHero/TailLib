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
        public TailInstance tail = null;

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
                        tail = new TailInstance(_currentTailType, player.Center, Layer.Player, player.FacingDirection());

                    tail.tailBones.Active = !player.dead && currentlyActive;

                    tail.Update(player.Center + new Vector2(0, player.gfxOffY), player.FacingDirection());
                    tail.alpha = (255 - player.immuneAlpha) * 0.003921568627f;//for the blinking when damaged
                }
                else if (_currentTailType != null)
                    tail = new TailInstance(_currentTailType, player.Center, Layer.Player, player.FacingDirection());
            }
        }

        private string playerMenuTexture = null;
        private Vector2 playerMenuTexOffset = Vector2.Zero;
        public override void ResetEffects()
        {
            if (Main.gameMenu)
            {
                Type curType = null;

                foreach (Item item in player.armor)
                    if (item.modItem is TailItem)
                        curType = (item.modItem as TailItem).TailType;

                Tailbase curBase = null;
                if(curType != null)
                    curBase = (Tailbase)Activator.CreateInstance(curType);

                if (curBase != null)
                {
                    playerMenuTexture = curBase.Texture;
                    playerMenuTexOffset = new Vector2(2 * curBase.WorldOffset.X, curBase.WorldOffset.Y);
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

        internal static string curMenuTexture = null;
        internal static Vector2 curMenuTexOffset = Vector2.Zero;
        internal static readonly PlayerLayer TailPlayerLayer = new PlayerLayer("TailLib", "TailLayer", PlayerLayer.MiscEffectsBack, delegate (PlayerDrawInfo drawInfo)//just for the main menu
        {
            Player drawPlayer = drawInfo.drawPlayer;
            if (Main.gameMenu && curMenuTexture != null)
            {
                //if (drawInfo.drawPlayer.dead) return;//player is never dead on main menu 
                Texture2D texture = ModContent.GetTexture(curMenuTexture);
                int frameSize = texture.Height / 20;//what??? was left over from my orignal player layer base, breaks if removed
                DrawData data = new DrawData(texture, (drawInfo.position - Main.screenPosition) + (curMenuTexOffset * 0.5f), null, Color.White, 0f, new Vector2(texture.Width, frameSize), 1f, SpriteEffects.FlipHorizontally, 0);
                Main.playerDrawData.Add(data);
            }
        });

        public override void ModifyDrawLayers(List<PlayerLayer> layers)
        {
            curMenuTexture = playerMenuTexture;
            curMenuTexOffset = playerMenuTexOffset;
            layers.Insert(0, TailPlayerLayer);
        }
    }
} 