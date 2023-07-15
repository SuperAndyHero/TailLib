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
    /// This class stores everything needed have a working tail, as an instance is made for anything with a tail.
    /// It stores the TailBase instance, the verlet chain instace, the vertex/index buffers, and other info like direction and alpha
    /// </summary>
    public class TailInstance
    {
        /// <summary>
        /// Has all the values this tail uses, but does not store any data itself
        /// </summary>
        public Tailbase tailBase;
        /// <summary>
        /// The VerletChainInstance, where you can access anything about the verlet chain
        /// </summary>
        public VerletChainInstance tailBones;
        /// <summary>
        /// 1 or -1, X for facing direction, Y for gravity direction
        /// </summary>
        public Vector2 facingDirection;
        /// <summary>
        /// The alpha value, this ranges from 0 to 1.
        /// Multiply your color by this to have it be effected by player immune opacity
        /// </summary>
        public float alpha = 1f;

        /// <summary>
        /// The entity this is attached to, used only for the shader but can be used to get other data
        /// This is only set on creation but in theory could be changed
        /// </summary>
        public Entity entity;

        /// <summary>
        /// The shader to be applied to this 
        /// gotten from the the dye item in the dye slot before each update
        /// </summary>
        public ArmorShaderData armorShaderData = null;

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
        /// <param name="tailType">The type of the tail</param>
        /// <param name="position">The start position for the tail, usually the center of the object</param>
        /// <param name="drawLayer">Which layer it should be drawn on</param>
        /// <param name="packedDirection">entity
        public TailInstance(Type tailType, Vector2 position, Layer drawLayer, Entity entity, int gravDir = 1)
        {
            this.entity = entity;
            facingDirection = new Vector2(-entity.direction, gravDir);
            tailBase = (Tailbase)Activator.CreateInstance(tailType);
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
                    float dist = distances ? tailBase.VertexDistances[i] : tailBase.VertexDistance;
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
            if (!Main.dedServ)
            {
                spineBuffer = !tailBase.SpineEnabled ? null : new VertexBuffer(Main.graphics.GraphicsDevice, typeof(VertexPositionColorTexture), tailBase.VertexCount, BufferUsage.WriteOnly);

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

                    texCoordR = TexCoords(1, settledPoints, smallestSize, largestSize);
                    texCoordL = TexCoords(-1, settledPoints, smallestSize, largestSize);
                    #endregion
                }
            }
            //}

            GetLayerList(layer).Add(this);
        }

        /// <summary>
        /// updates the verlet chain and it's start point, and checks if the tail should be flipped
        /// </summary>
        /// <param name="position"></param>
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
        /// Removes this TailInstance from the drawlist its in
        /// </summary>
        public void Remove() =>
            GetLayerList(layer).Remove(this);

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
        /// applies the armor shader, call this before each drawn sprite, unless using DrawShaderSprite
        /// </summary>
        public void ApplyShader(DrawData drawdata)
        {
            if (armorShaderData == null)
                return;

            armorShaderData.Apply(entity, drawdata);
        }

        /// <summary>
        /// use this to draw a sprite and apply the shader at the same time
        /// </summary>
        public void DrawShaderSprite(SpriteBatch spriteBatch, DrawData drawdata)
        {
            ApplyShader(drawdata);
            drawdata.Draw(spriteBatch);
        }

        /// <summary>
        /// allows the tailbase to draw custom sprites
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void DrawSprites(SpriteBatch spriteBatch)
        {
            if (tailBones.Active)
            {
                //in theory you can call pass.apply on any shader you want before drawing your sprite to set a specific/custom shader
                tailBase.DrawSprites(spriteBatch);
            }
        }

        /// <summary>
        /// draws the geometry and the spine (if enabled)
        /// as well as any custom geometry the base has
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="pass"></param>
        /// <param name="graphicsDevice"></param>
        public void DrawGeometry(bool wireframe)
        {
            if (!tailBones.Active)// && tailBones.segmentCount > 1)
                return;

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;

            //if armorShaderData is null these are used instead
            BasicEffect basiceffect = TailLib.BasicEffect;
            EffectPass basiceffectpass = basiceffect.CurrentTechnique.Passes[0];

            tailBase.PreDrawGeometry(armorShaderData, entity, basiceffect, basiceffectpass, graphicsDevice);

            bool useArmorShader = armorShaderData != null && !wireframe;//maybe add this as a param to predrawgeometry

            Matrix viewMatrix = useArmorShader ? Main.GameViewMatrix.TransformationMatrix * Matrix.CreateScale(0.25f) : Main.GameViewMatrix.ZoomMatrix * Matrix.CreateScale(1, -1, 1);
            Vector2 viewSizeOffset = useArmorShader ? Vector2.Zero : (Main.ViewSize * 0.5f);

            if (tailBase.GeometryEnabled)
            {
                if (wireframe)
                    graphicsDevice.RasterizerState.FillMode = FillMode.WireFrame;

                VertexPositionColorTexture[] vertexPos = new VertexPositionColorTexture[geometryBuffer.VertexCount];

                Vector2 startLocation = tailBones.ropeSegments[0].ScreenPos - viewSizeOffset;
                //int directionMult = (startLocation.X > tailBones.ropeSegments[tailBones.segmentCount - 1].ScreenPos.X - Main.ViewSize.X * 0.5f ? -1 : 1) * -(int)facingDirection.Y;
                int facingDir = (tailBase.SpriteDirection() ? 1 : -1);
                int directionMult = (int)facingDirection.Y * facingDir;
                Vector2[] texCor = directionMult == 1 ? texCoordL : texCoordR;

                //this angle allows the base to tilt if the angle is too extreme
                const float rotateAngle = (float)Math.PI * 0.175f;//0.15-0.2 is a good range, this is the limit and range it tilts
                float seg0to1angle = (facingDir == 1 ? 
                    (tailBones.ropeSegments[0].posNow - tailBones.ropeSegments[1].posNow) : 
                    (tailBones.ropeSegments[1].posNow - tailBones.ropeSegments[0].posNow)).ToRotation();
                bool aboveAngle = seg0to1angle > rotateAngle;
                bool belowAngle = seg0to1angle < -rotateAngle;
                float topStartRot = aboveAngle ? seg0to1angle - (Math.Sign(seg0to1angle) * rotateAngle) : 0;
                float bottomStartRot = belowAngle ? seg0to1angle - (Math.Sign(seg0to1angle) * rotateAngle) : 0;

                vertexPos[2] = new VertexPositionColorTexture(new Vector3(tailBones.ropeSegments[1].ScreenPos - viewSizeOffset, 0).Transform(viewMatrix), tailBase.GeometryColor(1), texCor[2]);

                vertexPos[1] = new VertexPositionColorTexture(new Vector3(startLocation + (Vector2.UnitY.RotatedBy(bottomStartRot + (topStartRot * 0.5f)) * (directionMult * (int)facingDirection.Y) * tailBase.Width), 0).Transform(viewMatrix), tailBase.GeometryColor(0), texCor[1]);
                vertexPos[0] = new VertexPositionColorTexture(new Vector3(startLocation + (Vector2.UnitY.RotatedBy(topStartRot + (bottomStartRot * 0.5f)) * -(directionMult * (int)facingDirection.Y) * tailBase.Width), 0).Transform(viewMatrix), tailBase.GeometryColor(0), texCor[0]);

                for (int i = 1; i < tailBones.segmentCount - 1; i++)//sets all vertexes besides the last two
                {
                    int index = i * 3;
                    vertexPos[index + 2] = new VertexPositionColorTexture(new Vector3(tailBones.ropeSegments[i + 1].ScreenPos - viewSizeOffset, 0).Transform(viewMatrix), tailBase.GeometryColor(i + 1), texCor[index + 2]);

                    float directionRot = 
                        ((tailBones.ropeSegments[i + 1].posNow - tailBones.ropeSegments[i].posNow) + 
                        (tailBones.ropeSegments[i].posNow - tailBones.ropeSegments[i - 1].posNow)).ToRotation() + 
                        (float)Math.PI * 0.5f;
                    Vector2 segLocation = tailBones.ropeSegments[i].ScreenPos - viewSizeOffset;
                    vertexPos[index + 1] = new VertexPositionColorTexture(new Vector3(segLocation + (Vector2.UnitY.RotatedBy(directionRot + (Math.PI * 0.5f)) * tailBase.Width), 0).Transform(viewMatrix), tailBase.GeometryColor(i), texCor[index + 1]);
                    vertexPos[index] = new VertexPositionColorTexture(new Vector3(segLocation + (Vector2.UnitY.RotatedBy(directionRot - (Math.PI * 0.5f)) * tailBase.Width), 0).Transform(viewMatrix), tailBase.GeometryColor(i), texCor[index]);
                }

                //last two vert
                float directionRot2 = (tailBones.ropeSegments[tailBones.segmentCount - 2].posNow - tailBones.ropeSegments[tailBones.segmentCount - 1].posNow).ToRotation() + (float)Math.PI * 0.5f;
                Vector2 segLocation2 = tailBones.ropeSegments[tailBones.segmentCount - 1].ScreenPos - viewSizeOffset;
                vertexPos[geometryBuffer.VertexCount - 2] = new VertexPositionColorTexture(new Vector3(segLocation2 + (Vector2.UnitY.RotatedBy(directionRot2 + (Math.PI * 0.5f)) * tailBase.Width), 0).Transform(viewMatrix), tailBase.GeometryColor(tailBones.segmentCount - 1), texCor[geometryBuffer.VertexCount - 2]);
                vertexPos[geometryBuffer.VertexCount - 1] = new VertexPositionColorTexture(new Vector3(segLocation2 + (Vector2.UnitY.RotatedBy(directionRot2 - (Math.PI * 0.5f)) * tailBase.Width), 0).Transform(viewMatrix), tailBase.GeometryColor(tailBones.segmentCount - 1), texCor[geometryBuffer.VertexCount - 1]);

                geometryBuffer.SetData(vertexPos);

                if(useArmorShader)
                {
                    graphicsDevice.Textures[0] = ModContent.Request<Texture2D>(tailBase.Texture).Value;
                    armorShaderData.Apply(entity, new DrawData(ModContent.Request<Texture2D>(tailBase.Texture).Value, new Rectangle(0,0,4,4), Color.White));//rect does not matter
                }
                else//assumes basiceffect
                {
                    basiceffect.TextureEnabled = true;
                    basiceffect.Texture = wireframe ? Terraria.GameContent.TextureAssets.BlackTile.Value : ModContent.Request<Texture2D>(tailBase.Texture).Value;
                    basiceffectpass.Apply();
                }

                graphicsDevice.SetVertexBuffer(geometryBuffer);
                graphicsDevice.Indices = geometryIndexBuffer;
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, geometryBuffer.VertexCount, 0, geometryIndexBuffer.IndexCount / 3);
                
                if(wireframe)
                    graphicsDevice.RasterizerState.FillMode = FillMode.Solid;
            }

            if (tailBase.SpineEnabled)
            {
                VertexPositionColorTexture[] vertexPos = new VertexPositionColorTexture[spineBuffer.VertexCount];//vanilla dyes need a texture coord value

                for (int i = 0; i < vertexPos.Length; i++)
                    vertexPos[i] = new VertexPositionColorTexture(new Vector3(tailBones.ropeSegments[i].ScreenPos - viewSizeOffset, 0).Transform(viewMatrix), tailBase.SpineColor(i), Vector2.Zero);

                spineBuffer.SetData(vertexPos);

                if (useArmorShader)
                {
                    graphicsDevice.Textures[0] = Terraria.GameContent.TextureAssets.BlackTile.Value;
                    armorShaderData.Apply(entity, new DrawData(ModContent.Request<Texture2D>(tailBase.Texture).Value, new Rectangle(0, 0, 4, 4), Color.White));//rect does not matter
                }
                else//assumes basiceffect
                {
                    basiceffect.TextureEnabled = false;
                    basiceffectpass.Apply();
                }

                graphicsDevice.SetVertexBuffer(spineBuffer);
                graphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, spineBuffer.VertexCount - 1);
            }
        }

        private Vector2[] TexCoords(int dir, Vector2[] settledPoints, Vector2 smallestSize, Vector2 largestSize)
        {
            //this pre-calculates each vertex position and gets it's texture coords, and is only ran when the tail is being created

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

                float directionRot = ((settledPoints[i + 1] - settledPoints[i]) + (settledPoints[i] - settledPoints[i - 1])).ToRotation() + ((float)Math.PI * 0.5f);

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
    }
}
