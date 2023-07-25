using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace TailLib
{
	internal class TailLib : Mod
	{
        private static bool _NpcRenderingActive = false;
        /// <summary>
        /// If npcs should have tails rendered, set this to true during load if your mod needs tail rendering on npcs.
        /// Prevents the npc rendertarget from being loaded if not in use.
        /// </summary>
        public static bool NpcRenderingActive
        {
            //most methods that deal with npcs check this (they could all be moved to a RT null check instead, could be needed since enabling is a tick behind)
            get { return _NpcRenderingActive; }
            set
            {
                ModContent.GetInstance<TailSystem>().CheckNpcTarget(value);
                _NpcRenderingActive = value;
            }
        }

        public static BasicEffect BasicEffect;

        public override void Load()
        {
            Main.QueueMainThreadAction(() => SetupEffect());
        }

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