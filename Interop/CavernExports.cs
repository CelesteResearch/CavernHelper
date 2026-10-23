using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Physics.Collision.Shapes;
using Celeste.Mods.CavernHelper.Components.Explosives;
using MonoMod.ModInterop;

namespace Celeste.Mods.CavernHelper.Interop
{
    [ModExportName("CavernHelper")]
    public sealed class CavernExports
    {
        public SourceComponentState GetCrystalBombExplosionCollider(Action<Vector2> action,
            ColliderGeometry collider = null)
            => new CrystalBombExplosionCollider(action, collider);

        public SourceComponentState GetCrystalBombExploderCollider(ColliderGeometry collider = null)
            => new CrystalBombExploderCollider(collider);
    }
}
