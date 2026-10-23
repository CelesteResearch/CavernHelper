// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Core.Reflection.Runtime;
using Celeste.Mods.CavernHelper.Entities.Explosives;

namespace Celeste.Mods.CavernHelper.Components.Explosives
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.CavernHelper.CrystalBombExplosionCollider")]
    public class CrystalBombExplosionCollider : SourceComponentState
    {
        public Action<Vector2> OnExplode;
        public ColliderGeometry Collider;

        public CrystalBombExplosionCollider(Action<Vector2> onExplode, ColliderGeometry collider = null)
            : base(false, false)
        {
            OnExplode = onExplode;
            Collider = collider;
        }

        internal void Check(CrystalBomb bomb)
        {
            if (OnExplode != null)
            {
                ColliderGeometry origCollider = ((ISourceEntityCollider)Entity).Collider;
                if (Collider != null)
                {
                    ((ISourceEntityCollider)Entity).Collider = Collider;
                }

                if (SourceEntityCollision.Check(bomb, (ISourceEntityCollider)Entity))
                {
                    OnExplode.Invoke(bomb.Position);
                }

                ((ISourceEntityCollider)Entity).Collider = origCollider;
            }
        }
    }
}
