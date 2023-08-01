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
using static TailLib.TailSystem;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using static Terraria.GameContent.Animations.Actions;
using Microsoft.VisualBasic;
using log4net;

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

        public int DyeItemType = -1;

        public void ResetTail()
        {
            if (currentlyActive && tail != null)
            {
                tail.ResetTailBones(Player.MountedCenter);
            }
        }

        public override void PreUpdate()
        {
            //Main.LocalPlayer.gravity = 0f;
        }

        public override void PostUpdate()
        {
            if (currentlyActive)
            {
                if (tail != null)
                {
                    //the cull check is here instead of in tail.update so that this extra logic can be skipped, but this extra logic isnt much slowdown so it can be moved if needed
                    if (!(CullRect.Contains(Player.Center.ToPoint()) || CullRect.Contains(tail.tailBones.startPoint.ToPoint())))
                        return;//returns if offscreen

                    //if current tailType is different than the type of the active tail
                    if (_currentTailType != null && (tail.tailBase.GetType() != _currentTailType || (!previouslyActive && currentlyActive)))
                    {
                        tail.Remove();
                        tail = new TailInstance(_currentTailType, Player.MountedCenter, Layer.Player, Player, (int)Player.gravDir);
                    }

                    tail.tailBones.Active = !Player.dead && currentlyActive;

                    Vector2 roundedPos = new Vector2(
                        MathF.Round(Player.MountedCenter.X, MidpointRounding.ToNegativeInfinity),
                        MathF.Round(Player.MountedCenter.Y, MidpointRounding.AwayFromZero));

                    tail.Update(roundedPos + new Vector2(/*fixes centering issue*/ -0.5f, Player.gfxOffY), Player.FacingDirection());
                    tail.alpha = (255 - Player.immuneAlpha) * 0.003921568627f;//for the blinking when damaged
                    tail.armorShaderData = GameShaders.Armor.GetShaderFromItemId(DyeItemType);//GameShaders.Armor.GetShaderFromItemId(ItemID.MushroomDye);
                }
                else if (_currentTailType != null)
                    tail = new TailInstance(_currentTailType, Player.MountedCenter, Layer.Player, Player, (int)Player.gravDir);
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
        /// method called in singleplayer and client only (?), convenient place to clear all lists
        /// </summary>
        /// <param name="player"></param>
        public override void OnEnterWorld()
        {
            TailSystem.GlobalPlayerTailList.Clear();
            TailSystem.GlobalNpcTailList.Clear();
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