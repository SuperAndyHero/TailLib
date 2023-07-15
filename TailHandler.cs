using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using static TailLib.TailHandler;
using static Terraria.GameContent.Animations.Actions;

namespace TailLib
{
    /// <summary>
    /// this handles everything to do with tails
    /// </summary>
    public static class TailHandler
    {
        /// <summary>
        /// List of every tail instance on players
        /// Note: updating is done locally and not through this
        /// </summary>
        public static List<TailInstance> PlayerTailList = new List<TailInstance>();
        /// <summary>
        /// List of every tail instace on npcs
        /// </summary>
        public static List<TailInstance> NpcTailList = new List<TailInstance>();

        /// <summary>
        /// The render target the player tails are drawn to, drawn just before players
        /// </summary>
        public static RenderTarget2D PlayerTailTarget = Main.dedServ ? null : new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        /// <summary>
        /// The render target npc tails are drawn to, drawn just before npcs
        /// </summary>
        public static RenderTarget2D NpcTailTarget = Main.dedServ ? null : new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        
        /// <summary>
        /// The layers you can have the tail draw on
        /// </summary>
        public enum Layer : ushort
        {
            Player,
            Npc
        }

        /// <summary>
        /// Gets a reference to a specific list via tha passed layer
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static List<TailInstance> GetLayerList(Layer layer)
        {
            switch (layer)
            {
                case Layer.Player:
                    return PlayerTailList;
                default:
                    return NpcTailList;
            }
        }

        internal static void Load()
        {
            //On.Terraria.DataStructures.PlayerDrawLayers.DrawPlayer_RenderAllLayers += DrawTailTargetPlayer;
            Terraria.On_Main.DrawNPCs += DrawTailTargetNpc;
            Main.OnPreDraw += DrawTails;
            Terraria.On_Main.DoUpdate += Main_DoUpdate;
        }

        private static void Main_DoUpdate(Terraria.On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
        {
            foreach (NPC npc in TailGlobalNPC.RemovalQueue)
                TailGlobalNPC.ActiveTailNpcsList.Remove(npc);

            TailGlobalNPC.RemovalQueue.Clear();

            foreach (KeyValuePair<NPC, TailInstance> pair in TailGlobalNPC.ActiveTailNpcsList)
            {
                if (!pair.Key.active)
                {
                    pair.Value.Remove();
                    TailGlobalNPC.RemovalQueue.Add(pair.Key);
                }
            }
            orig(self, ref gameTime);
        }

        private static void DrawTails(GameTime obj)
        {
            GraphicsDevice graphics = Main.instance.GraphicsDevice;

            graphics.SetRenderTarget(PlayerTailTarget);
            graphics.Clear(Color.Transparent);

            foreach (TailInstance tail in PlayerTailList)
                tail.DrawGeometry();

            //uses immediate so that the shader can be changed between each sprite
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, default, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.ZoomMatrix);
            
            foreach (TailInstance tail in PlayerTailList)
                tail.DrawSprites(Main.spriteBatch);
            Main.spriteBatch.End();

            graphics.SetRenderTarget(null);




            graphics.SetRenderTarget(NpcTailTarget);
            graphics.Clear(Color.Transparent);

            foreach (TailInstance tail in NpcTailList)
                tail.DrawGeometry();

            Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.ZoomMatrix);
            foreach (TailInstance tail in NpcTailList)
                tail.DrawSprites(Main.spriteBatch);
            Main.spriteBatch.End();

            graphics.SetRenderTarget(null);
        }

        //broken due to pixelation issues, moved to npc draw hook
        //private static void DrawTailTargetPlayer(On.Terraria.DataStructures.PlayerDrawLayers.orig_DrawPlayer_RenderAllLayers orig, ref Terraria.DataStructures.PlayerDrawSet drawinfo)
        //{
        //    if (!Main.gameMenu)
        //    {
        //        //Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default, default);
        //        //Main.spriteBatch.Draw(PlayerTailTarget, new Rectangle(0, 0, PlayerTailTarget.Width, PlayerTailTarget.Height), null, Color.White, 0, default, Main.LocalPlayer.gravDir == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically, default);
        //        //Main.spriteBatch.End();
        //    }
        //    orig(ref drawinfo);
        //}

        //draws both player and npc tails to the world
        private static void DrawTailTargetNpc(Terraria.On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
        {
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(default, null, SamplerState.PointClamp, null, null, null);
            Main.spriteBatch.Draw(NpcTailTarget, new Rectangle(0, 0, PlayerTailTarget.Width, PlayerTailTarget.Height), null, Color.White, 0, default, Main.LocalPlayer.gravDir == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically, default);
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            orig(self, behindTiles);
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(default, null, SamplerState.PointClamp, null, null, null);
            Main.spriteBatch.Draw(PlayerTailTarget, new Rectangle(0, 0, PlayerTailTarget.Width, PlayerTailTarget.Height), null, Color.White, 0, default, Main.LocalPlayer.gravDir == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically, default);
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}