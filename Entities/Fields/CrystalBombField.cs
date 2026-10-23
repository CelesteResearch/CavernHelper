// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Graphics.Display;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Enemies.Seeker;

namespace Celeste.Mods.CavernHelper.Entities.Fields
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.CavernHelper.CrystalBombField")]
    [CustomEntity("cavern/crystalBombField")]
    public sealed class CrystalBombField : SeekerBarrierState
    {
        private readonly string rendererTrackerKey;
        private readonly string adjacentTrackerKey;
        protected override string RendererTrackerKey => rendererTrackerKey;
        protected override string AdjacentTrackerKey => adjacentTrackerKey;

        public static CrystalBombField Load(OriginalLevelSceneRoot level,
            Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room,
            Vector2 offset, Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data)
            => new CrystalBombField(level, data, offset);

        public CrystalBombField(OriginalLevelSceneRoot level, EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height,
                level.SolidWorldFactory.Create(data.Position + offset, false))
        {
            var catalog = ((ISourceSceneCatalogEnvironment)level.EngineContext).SceneCatalog;
            rendererTrackerKey = catalog.Find(typeof(CrystalBombFieldRenderer)).RegistryKey;
            adjacentTrackerKey = catalog.Find(typeof(CrystalBombField)).RegistryKey;
        }

        public void OnTriggerDetonation() => OnReflectSeeker();

        public override void Render(ISourceRenderContext context)
        {
            PackedColor color = PackedColor.Yellow * .6f;
            foreach (Vector2 value in ParticlePositions)
            {
                var draw = new AtlasTextureDraw(((ISourceDrawScope)context).Draw.Pixel, Position + value,
                    Vector2.Zero, color, Vector2.One);
                ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, this);
            }

            if (Flashing)
                SourcePrimitiveDrawing.Rect(context, this, Collider.AbsoluteLeft,
                    Collider.AbsoluteTop, Collider.Width, Collider.Height, PackedColor.Purple * Flash * .5f);
        }
    }
}
