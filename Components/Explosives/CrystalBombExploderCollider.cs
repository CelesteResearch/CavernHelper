// SPDX-License-Identifier: MIT

using System;
using Celeste.Core.Components;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Core.Reflection.Runtime;
using Celeste.Mods.CavernHelper.Entities.Explosives;

namespace Celeste.Mods.CavernHelper.Components.Explosives
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.CavernHelper.CrystalBombExploderCollider")]
    // Component that makes Crystal Bombs explode when they touch the collider.
    public class CrystalBombExploderCollider : SourceComponentState
    {
        public ColliderGeometry Collider;

        public CrystalBombExploderCollider(ColliderGeometry collider = null)
            : base(false, false)
        {
            Collider = collider;
        }

        internal bool Check(CrystalBomb bomb)
        {
            ColliderGeometry origCollider = ((ISourceEntityCollider)Entity).Collider;
            if (Collider != null)
            {
                ((ISourceEntityCollider)Entity).Collider = Collider;
            }

            bool result = false;

            if (SourceEntityCollision.Check(bomb, (ISourceEntityCollider)Entity))
            {
                result = true;
            }

            ((ISourceEntityCollider)Entity).Collider = origCollider;

            return result;
        }
    }
}
