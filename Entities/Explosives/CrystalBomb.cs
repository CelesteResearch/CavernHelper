// Copyright (c) Exudias and CommunalHelper. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Core.Reflection.Runtime;
using Celeste.Graphics.Animation;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Graphics.Content;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Holdables.TheoCrystal;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Simulation.Presentation.Hud.Tutorials.Birds;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Gameplay.Effects.Debris;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.CavernHelper.Components.Explosives;
using Celeste.Mods.CavernHelper.Entities.Floors;
using Celeste.Mods.CavernHelper.Entities.Fields;
using Player = Celeste.Simulation.RoomPlayerSession;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;
using DashSwitch = Celeste.Game.Gameplay.Mechanisms.DashSwitches.OriginalDashSwitchState;
using SwapBlock = Celeste.Game.Entities.Platforms.SwapBlocks.OriginalSwapBlockState;
using CassetteBlock = Celeste.Game.Gameplay.Platforms.Cassette.CassetteBlockState;
using TempleCrackedBlock = Celeste.Game.Gameplay.Walls.Breakable.OriginalTempleCrackedBlockState;
using DashBlock = Celeste.Game.Gameplay.Walls.Breakable.OriginalDashBlockState;
using TouchSwitch = Celeste.Game.Gameplay.Mechanisms.Switches.OriginalTouchSwitchState;
using Spring = Celeste.Game.Gameplay.Interaction.Springs.OriginalSpringState;
using Solid = Celeste.Simulation.Gameplay.Platforms.Foundation.SourceSolidEntityState;

