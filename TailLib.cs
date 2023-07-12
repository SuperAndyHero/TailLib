using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace TailLib
{
	internal class TailLib : Mod
	{
        public override void Load()
        {
            Main.QueueMainThreadAction(() => SetupEffect());
            TailHandler.Load();
        }

        //TODO
        //tail screen lag for every player but client's player//try diff between localPlayer pos and old pos?
        //cont: in TailPlayer add a check if player is LocalPlayer and shift the position, or other way around (to avoid extra layer)

        //Fixed?
        //fixed with PlayerDisconnect(Player player) in TailPlayer 
        ////list not cleared when player leaves
        ////npc tail screenlag when upsidedown (only vertically)

        public static BasicEffect BasicEffect;
        private static void SetupEffect()
        {
            if (!Main.dedServ)
            {
                BasicEffect = new BasicEffect(Main.graphics.GraphicsDevice)
                {
                    VertexColorEnabled = true,
                };
                BasicEffect.Projection = Matrix.CreateOrthographic(Main.screenWidth, Main.screenHeight, 0, 1000);
            }
        }
    }
}