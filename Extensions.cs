using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using static TailLib.TailHandler;

namespace TailLib
{
    /// <summary>
    /// Helper Extensions
    /// </summary>
    [ExtendsFromMod]
    public static class Extensions
    {
        /// <summary>
        /// Adds a tail to a npc, suggested that you call this in setdefaults
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="type"></param>
        public static TailInstance SetTail(this NPC npc, Type type)
        {
            TailGlobalNPC globalNpc = npc.GetGlobalNPC<TailGlobalNPC>();
            globalNpc.tail = new TailInstance(type, npc.Center, Layer.Npc, npc, 1);
            globalNpc.tailActive = true;
            TailGlobalNPC.ActiveTailNpcsList.Add(npc, globalNpc.tail);
            return globalNpc.tail;
        }

        /// <summary>
        /// gets the tailactive value for said npc
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        public static bool TailActive(this NPC npc) => 
            npc.GetGlobalNPC<TailGlobalNPC>().tailActive;

        /// <summary>
        /// sets the tail active value for said npc
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="value"></param>
        public static void TailActive(this NPC npc, bool value) => 
            npc.GetGlobalNPC<TailGlobalNPC>().tailActive = value;

        /// <summary>
        /// combines player.direction and player.gravDir
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static Vector2 FacingDirection(this Player player) => 
            new Vector2(player.direction, player.gravDir);

        #region tail helper extension methods
        /// <summary>
        /// This gives the tail upwards momentum when turning (when the player direction and the side the last tail segement are on dont match
        /// </summary>
        /// <param name="tailInstance"></param>
        /// <param name="strength"></param>
        public static void UpwardsTurn(this TailInstance tailInstance, float strength = 2)
        {
            float startLocation = tailInstance.tailBones.ropeSegments[0].ScreenPos.X;
            int directionMult = startLocation > tailInstance.tailBones.ropeSegments[tailInstance.tailBones.segmentCount - 1].ScreenPos.X ? -1 : 1;
            if (directionMult != tailInstance.facingDirection.X)
                tailInstance.tailBones.ropeSegments[tailInstance.tailBones.segmentCount - 1].posNow.Y -= (strength * tailInstance.facingDirection.Y);
        }

        /// <summary>
        /// Wobbles the tail when idle or running. is run on the last segment in the tail. Use the other overload to run it on other segments.
        /// </summary>
        /// <param name="tailInstance">passed instance to run this on</param>
        /// <param name="freq">frequency of the cycles</param>
        /// <param name="runningFreq">frequency of the cycles when running</param>
        /// <param name="amplitude">ampiltude of the cycles</param>
        /// <param name="runningAmplitude">amplitude of the cycles when running</param>
        /// <param name="multX">multiplier for the X axis. For tails that rest vertically</param>
        /// <param name="multY">multiplier for the Y axis. For tails that rest horizontally</param>
        public static void TailWobble(this TailInstance tailInstance, float freq = 0.05f, float runningFreq = 0.2f, float amplitude = 0.25f, float runningAmplitude = 0.6f, float multX = 1, float multY = 0)
        {
            int index = tailInstance.tailBones.segmentCount - 1;
            tailInstance.TailWobble(index - 1, index, freq, runningFreq, amplitude, runningAmplitude, multX, multY);
        }

        /// <summary>
        /// Wobbles the tail when idle or running.
        /// </summary>
        /// <param name="tailInstance">passed instance to run this on</param>
        /// <param name="startIndex">first index that is moved</param>
        /// <param name="endIndex">last index that is moved</param>
        /// <param name="freq">frequency of the cycles</param>
        /// <param name="runningFreq">frequency of the cycles when running</param>
        /// <param name="amplitude">ampiltude of the cycles</param>
        /// <param name="runningAmplitude">amplitude of the cycles when running</param>
        /// <param name="multX">multiplier for the X axis. For tails that rest vertically</param>
        /// <param name="multY">multiplier for the Y axis. For tails that rest horizontally</param>
        /// <param name="runThreshold">speed threshold for when the player is running</param> 
        public static void TailWobble(this TailInstance tailInstance, int startIndex, int endIndex, float freq = 0.05f, float runningFreq = 0.2f, float amplitude = 1f, float runningAmplitude = 1.2f, float multX = 1, float multY = 0, float runThreshold = 0.1f)
        {
            for(int i = startIndex; i < endIndex; i++)
            {
                float str = Math.Abs(tailInstance.tailBones.ropeSegments[0].posOld.X - tailInstance.tailBones.ropeSegments[0].posNow.X);
                float sin = str > 0.1f ? (float)Math.Sin((float)Main.GameUpdateCount * runningFreq) * runningAmplitude : 
                                            (float)Math.Sin((float)Main.GameUpdateCount * freq) * amplitude;
                tailInstance.tailBones.ropeSegments[i].posNow += new Vector2(-sin * multX, sin * multY);
            }
        }

        #endregion

        public static Vector3 Transform(this Vector3 vec3, Matrix matrix) => 
            Vector3.Transform(vec3, matrix);
    }
}