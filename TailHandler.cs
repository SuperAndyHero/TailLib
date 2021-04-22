
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using static TailLib.TailHandler;

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
            On.Terraria.Main.DrawPlayers += DrawTailTargetPlayer;
            On.Terraria.Main.DrawNPCs += DrawTailTargetNpc;
            Main.OnPreDraw += DrawTails;
        }

        private static void DrawTails(GameTime obj)
        {
            GraphicsDevice graphics = Main.instance.GraphicsDevice;

            graphics.SetRenderTarget(PlayerTailTarget);
            graphics.Clear(Color.Transparent);

            foreach (TailInstance tail in PlayerTailList)
                tail.DrawGeometry(TailLib.basicEffect, TailLib.basicEffect.CurrentTechnique.Passes[0], graphics);

            Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.ZoomMatrix);
            foreach (TailInstance tail in PlayerTailList)
                tail.DrawSprites(Main.spriteBatch);
            Main.spriteBatch.End();

            graphics.SetRenderTarget(null);



            graphics.SetRenderTarget(NpcTailTarget);
            graphics.Clear(Color.Transparent);

            foreach (TailInstance tail in NpcTailList)
                tail.DrawGeometry(TailLib.basicEffect, TailLib.basicEffect.CurrentTechnique.Passes[0], graphics);

            Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.ZoomMatrix);
            foreach (TailInstance tail in NpcTailList)
                tail.DrawSprites(Main.spriteBatch);
            Main.spriteBatch.End();

            graphics.SetRenderTarget(null);
        }

        private static void DrawTailTargetPlayer(On.Terraria.Main.orig_DrawPlayers orig, Main self)
        {
            Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default, default);
            Main.spriteBatch.Draw(PlayerTailTarget, new Rectangle(0, 0, PlayerTailTarget.Width, PlayerTailTarget.Height), null, Color.White, 0, default, Main.LocalPlayer.gravDir == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically, default);
            Main.spriteBatch.End();
             orig(self);
        }

        private static void DrawTailTargetNpc(On.Terraria.Main.orig_DrawNPCs orig, Main self, bool behindTiles)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(default, null, SamplerState.PointClamp, null, null, null);
            Vector2 offset = Main.screenLastPosition - Main.screenPosition;//this mostly fixes a weird bug when the screen moves (Main.LocalPlayer.oldPosition - Main.LocalPlayer.position; works too)
            Main.spriteBatch.Draw(NpcTailTarget, new Rectangle((int)offset.X, (int)(offset.Y * Main.LocalPlayer.gravDir), NpcTailTarget.Width, NpcTailTarget.Height), null, Color.White, 0, default, Main.LocalPlayer.gravDir == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically, default);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, Main.GameViewMatrix.TransformationMatrix);
            orig(self, behindTiles);
        }
    }

    /// <summary>
    /// The base class for a new tail.
    /// </summary>
    public abstract class Tailbase
    {
        /// <summary>
        /// This is a reference to the tailInstance this is contained in.
        /// Can be used to access
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
        public virtual int Width { get => VertexDistance; }



        /// <summary>
        /// Distance between every vertex.
        /// VertexDistances can be used instead to set each distance specifically.
        /// </summary>
        public virtual int VertexDistance { get => 5; }
        /// <summary>
        /// Distance between each specific vertex.
        /// VertexDistance can be used instead to set every distance the same.
        /// Note: if this is overridded then VertexDistance is not used.
        /// </summary>
        public virtual int[] VertexDistances { get; }


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
        public virtual Color SpineColor(int index) => Color.Blue; // GetLightColor(tailInstance.tailBones.ropeSegments[index].posNow);



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
        /// <param name="effect"></param>
        /// <param name="pass"></param>
        /// <param name="graphicsDevice"></param>
        public virtual void PreDrawGeometry(BasicEffect effect, EffectPass pass, GraphicsDevice graphicsDevice) { }
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
    }

    /// <summary>
    /// This class stores everything needed have a working tail, as an instance is made for anything with a tail.
    /// It stores the TailBase instance, the verlet chain instace, the vertex/index buffers, and other info like direction and alpha
    /// </summary>
    public class TailInstance
    {
        /// <summary>
        /// The TailBase instance, has all the values to set the tail up
        /// </summary>
        public Tailbase tailBase;
        /// <summary>
        /// The VerletChainInstance, where you can access anything about the verlet chain
        /// </summary>
        public VerletChainInstance tailBones;
        /// <summary>
        /// Direction can either be 1 or -1 for Left or Right respectively
        /// </summary>
        public Vector2 facingDirection;
        /// <summary>
        /// The alpha value, this ranges from 0 to 1.
        /// Multiply your color by this to have it be effected by player immune opacity
        /// </summary>
        public float alpha = 1f;

        /// <summary>
        /// The layer this is drawn on
        /// You can check this to see if this tail is on a player/npc/etc
        /// </summary>
        public readonly Layer layer;

        private readonly VertexBuffer geometryBuffer;
        private readonly IndexBuffer geometryIndexBuffer;
        private readonly VertexBuffer spineBuffer;

        private readonly Vector2[] texCoordR;
        private readonly Vector2[] texCoordL;

        /// <summary>
        /// Makes a instance of the TailBase type passed in
        /// </summary>
        /// <param name="type">The type of the tail</param>
        /// <param name="position">The start position for the tail, usually the center of the object</param>
        /// <param name="drawLayer">Which layer it should be drawn on</param>
        /// <param name="direction">The facing direction, can be 1 or -1</param>
        public TailInstance(Type type, Vector2 position, Layer drawLayer, Vector2 entityDirection)
        {
            facingDirection = new Vector2(-entityDirection.X, entityDirection.Y);
            tailBase = (Tailbase)Activator.CreateInstance(type);
            tailBase.tailInstance = this;

            layer = drawLayer;

            bool distances = tailBase.VertexDistances != null;
            bool gravities = tailBase.VertexGravityForces != null;

            Vector2[] settledPoints;
            if (tailBase.SettledPoints != null)
                settledPoints = tailBase.SettledPoints;
            else
            {
                settledPoints = new Vector2[tailBase.VertexCount];
                for (int i = 0; i < tailBase.VertexCount; i++)
                {
                    int dist = distances ? tailBase.VertexDistances[i] : tailBase.VertexDistance;
                    Vector2 grav = gravities ? tailBase.VertexGravityForces[i] * tailBase.VertexGravityForce : tailBase.VertexGravityForce;
                    settledPoints[i] = (grav * dist) + (i > 0 ? settledPoints[i - 1] : Vector2.Zero);
                }
            }


            tailBones = new VerletChainInstance(tailBase.VertexCount, position + tailBase.WorldOffset, 
                position + ((tailBase.WorldOffset + settledPoints[tailBase.VertexCount - 1]) * facingDirection), tailBase.VertexDistance, tailBase.VertexGravityForce * facingDirection,
                gravities, tailBase.VertexGravityForces?.ToList(), distances, tailBase.VertexDistances?.ToList())
            {
                drag = tailBase.VertexDrag,
                constraintRepetitions = tailBase.PhysicsRepetitions
            };

            //if (tailBase.VertexCount > 1)
            if(!Main.dedServ)
            {
                spineBuffer = !tailBase.SpineEnabled ? null : new VertexBuffer(Main.graphics.GraphicsDevice, typeof(VertexPositionColor), tailBase.VertexCount, BufferUsage.WriteOnly);

                if (tailBase.GeometryEnabled)
                {
                    //removed dedserv checks since this is all wrapped in one
                    geometryBuffer = new VertexBuffer(Main.graphics.GraphicsDevice, typeof(VertexPositionColorTexture), ((tailBase.VertexCount - 1) * 3) + 2, BufferUsage.WriteOnly);
                    geometryIndexBuffer = new IndexBuffer(Main.graphics.GraphicsDevice, typeof(int), (tailBase.VertexCount - 1) * 9, BufferUsage.WriteOnly);

                    #region index buffer
                    int[] indices = new int[geometryIndexBuffer.IndexCount];
                    for (int i = 0; i < indices.Length / 3; i++)
                    {
                        int index = i * 3;
                        switch (i % 3)
                        {
                            case 0:
                                indices[index] = i;
                                indices[index + 1] = i + 1;
                                indices[index + 2] = i + 2;
                                break;
                            case 1:
                                indices[index] = i;
                                indices[index + 2] = i + 1;
                                indices[index + 1] = i + 3;
                                break;
                            case 2:
                                indices[index] = i - 2;
                                indices[index + 2] = i + 1;
                                indices[index + 1] = i;
                                break;
                        }
                    }

                    geometryIndexBuffer.SetData(indices);
                    #endregion

                    #region tex coords
                    Vector2 largestSize = Vector2.Zero;
                    Vector2 smallestSize = Vector2.One * 65536;
                    foreach (Vector2 vec in settledPoints)
                    {
                        if (Math.Abs(vec.X) > largestSize.X)
                            largestSize.X = vec.X;
                        if (vec.X < smallestSize.X)
                            smallestSize.X = vec.X;

                        if (Math.Abs(vec.Y) > largestSize.Y)
                            largestSize.Y = vec.Y;
                        if (-vec.Y < smallestSize.Y)
                            smallestSize.Y = -vec.Y;
                    }

                    largestSize += tailBase.TexSizeOffset * new Vector2(1, -1);
                    smallestSize += tailBase.TexPosOffset * new Vector2(1, -1);

                    texCoordR = TexCoords( 1, settledPoints, smallestSize, largestSize);
                    texCoordL = TexCoords(-1, settledPoints, smallestSize, largestSize);
                    #endregion
                }
            }
            //}

            GetLayerList(layer).Add(this);
        }

        private Vector2[] TexCoords(int dir, Vector2[] settledPoints, Vector2 smallestSize, Vector2 largestSize)
        {
            Vector2[] texCoords = new Vector2[((tailBase.VertexCount - 1) * 3) + 2];

            Vector2 TexCoordConvert(Vector2 realPosition)
            {
                Vector2 diff = (realPosition - smallestSize) - (new Vector2(0, tailBase.Width));
                Vector2 size = diff / (largestSize - (new Vector2(0, tailBase.Width) * 2));
                return (size * new Vector2(1, -1)) + new Vector2(0, 1);
            }

            texCoords[2] = TexCoordConvert(settledPoints[1]);

            texCoords[1] = TexCoordConvert(settledPoints[0] + (Vector2.UnitY * tailBase.Width * -dir));
            texCoords[0] = TexCoordConvert(settledPoints[0] + (Vector2.UnitY * tailBase.Width * dir));

            for (int i = 1; i < tailBase.VertexCount - 1; i++)//sets all vertexes besides the last two
            {
                int index = i * 3;

                texCoords[index + 2] = TexCoordConvert(settledPoints[i + 1]);

                float directionRot = (settledPoints[i + 1] - settledPoints[i]).ToRotation() + ((float)Math.PI * 0.5f);

                Vector2 segLocationA = settledPoints[i] + (Vector2.UnitY.RotatedBy(directionRot + (Math.PI * 0.5f * dir)) * tailBase.Width);
                texCoords[index + 1] = TexCoordConvert(segLocationA);

                Vector2 segLocationB = settledPoints[i] + (Vector2.UnitY.RotatedBy(directionRot - (Math.PI * 0.5f * dir)) * tailBase.Width);
                texCoords[index] = TexCoordConvert(segLocationB);
            }

            //last two vert
            float directionRot2 = (settledPoints[tailBase.VertexCount - 2] - settledPoints[tailBase.VertexCount - 1]).ToRotation() + ((float)Math.PI * 0.5f);

            Vector2 segLocation2A = settledPoints[tailBones.segmentCount - 1] + (Vector2.UnitY.RotatedBy(directionRot2 + (Math.PI * 0.5f * dir)) * tailBase.Width);  
            texCoords[geometryBuffer.VertexCount - 2] = TexCoordConvert(segLocation2A);

            Vector2 segLocation2B = settledPoints[tailBones.segmentCount - 1] + (Vector2.UnitY.RotatedBy(directionRot2 - (Math.PI * 0.5f * dir)) * tailBase.Width);
            texCoords[geometryBuffer.VertexCount - 1] = TexCoordConvert(segLocation2B);

            return texCoords;
        }

        /// <summary>
        /// Removes this TailInstance from the drawlist its in
        /// </summary>
        public void Remove() => 
            GetLayerList(layer).Remove(this);

        /// <summary>
        /// updates the verlet chain and it's start point, and checks if the tail should be flipped
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        public void Update(Vector2 position, Vector2 entityDirection) 
        {
            if (tailBase.PreUpdate())
            {
                facingDirection = new Vector2(-entityDirection.X, entityDirection.Y);
                tailBones.startPoint = position + (tailBase.WorldOffset * facingDirection);

                tailBones.forceGravity = facingDirection * tailBase.VertexGravityForce;

                tailBones.UpdateChain();
            }
        }

        /// <summary>
        /// flips the direction value and the chain's gravity direction
        /// </summary>
        //public void FlipHorizontal()
        //{
        //    facingDirection = -facingDirection;
        //    if (tailBones.forceGravities == null)
        //        tailBones.forceGravity *= new Vector2(-1, 1);
        //    else
        //        for (int i = 0; i < tailBones.segmentCount; i++)
        //            tailBones.forceGravities[i] *= new Vector2(-1, 1);
        //}

        /// <summary>
        /// allows the tailbase to draw custom sprites
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void DrawSprites(SpriteBatch spriteBatch)
        {
            if (tailBones.Active) 
                tailBase.DrawSprites(spriteBatch);
        }

        /// <summary>
        /// draws the geometry and the spine (if enabled)
        /// as well as any custom geometry the base has
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="pass"></param>
        /// <param name="graphicsDevice"></param>
        public void DrawGeometry(BasicEffect effect, EffectPass pass, GraphicsDevice graphicsDevice)
        {
            if (tailBones.Active)// && tailBones.segmentCount > 1)
            {
                tailBase.PreDrawGeometry(effect, pass, graphicsDevice);
                if (tailBase.GeometryEnabled)
                {
                    VertexPositionColorTexture[] vertexPos = new VertexPositionColorTexture[geometryBuffer.VertexCount];

                    Vector2 startLocation = tailBones.ropeSegments[0].ScreenPos - Main.ViewSize * 0.5f;
                    int directionMult = (startLocation.X > tailBones.ropeSegments[tailBones.segmentCount - 1].ScreenPos.X - Main.ViewSize.X * 0.5f ? -1 : 1) * -(int)facingDirection.Y;
                    Vector2[] texCor = directionMult == 1 ? texCoordL : texCoordR;

                    vertexPos[2] = new VertexPositionColorTexture(new Vector3(tailBones.ropeSegments[1].ScreenPos - Main.ViewSize / 2, 0), tailBase.GeometryColor(1), texCor[2]);

                    vertexPos[1] = new VertexPositionColorTexture(new Vector3(startLocation + (Vector2.UnitY * (directionMult * (int)facingDirection.Y) * tailBase.Width), 0), tailBase.GeometryColor(0), texCor[1]);
                    vertexPos[0] = new VertexPositionColorTexture(new Vector3(startLocation + (Vector2.UnitY * -(directionMult * (int)facingDirection.Y) * tailBase.Width), 0), tailBase.GeometryColor(0), texCor[0]);

                    for (int i = 1; i < tailBones.segmentCount - 1; i++)//sets all vertexes besides the last two
                    {
                        int index = i * 3;
                        vertexPos[index + 2] = new VertexPositionColorTexture(new Vector3(tailBones.ropeSegments[i + 1].ScreenPos - Main.ViewSize / 2, 0), tailBase.GeometryColor(i + 1), texCor[index + 2]);

                        float directionRot = (tailBones.ropeSegments[i + 1].posNow - tailBones.ropeSegments[i].posNow).ToRotation() + (float)Math.PI * 0.5f;
                        Vector2 segLocation = tailBones.ropeSegments[i].ScreenPos - Main.ViewSize / 2;
                        vertexPos[index + 1] = new VertexPositionColorTexture(new Vector3(segLocation + (Vector2.UnitY.RotatedBy(directionRot + (Math.PI * 0.5f)) * tailBase.Width), 0), tailBase.GeometryColor(i), texCor[index + 1]);
                        vertexPos[index] = new VertexPositionColorTexture(new Vector3(segLocation + (Vector2.UnitY.RotatedBy(directionRot - (Math.PI * 0.5f)) * tailBase.Width), 0), tailBase.GeometryColor(i), texCor[index]);
                    }

                    //last two vert
                    float directionRot2 = (tailBones.ropeSegments[tailBones.segmentCount - 2].posNow - tailBones.ropeSegments[tailBones.segmentCount - 1].posNow).ToRotation() + (float)Math.PI * 0.5f;
                    Vector2 segLocation2 = tailBones.ropeSegments[tailBones.segmentCount - 1].ScreenPos - Main.ViewSize / 2;
                    vertexPos[geometryBuffer.VertexCount - 2] = new VertexPositionColorTexture(new Vector3(segLocation2 + (Vector2.UnitY.RotatedBy(directionRot2 + (Math.PI * 0.5f)) * tailBase.Width), 0), tailBase.GeometryColor(tailBones.segmentCount - 1), texCor[geometryBuffer.VertexCount - 2]);
                    vertexPos[geometryBuffer.VertexCount - 1] = new VertexPositionColorTexture(new Vector3(segLocation2 + (Vector2.UnitY.RotatedBy(directionRot2 - (Math.PI * 0.5f)) * tailBase.Width), 0), tailBase.GeometryColor(tailBones.segmentCount - 1), texCor[geometryBuffer.VertexCount - 1]);

                    geometryBuffer.SetData(vertexPos);

                    effect.View = Main.GameViewMatrix.ZoomMatrix * Matrix.CreateScale(1, -1, 1);

                    effect.TextureEnabled = true;
                    effect.Texture = ModContent.GetTexture(tailBase.Texture);

                    pass.Apply();
                    graphicsDevice.SetVertexBuffer(geometryBuffer);
                    graphicsDevice.Indices = geometryIndexBuffer;
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geometryBuffer.VertexCount, 0, geometryIndexBuffer.IndexCount / 3);
                }

                if (tailBase.SpineEnabled)
                {
                    VertexPositionColor[] vertexPos = new VertexPositionColor[spineBuffer.VertexCount];

                    for (int i = 0; i < vertexPos.Length; i++)
                        vertexPos[i] = new VertexPositionColor(new Vector3(tailBones.ropeSegments[i].ScreenPos - Main.ViewSize / 2, 0), tailBase.SpineColor(i));
                    
                    spineBuffer.SetData(vertexPos);

                    effect.TextureEnabled = false;//this or a second effect is needed
                    effect.View = Main.GameViewMatrix.ZoomMatrix * Matrix.CreateScale(1, -1, 1);

                    pass.Apply();
                    graphicsDevice.SetVertexBuffer(spineBuffer);
                    graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, spineBuffer.VertexCount - 1);
                }
            }
        }
    }


    /// <summary>
    /// A helper for items that give the player a tail
    /// Internally this is done with:  <c>player.GetModPlayer<TailPlayer>().CurrentTailType = TailType</c>
    /// UpdateAccessory is used for this so use SafeUpdateAccessory instead
    /// </summary>
    public abstract class TailItem : ModItem
    {
        public readonly Type TailType;
        protected TailItem(Type tailType) => 
            TailType = tailType;

        public sealed override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (SafeUpdateAccessory(player, hideVisual)) 
                player.GetModPlayer<TailPlayer>().CurrentTailType = TailType;
        }

        /// <summary>
        /// Use this as you would normally use UpdateAccessory.
        /// Return false to disable tail.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="hideVisual"></param>
        /// <returns></returns>
        public virtual bool SafeUpdateAccessory(Player player, bool hideVisual) => true;
    }

    /// <summary>
    /// A helper for npcs with tail
    /// Internally this is done with:  <c>npc.SetTail(TailType)</c>
    /// AI is used for this so use SafeAI instead
    /// </summary>
    public abstract class TailNpc : ModNPC
    {
        public readonly Type TailType;
        protected TailNpc(Type tailType) =>
            TailType = tailType;

        private bool hasSpawned = false;
        public sealed override void AI()
        {
            if (!hasSpawned)
            {
                npc.SetTail(TailType);
                hasSpawned = true;
            }

            npc.TailActive(SafeAI());
        }

        /// <summary>
        /// Use this as you would normally use AI.
        /// Return false to disable tail.
        /// </summary>
        /// <returns></returns>
        public virtual bool SafeAI() => true;
    }
}