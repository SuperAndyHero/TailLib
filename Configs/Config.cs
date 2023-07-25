using Terraria.ModLoader.Config;

namespace TailLib.Configs
{
	public class Config : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

        public static bool PixelateTailMode = false;
        public bool PixelateTails { get { return PixelateTailMode; } set { PixelateTailMode = value; } }//players only

        public static bool WireFrameMode = false;
        public bool ShowWireFrame { get { return WireFrameMode; } set { WireFrameMode = value; } }
    }
}