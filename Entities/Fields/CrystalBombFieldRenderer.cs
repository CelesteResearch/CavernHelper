// SPDX-License-Identifier: MIT

using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Enemies.Seeker;

namespace Celeste.Mods.CavernHelper.Entities.Fields
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.CavernHelper.CrystalBombFieldRenderer")]
    public sealed class CrystalBombFieldRenderer : SeekerBarrierRendererState
    {
        public CrystalBombFieldRenderer(ISourceEngineContext engine)
            : base(engine, (ISourceRenderEnvironment)engine, PackedColor.Purple, .45f, 0f)
        {
        }
    }
}
