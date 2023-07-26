using Terraria.ModLoader.Config;

namespace TailLib.Configs
{
	public class Config : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

        //public static bool PixelateTailMode = false;
        //public bool PixelateTails { get { return PixelateTailMode; } set { PixelateTailMode = value; } }

        public enum PixelationLevel
        {
            None,
            On,
            OnAA
        }

        public static PixelationLevel TailPixelationLevel = PixelationLevel.None;
        public PixelationLevel TailPixelation { get { return TailPixelationLevel; } set { TailPixelationLevel = value; } }

        //todo tail rigidness, adjusts the amount of gravity on the chain from 0.25 to 1

        public static bool WireFrameMode = false;
        public bool ShowWireFrame { get { return WireFrameMode; } set { WireFrameMode = value; } }

        //public bool DEBUG_Force_Npc_Enabled_State { get { return TailLib.NpcRenderingActive; } set { TailLib.NpcRenderingActive = value; } }
    }
}