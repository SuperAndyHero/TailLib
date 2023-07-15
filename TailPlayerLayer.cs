using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using static Terraria.ModLoader.PlayerDrawLayer;

namespace TailLib
{
    public class TailPlayerLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Tails);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            //this is for drawing the tail on the menu
            if (Main.gameMenu && drawInfo.drawPlayer.TryGetModPlayer<TailPlayer>(out TailPlayer tailPlayer))
            {
                if(tailPlayer.currentMenuBase != null)
                {
                    //if (drawInfo.drawPlayer.dead) return;//player is never dead on main menu 
                    var curBase = tailPlayer.currentMenuBase;
                    if (curBase.PreDrawMenuLayer(ref drawInfo))
                    {
                        Texture2D texture = ModContent.Request<Texture2D>(curBase.Texture).Value;
                        int frameSize = texture.Height / 20;//was left over from my orignal player layer base, breaks if removed

                        //if this is changed in future, PlayerDrawSet has a center property
                        Vector2 playerMenuTexOffset = new Vector2((curBase.WorldOffset.X - curBase.TexPosOffset.X) - (drawInfo.drawPlayer.width / 2), (curBase.WorldOffset.Y - texture.Height) - curBase.TexPosOffset.Y) - new Vector2(3, 0) + curBase.DrawMenuOffset();

                        DrawData data = new DrawData(texture, (drawInfo.Center - Main.screenPosition) + (playerMenuTexOffset * 0.5f), null, Color.White, 0f, new Vector2(texture.Width, frameSize), 1f, SpriteEffects.FlipHorizontally, 0);
                        data.shader = GameShaders.Armor.GetShaderIdFromItemId(tailPlayer.DyeItemType);
                        drawInfo.DrawDataCache.Add(data);
                    }
                    curBase.PostDrawMenuLayer(ref drawInfo);
                }
            }
        }
    }
}
