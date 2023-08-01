using log4net.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Core.Platforms;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TailLib.Configs;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using static TailLib.TailSystem;
using static Terraria.GameContent.Animations.Actions;

namespace TailLib
{
    /// <summary>
    /// this handles everything to do with tails
    /// </summary>
    public class TailSystem : ModSystem
    {
        /// <summary>
        /// culls everything outside this rectangle
        /// </summary>
        public static Rectangle CullRect = new Rectangle();

        /// <summary>
        /// List of every tail instance on players
        /// Note: updating is done locally and not through this
        /// </summary>
        public static List<TailInstance> GlobalPlayerTailList = new List<TailInstance>();
        /// <summary>
        /// List of every tail instace on npcs
        /// </summary>
        public static List<TailInstance> GlobalNpcTailList = new List<TailInstance>();

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
                    return GlobalPlayerTailList;
                default:
                    return GlobalNpcTailList;
            }
        }

        /// <summary>
        /// The render target the player tails are drawn to, drawn just before players
        /// </summary>
        public RenderTarget2D PlayerTailTarget;
        /// <summary>
        /// The render target npc tails are drawn to, drawn just before npcs
        /// </summary>
        public RenderTarget2D NpcTailTarget;

        /// <summary>
        /// The layers you can have the tail draw on
        /// </summary>
        public enum Layer : ushort
        {
            Player,
            Npc
        }


        private delegate void orig_ModifyTransformMatrix(ref SpriteViewMatrix Transform);

        public override void Load()
        {
            Main.QueueMainThreadAction(() => 
                PlayerTailTarget = Main.dedServ ? null : new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents));

            CheckNpcTarget(TailLib.NpcRenderingActive);//likely not needed but allows it to load if the default is true, and has no cost if false

            Terraria.On_Player.Teleport += On_Player_Teleport;
            Terraria.On_Player.Spawn += On_Player_Spawn;
            //On.Terraria.DataStructures.PlayerDrawLayers.DrawPlayer_RenderAllLayers += DrawTailTargetPlayer;
            Terraria.On_Main.DrawNPCs += DrawTailTarget;//draws both rendertargets to the screen
            Terraria.On_Main.DoUpdate += Main_DoUpdate;//updates active npc tails

            Type SystemLoaderType = typeof(Terraria.ModLoader.SystemLoader);
            MethodInfo detourMethod = SystemLoaderType.GetMethod("ModifyTransformMatrix", BindingFlags.Public | BindingFlags.Static);

            //var a = new Hook(detourMethod, ModifyTransformMatrix_Detour);
            MonoModHooks.Add(detourMethod, ModifyTransformMatrix_Detour);

            //Terraria.On_Main.DoDraw_UpdateCameraPosition += On_Main_DoDraw_UpdateCameraPosition;//remove
        }

        private void ModifyTransformMatrix_Detour(orig_ModifyTransformMatrix orig, ref SpriteViewMatrix Transform)
        {
            orig(ref Transform);
            RenderTails();//this hook position fixes camera delay and zoom delay
        }

        /// <summary>
        /// keeps the npc target from loading if not needed
        /// </summary>
        /// <param name="newActiveState"></param>
        public void CheckNpcTarget(bool newActiveState)
        {
            if (Main.dedServ)
                return;

            if(newActiveState && NpcTailTarget == null)
            {
                Main.QueueMainThreadAction(() => 
                    NpcTailTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents));
            }
            else if (!newActiveState && NpcTailTarget != null)
            {
                NpcTailTarget = null;
            }
        }

        private void On_Player_Spawn(On_Player.orig_Spawn orig, Player self, PlayerSpawnContext context)
        {
            orig(self, context);
            TailPlayer modplayer = self.GetModPlayer<TailPlayer>();
            modplayer.ResetTail();//method does nothing if the tail is not active
        }

        private void On_Player_Teleport(On_Player.orig_Teleport orig, Player self, Vector2 newPos, int Style, int extraInfo)
        {
            orig(self, newPos, Style, extraInfo);
            TailPlayer modplayer = self.GetModPlayer<TailPlayer>();
            modplayer.ResetTail();//method does nothing if the tail is not active
        }

        //private void On_Main_DoDraw_UpdateCameraPosition(On_Main.orig_DoDraw_UpdateCameraPosition orig)
        //{
        //    //this hook is used due to a bug when moving the camera seperate from the player
        //    orig();
        //    //RenderTails();//uses other hook since it also fixes zoom
        //}
        private bool LastMouseL = false;
        private void Main_DoUpdate(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
        {
            if (Main.mouseLeft &! LastMouseL)
            {
                Main.LocalPlayer.position.X -= 0.1f;
            }
            LastMouseL = Main.mouseLeft;

            CullRect = new Rectangle((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);
            CullRect.Inflate(Main.screenWidth, Main.screenHeight);//adds the values to each side of rect (resulting rect ends up with a new width of old value + new value * 2)

            if (TailLib.NpcRenderingActive)
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
            }
            orig(self, ref gameTime);
        }

        private void RenderTails()//draws tails to the rendertargets, just needs a time when no rendertarget or spritebatch is active
        {
            GraphicsDevice graphics = Main.instance.GraphicsDevice;
            graphics.SamplerStates[0] = SamplerState.PointClamp;//position in main.draw causes this to be linear by default

            //PLAYERS
            graphics.SetRenderTarget(PlayerTailTarget);
            graphics.Clear(Color.Transparent);

            foreach (TailInstance tail in GlobalPlayerTailList)
            {
                tail.DrawGeometry(false);
                if (Configs.Config.WireFrameMode)
                    tail.DrawGeometry(true);
            }

            //uses immediate so that the shader can be changed between each sprite
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, default, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.TransformationMatrix);//may have to be changed back to zoomatrix? no change observed
            
            foreach (TailInstance tail in GlobalPlayerTailList)
                tail.DrawSprites(Main.spriteBatch);
            Main.spriteBatch.End();

            graphics.SetRenderTarget(null);



            if (!TailLib.NpcRenderingActive)//stop here if npcs are not being rendered
                return;

            //NPCS
            graphics.SetRenderTarget(NpcTailTarget);
            graphics.Clear(Color.Transparent);

            foreach (TailInstance tail in GlobalNpcTailList)
            {
                tail.DrawGeometry(false);
                if(Configs.Config.WireFrameMode)
                    tail.DrawGeometry(true);
            }

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, default, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.TransformationMatrix);
            foreach (TailInstance tail in GlobalNpcTailList)
                tail.DrawSprites(Main.spriteBatch);
            Main.spriteBatch.End();

            graphics.SetRenderTarget(null);
            graphics.SamplerStates[0] = SamplerState.LinearClamp;//returns to previous sampler to prevent possible issues
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
        private void DrawTailTarget(Terraria.On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
        {
            TailLib.PixelationEffect.Parameters["pixelation"].SetValue(Main.GameZoomTarget);
            TailLib.PixelationEffect.Parameters["resolution"].SetValue(new Vector2(Main.screenWidth / 2, Main.screenHeight / 2));

            bool pixelation = Config.TailPixelationLevel != Config.PixelationLevel.None;

            if (TailLib.NpcRenderingActive)
            {
                Main.spriteBatch.End();

                if(pixelation)
                    Main.spriteBatch.Begin(default, null, 
                        Config.TailPixelationLevel == Config.PixelationLevel.OnAA ? SamplerState.LinearClamp : SamplerState.PointClamp, 
                        null, null, TailLib.PixelationEffect);
                else
                    Main.spriteBatch.Begin(default, null, SamplerState.PointClamp, null, null, null);

                Main.spriteBatch.Draw(NpcTailTarget, new Rectangle(0, 0, PlayerTailTarget.Width, PlayerTailTarget.Height + (pixelation ? 2 : 0)), null, Color.White, 0, default, Main.LocalPlayer.gravDir == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically, default);
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            orig(self, behindTiles);

            Main.spriteBatch.End();

            if (pixelation)
                Main.spriteBatch.Begin(default, null,
                    Config.TailPixelationLevel == Config.PixelationLevel.OnAA ? SamplerState.LinearClamp : SamplerState.PointClamp, 
                    null, null, TailLib.PixelationEffect);
            else
                Main.spriteBatch.Begin(default, null, SamplerState.PointClamp, null, null, null);

            Main.spriteBatch.Draw(PlayerTailTarget, new Rectangle(0, 0, PlayerTailTarget.Width, PlayerTailTarget.Height + (pixelation ? 2 : 0)), null, Color.White, 0, default, Main.LocalPlayer.gravDir == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically, default);
            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}