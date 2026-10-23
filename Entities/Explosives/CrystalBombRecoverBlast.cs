// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.CavernHelper.Entities.Explosives
{
    [RuntimeTypeAlias("Celeste.Mod.CavernHelper.CrystalBomb+RecoverBlast")]
    public sealed class CrystalBombRecoverBlast : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private Sprite sprite;

        public CrystalBombRecoverBlast(ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration) => this.engine = engine;

        public static void Spawn(ISourceEngineContext engine, Vector2 position)
        {
            var blast = engine.EntityPools.Create<CrystalBombRecoverBlast>();
            blast.Position = position;
            ((ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene.QueueSourceEntity(blast);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            Depth = -199;
            if (sprite == null)
            {
                Add(sprite =
                    ((ISourceGameGraphicsEnvironment)engine).GameGraphics.SpriteFactory.Create("seekerShockWave"));
                sprite.OnLastFrame = _ => RemoveSelf();
            }

            sprite.Play("shockwave", true, false);
            sprite.Rate = 5f;
        }
    }
}