namespace Celeste.Mods.CavernHelper.Entities.Explosives
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.CavernHelper.CrystalBomb")]
    [CustomEntity("cavern/crystalbomb")]
    public class CrystalBomb : SourceActorEntityState
    {
        public HoldableState Hold;
        private readonly ISourceEngineContext engine;
        private readonly string exploderTrackerKey;
        private readonly string explosionTrackerKey;
        public Vector2 Speed;

        private readonly Sprite sprite;
        private readonly SourceCollisionHandler onCollideH;
        private readonly SourceCollisionHandler onCollideV;
        private readonly CircleColliderGeometry pushRadius;
        private readonly HitboxColliderGeometry hitBox;
        private readonly float maxRespawnTime;
        private readonly float maxExplodeTimer;
        private readonly bool explodeOnSpawn = false;
        private readonly bool respawnOnExplode = true;
        private readonly bool breakDashBlocks = false;
        private readonly bool legacyMode = true;

        private BirdTutorialGuiState tutorialGui;
        private Vector2 prevLiftSpeed;
        private Vector2 startPos;
        public string startLevel;
        private float noGravityTimer;
        private float hardVerticalHitSoundCooldown;
        private float respawnTime = 0f;
        private float frameCount;
        private float explodeTimer = 0f;
        private bool exploded;
        private bool activated = false;
        private bool shouldShowTutorial = true;
        private bool playedFuseSound = false;

        public CrystalBomb(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(new PixelActor(null, data.Position + offset), engine,
                ((ISourcePlatformTargetMotionEnvironment)engine).TargetMotionPolicy)
        {
            this.engine = engine;
            var catalog = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog;
            exploderTrackerKey = catalog.Find(typeof(CrystalBombExploderCollider)).RegistryKey;
            explosionTrackerKey = catalog.Find(typeof(CrystalBombExplosionCollider)).RegistryKey;
            Depth = 100;
            maxRespawnTime = EntityAttributeReader.Float(data.Values, "respawnTime", 2f);
            maxExplodeTimer = EntityAttributeReader.Float(data.Values, "explodeTime", 1f);
            explodeOnSpawn = EntityAttributeReader.Bool(data.Values, "explodeOnSpawn", false);
            respawnOnExplode = EntityAttributeReader.Bool(data.Values, "respawnOnExplode", true);
            breakDashBlocks = EntityAttributeReader.Bool(data.Values, "breakDashBlocks", false);

            // Legacy behavior did not call Holdable.CheckAgainstColliders()
            // Defaults to true for old placements, false for new ones
            legacyMode = EntityAttributeReader.Bool(data.Values, "legacyMode", !data.Values.ContainsKey("legacyMode"));

            pushRadius = new CircleColliderGeometry(((ISourceVectorEnvironment)engine).VectorArithmetic, 40f, 0f, 0f);
            hitBox = new HitboxColliderGeometry(8f, 10f, -4f, -10f);
            Collider = hitBox;
            onCollideH = OnCollideH;
            onCollideV = OnCollideV;

            Add(sprite = Celeste.Mods.CavernHelper.Runtime.CavernRuntime.For(engine).SpriteBank.Create("crystalbomb"));

            Add(Hold = new HoldableState(engine));
            Hold.PickupCollider = new HitboxColliderGeometry(16f, 16f, -8f, -16f);
            Hold.OnPickup = new Action(OnPickup);
            Hold.OnRelease = new Action<Vector2>(OnRelease);
            Hold.OnHitSpring = value => HitSpring((Spring)value);

            Hold.SpeedGetter = () => Speed;

            Add(new VertexLightState(engine, Collider.Center, PackedColor.White, 1f, 32, 64));
            Add(new Celeste.Simulation.Presentation.Effects.Mirrors.MirrorReflectionState());
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            startPos = Position;
            startLevel = ((Level)Scene).Session.Level;
            if (explodeOnSpawn)
            {
                activated = true;
            }

            if (((Level)Scene).Session.Level == "a-05tut")
            {
                tutorialGui = new BirdTutorialGuiState(engine, ((IOriginalGameTagsEnvironment)engine).Tags.HUD.Value,
                        (ITheoCrystalTutorialResources)engine, this, new Vector2(0f, -24f),
                        ((ITheoCrystalTutorialResources)engine).CleanDialog("tutorial_carry"),
                        ((ITheoCrystalTutorialResources)engine).CleanDialog("tutorial_hold"), BirdTutorialButton.Grab)
                    { Open = false };
                shouldShowTutorial = true;
                ((ISourceScene)Scene).SourceScene.QueueSourceEntity(tutorialGui);
            }
        }

        public override void Update()
        {
            base.Update();
            frameCount += 1f;

            Level level = ((Level)Scene);
            Player player = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<Player>();
            if (!exploded)
            {
                foreach (CrystalBombField field in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                             typeof(CrystalBombField)))
                {
                    field.Collidable = true;
                    if (CollideCheck(field))
                    {
                        field.Collidable = false;
                        Explode();
                        return;
                    }

                    field.Collidable = false;
                }

                foreach (CrystalBombExploderCollider collider in ((ISourceScene)Scene).SourceScene.Components
                         .GetTrackedComponents(exploderTrackerKey))
                {
                    if (collider.Check(this))
                    {
                        Explode();
                        return;
                    }
                }

                if (!Hold.IsHeld)
                {
                    if (legacyMode)
                    {
                        foreach (Spring spring in ((ISourceScene)Scene).SourceScene.Entities.Entities.OfType<Spring>())
                        {
                            if (CollideCheck(spring) && HitSpring(spring))
                            {
                                BounceAnimate(spring);
                            }
                        }
                    }

                    if (tutorialGui != null)
                    {
                        tutorialGui.Open = shouldShowTutorial && player != null &&
                                           ((ISourceVectorEnvironment)engine).VectorArithmetic.Distance(player.Position,
                                               Position) < 64;
                    }
                }

                if (activated)
                {
                    if (!playedFuseSound)
                    {
                        PlaySound("event:/game/04_cliffside/arrowblock_debris", Position);
                        playedFuseSound = true;
                    }

                    explodeTimer += engine.DeltaTime;
                    sprite.Play("crbomb", false, false);
                    if (maxExplodeTimer > 0)
                    {
                        sprite.SetAnimationFrame((int)Math.Floor((double)(explodeTimer / maxExplodeTimer * 60f)));
                    }

                    if (explodeTimer >= maxExplodeTimer)
                    {
                        Explode();
                    }
                }

                hardVerticalHitSoundCooldown -= engine.DeltaTime;
                Depth = 100;
                if (Hold.IsHeld)
                {
                    prevLiftSpeed = Vector2.Zero;
                }
                else
                {
                    if (OnGround(downCheck: 1))
                    {
                        float target = !OnGround(Position + (Vector2.UnitX * 3f), 1) ? 20f :
                            !OnGround(Position - (Vector2.UnitX * 3f), 1) ? -20f : 0f;

                        Speed.X = ScalarMath.Approach(Speed.X, target,
                            800f * engine.DeltaTime * (CollideCheck<IcyFloor>() ? 0.01f : 1));
                        Vector2 liftSpeed = LiftSpeed;
                        if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero)
                        {
                            Speed = prevLiftSpeed;
                            prevLiftSpeed = Vector2.Zero;
                            Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
                            if (Speed.X != 0f && Speed.Y == 0f)
                            {
                                Speed.Y = -60f;
                            }

                            if (Speed.Y < 0f)
                            {
                                noGravityTimer = 0.15f;
                            }
                        }
                        else
                        {
                            prevLiftSpeed = liftSpeed;
                            if (liftSpeed.Y < 0f && Speed.Y < 0f)
                            {
                                Speed.Y = 0f;
                            }
                        }
                    }
                    else
                    {
                        bool shouldHaveGravity = Hold.ShouldHaveGravity;
                        if (shouldHaveGravity)
                        {
                            float num = 800f;
                            if (Math.Abs(Speed.Y) <= 30f)
                            {
                                num *= 0.5f;
                            }

                            float num2 = 350f;
                            if (Speed.Y < 0f)
                            {
                                num2 *= 0.5f;
                            }

                            Speed.X = ScalarMath.Approach(Speed.X, 0f, num2 * engine.DeltaTime);
                            if (noGravityTimer > 0f)
                            {
                                noGravityTimer -= engine.DeltaTime;
                            }
                            else
                            {
                                Speed.Y = ScalarMath.Approach(Speed.Y, 200f, num * engine.DeltaTime);
                            }
                        }
                    }

                    MoveH(Speed.X * engine.DeltaTime, onCollideH, null);
                    MoveV(Speed.Y * engine.DeltaTime, onCollideV, null);
                    if (Left < level.Bounds.Left)
                    {
                        Left = level.Bounds.Left;
                        OnCollideH(new PixelCollision
                        {
                            Direction = -Vector2.UnitX
                        });
                    }
                    else if (Right > level.Bounds.Right)
                    {
                        Right = level.Bounds.Right;
                        OnCollideH(new PixelCollision
                        {
                            Direction = Vector2.UnitX
                        });
                    }

                    if (Top < level.Bounds.Top)
                    {
                        Top = level.Bounds.Top;
                        OnCollideV(new PixelCollision
                        {
                            Direction = -Vector2.UnitY
                        });
                    }
                    else if (Bottom > level.Bounds.Bottom + 4)
                    {
                        if (!exploded)
                        {
                            Explode();
                            return;
                        }
                    }
                }

                if (!legacyMode)
                {
                    Hold.CheckAgainstColliders();
                }
            }
            else
            {
                if (respawnTime < maxRespawnTime)
                {
                    respawnTime += engine.DeltaTime;
                }
                else
                {
                    respawnTime = 0f;
                    exploded = false;
                    Visible = true;
                    Collidable = true;
                    playedFuseSound = false;
                    sprite.Play("idle", true, false);
                    Position = startPos;
                    if (explodeOnSpawn)
                    {
                        activated = true;
                    }
                }
            }
        }

        protected override void OnSquish(PixelCollision data)
        {
            if (!TrySquishWiggle(data))
            {
                Explode();
            }
        }

        private bool HitSpring(Spring spring)
        {
            if (!Hold.IsHeld)
            {
                if (spring.Orientation == SpringOrientation.Floor && (legacyMode || Speed.Y >= 0f))
                {
                    Speed.X *= 0.5f;
                    Speed.Y = -160f;
                    noGravityTimer = 0.15f;
                    return true;
                }
                else if (spring.Orientation == SpringOrientation.WallLeft && (legacyMode || Speed.X <= 0f))
                {
                    Speed.X = 240f;
                    Speed.Y = -140f;
                    noGravityTimer = 0.15f;
                    return true;
                }
                else if (spring.Orientation == SpringOrientation.WallRight && (legacyMode || Speed.X >= 0f))
                {
                    Speed.X = -240f;
                    Speed.Y = -140f;
                    noGravityTimer = 0.15f;
                    return true;
                }
            }

            return false;
        }

        private void OnPickup()
        {
            if (tutorialGui != null)
            {
                shouldShowTutorial = false;
            }

            if (!legacyMode)
            {
                AddTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);
            }

            Speed = Vector2.Zero;
            activated = true;
        }

        private void OnRelease(Vector2 force)
        {
            if (force.X != 0f && force.Y == 0f)
            {
                force.Y = -0.4f;
            }

            RemoveTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);

            Speed = force * 200f;
            if (Speed != Vector2.Zero)
            {
                noGravityTimer = 0.1f;
            }
        }

        private void OnCollideH(PixelCollision data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            }

            PlaySound("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
            if (Math.Abs(Speed.X) > 100f)
            {
                ImpactParticles(data.Direction);
            }

            Speed.X *= -0.4f;
        }

        private void OnCollideV(PixelCollision data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
            }

            if (Speed.Y > 0f && frameCount > 15f)
            {
                if (hardVerticalHitSoundCooldown <= 0f)
                {
                    PlaySound("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity",
                        ScalarMath.ClampedMap(Speed.Y, 0f, 200f, 0f, 1f));
                    hardVerticalHitSoundCooldown = 0.5f;
                }
                else
                {
                    PlaySound("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", 0f);
                }
            }

            if (Speed.Y > 160f)
            {
                ImpactParticles(data.Direction);
            }

            if (CollideCheck<IcyFloor>())
            {
                Speed.Y = 0;
            }
            else if (Speed.Y > 140f && data.Hit is not SwapBlock && data.Hit is not DashSwitch)
            {
                Speed.Y *= -0.6f;
            }
            else
            {
                Speed.Y = 0f;
            }
        }

        private void ImpactParticles(Vector2 dir)
        {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            if (dir.X > 0f)
            {
                direction = 3.14159274f;
                position = new Vector2(Right, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            }
            else if (dir.X < 0f)
            {
                direction = 0f;
                position = new Vector2(Left, Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            }
            else if (dir.Y > 0f)
            {
                direction = -1.57079637f;
                position = new Vector2(X, Bottom);
                positionRange = Vector2.UnitX * 6f;
            }
            else
            {
                direction = 1.57079637f;
                position = new Vector2(X, Top);
                positionRange = Vector2.UnitX * 6f;
            }

            ((Level)Scene).Particles.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.TheoCrystal.P_Impact, 12,
                position, positionRange, direction);
        }

        internal void Explode()
        {
            if (!exploded)
            {
                exploded = true;
                activated = false;
                explodeTimer = 0f;

                sprite.Play("idle", true, false);
                ((Level)Scene).Displacement.AddBurst(Position, 0.35f, 8f, 48f, 0.25f, null, null);
                CrystalBombRecoverBlast.Spawn(engine, Position);
                for (int i = 0; i < 10; i++)
                {
                    PlaySound("event:/game/06_reflection/fall_spike_smash", Position);
                }

                Collider = pushRadius;
                if (!CollideCheck<CassetteBlock>())
                {
                    CrystalDebrisState.Burst(engine, Position + (Vector2.UnitY * -10f),
                        SourceHexColor.HexToColor("639bff"), false, 32);
                }

                Player player = CollideFirst<Player>();
                if (player != null && !SolidBlocksExplosion(Position + Vector2.UnitY * -10f, player.Center))
                {
                    player.ExplodeLaunch(Position, true);
                }

                foreach (TempleCrackedBlock crackedBlock in CollideAll<TempleCrackedBlock>())
                {
                    crackedBlock.Break(Position);
                }

                if (breakDashBlocks)
                {
                    foreach (DashBlock dashBlock in CollideAll<DashBlock>())
                    {
                        dashBlock.Break(Position, Position - dashBlock.Position, true, true);
                    }
                }

                foreach (CrystalBombExplosionCollider ec in ((ISourceScene)Scene).SourceScene.Components
                         .GetTrackedComponents(explosionTrackerKey))
                {
                    ec.Check(this);
                }

                if (!legacyMode)
                {
                    foreach (TouchSwitch touchSwitch in CollideAll<TouchSwitch>())
                    {
                        touchSwitch.TurnOn();
                    }
                }

                if (Hold.IsHeld)
                {
                    Hold.Holder.Holding.Drop();
                }

                if (!respawnOnExplode || ((Level)Scene).Session.Level != startLevel)
                {
                    RemoveSelf();
                }

                Collider = hitBox;
                Visible = false;
                Collidable = false;
                Position = startPos;
                Speed = Vector2.Zero;
            }
        }


        private static readonly Action<Spring> BounceAnimate = (Action<Spring>)typeof(Spring)
            .GetMethod("BounceAnimate", BindingFlags.Instance | BindingFlags.NonPublic)
            .CreateDelegate(typeof(Action<Spring>));

        private bool CollideCheck(SceneEntityState entity) => SourceEntityCollision.Check(this, entity);

        private bool CollideCheck<T>() where T : SceneEntityState
        {
            foreach (T entity in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(T)))
                if (SourceEntityCollision.Check(this, entity))
                    return true;
            return false;
        }

        private T CollideFirst<T>() where T : SceneEntityState
        {
            foreach (T entity in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(T)))
                if (SourceEntityCollision.Check(this, entity))
                    return entity;
            return null;
        }

        private List<T> CollideAll<T>() where T : SceneEntityState
        {
            var result = new List<T>();
            foreach (T entity in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(T)))
                if (SourceEntityCollision.Check(this, entity))
                    result.Add(entity);
            return result;
        }

        private bool SolidBlocksExplosion(Vector2 from, Vector2 to)
        {
            foreach (Solid solid in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(Solid)))
                if (solid.Collidable && SourceEntityGeometryCollision.Line(solid, from, to))
                    return true;
            return false;
        }

        private void PlaySound(string path, Vector2 position, string parameter = null, float value = 0f)
        {
            var process = (IOriginalGameplayProcess)engine;
            if (parameter == null) process.AudioSession.Play(path, position, process.SaveContext);
            else process.AudioSession.Play(path, position, process.SaveContext, parameter, value);
        }
    }
}
