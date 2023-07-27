using Terraria.ModLoader.Config;

namespace TailLib.Configs
{
	public class Config : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

        public enum PixelationLevel
        {
            None,
            On,
            OnAA
        }

        public static PixelationLevel TailPixelationLevel = PixelationLevel.None;

        [DrawTicks]
        public PixelationLevel TailPixelationLevelConfig { get { return TailPixelationLevel; } set { TailPixelationLevel = value; } }



        public static float TailRigidness = 1f;

        [Range(0.3f, 1f)]
        [DrawTicks]
        [Increment(0.1f)]
        public float TailRigidnessConfig { get { return TailRigidness; } set { TailRigidness = value; } }



        public static bool WireFrameMode = false;
        public bool WireFrameModeConfig { get { return WireFrameMode; } set { WireFrameMode = value; } }

        //public bool DEBUG_Force_Npc_Enabled_State { get { return TailLib.NpcRenderingActive; } set { TailLib.NpcRenderingActive = value; } }
    }
}