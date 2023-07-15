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
    /// The base class for a new tail.
    /// </summary>
    public abstract class Tailbase
    {
        /// <summary>
        /// This is a reference to the tailInstance this is contained in.
        /// Can be used to access the tail bone instance
        /// this may be null if access from some methods, since a tailbase instance is made on the main menu for vanity drawing
        /// </summary>
        public TailInstance tailInstance;


        /// <summary>
        /// the path the to the texture. Note: do not access tailInstance from here or else it may be null
        /// </summary>
        public virtual string Texture { get => "Blank"; }

        /// <summary>
        /// offsets the tail in the world, this gets automatically flipped based on player direction
        /// </summary>
        public virtual Vector2 WorldOffset => Vector2.Zero;

        /// <summary>
        /// for offsetting the texture
        /// </summary>
        public virtual Vector2 TexPosOffset { get => Vector2.Zero; }

        /// <summary>
        /// for getting the scale / aspect ratio right. Make Sure your settled positions are accurate beforehand
        /// </summary>
        public virtual Vector2 TexSizeOffset { get => Vector2.Zero; }

        /// <summary>
        /// The amount of points in the tail.
        /// Make sure this is at least 2.
        /// </summary>
        public virtual int VertexCount { get => 2; }

        /// <summary>
        /// how many times the physics should be ran. A good range is between 2 and 10. 
        /// Default: 2
        /// </summary>
        public virtual int PhysicsRepetitions { get => 2; }

        /// <summary>
        /// how much the tail should be slowed in the air. 1 is none. A good range is 1 to 1.5.
        /// Default: 1
        /// </summary>
        public virtual float VertexDrag { get => 1f; }

        /// <summary>
        /// width of the tail geometry from center to edge.
        /// Note: if you leave VertexDistance as null please set this
        /// Default: VertexDistance
        /// </summary>
        public virtual float Width { get => VertexDistance; }



        /// <summary>
        /// Distance between every vertex.
        /// VertexDistances can be used instead to set each distance specifically.
        /// </summary>
        public virtual float VertexDistance { get => 5; }
        /// <summary>
        /// Distance between each specific vertex.
        /// VertexDistance can be used instead to set every distance the same.
        /// Note: if this is overridded then VertexDistance is not used.
        /// </summary>
        public virtual float[] VertexDistances { get; }


        /// <summary>
        /// Gravity applied to every vertex.
        /// Assume the player is facing left when setting. (1, 1) points to the bottom right.
        /// It is recommended you use VertexGravityForces instead.
        /// If VertexGravityForces is used, this becomes a multiplier for it.
        /// </summary>
        public virtual Vector2 VertexGravityForce { get => Vector2.One; }
        /// <summary>
        /// Gravity applied to each specific vertex.
        /// Assume the player is facing left when setting. (1, 1) points to the bottom right.
        /// VertexGravityForce can be used to set the same gravity for each.
        /// Note: if this is overridded then VertexGravityForce is not used.
        /// Tip1: It is suggested that you make a graph, and enter each point in here with the previous point being the relative zero point for the current one.
        /// Tip2: Then adjust until you end up with the shape you want, starting with more weight and giving less weight to the points the further you go.
        /// </summary>
        public virtual Vector2[] VertexGravityForces { get; }


        /// <summary>
        /// This is the most important part for having the texture remain undistorted.
        /// After you get the shape you want with VertexGravityForces, allow the tail to stop moving and record the points it settled in.
        /// Do this after you have decided on the distance, and if you change the distance this must be changed too.
        /// Make sure the player is facing left when setting
        /// Note: this can be left null before you have gotten the settled points from in-game.
        /// </summary>
        public virtual Vector2[] SettledPoints { get; }//this could be calculated from the gravity forces



        /// <summary>
        /// Helper method that converts the coords to tilecoords and multiplies by tailInstance.alpha
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        private Color GetLightColor(Vector2 worldPos) => Lighting.GetColor((int)(worldPos.X / 16), (int)(worldPos.Y / 16)) * tailInstance.alpha;



        /// <summary>
        /// For drawing sprites at every vertex
        /// </summary>
        /// <param name="spriteBatch"></param>
        public virtual void DrawSprites(SpriteBatch spriteBatch) { }

        public virtual Vector2 DrawMenuOffset() => Vector2.Zero;
        public virtual bool PreDrawMenuLayer(ref PlayerDrawSet drawInfo) => true;
        public virtual void PostDrawMenuLayer(ref PlayerDrawSet drawInfo) { }



        /// <summary>
        /// for if the spine outline is enabled. disabled by default.
        /// if you plan to enable/disable this at runtime, make sure this is true when the TailBase instance is created
        /// as the buffers for this are not created if this is false.
        /// Note: there is no predraw for this since it is geometry, and should be done in PreDrawGeometry
        /// </summary>
        public virtual bool SpineEnabled => false;
        /// <summary>
        /// the color its drawn in, if you pass your own color consider multiplying by tailInstance.alpha
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual Color SpineColor(int index) => Color.White; // GetLightColor(tailInstance.tailBones.ropeSegments[index].posNow);



        /// <summary>
        /// if the geometry should draw.
        /// return false to draw your own in PreDrawGeometry.
        /// if you plan to enable/disable this at runtime, make sure this is true when the TailBase instance is created
        /// as the buffers for this are not created if this is false.
        /// </summary>
        public virtual bool GeometryEnabled => true;
        /// <summary>
        /// For drawing your own geometry
        /// </summary>
        /// <param name="basicEffect">wip</param>
        /// <param name="basicEffectPass"></param>
        /// <param name="graphicsDevice"></param>
        public virtual void PreDrawGeometry(ArmorShaderData vanillaShader, Entity entity, BasicEffect basicEffect, EffectPass basicEffectPass, GraphicsDevice graphicsDevice) { }
        /// <summary>
        /// the color its drawn in, if you pass your own color consider multiplying by tailInstance.alpha
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual Color GeometryColor(int index) => GetLightColor(tailInstance.tailBones.ropeSegments[index].posNow);


        /// <summary>
        /// Allows you to do stuff when this is updated
        /// Return false to stop the tail from updating
        /// Note: tailInstance has a few helper extension methods you can call from here for some built in functionallity
        /// </summary>
        public virtual bool PreUpdate() { return true; }
        /// <summary>
        /// Use this for doing stuff after the tail updates, like changing the direction
        /// Called even if PreUpdate returns false
        /// </summary>
        public virtual void PostUpdate() { }

        /// <summary>
        /// which direction should the sprite face
        /// by default is based on which side of the player the end of the tail is on
        /// </summary>
        /// <param name="tailInstance"></param>
        public virtual bool SpriteDirection() =>
            tailInstance.tailBones.ropeSegments[0].posNow.X > tailInstance.tailBones.ropeSegments[tailInstance.tailBones.segmentCount - 1].posNow.X;
    }

}
