// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.CavernHelper.Entities.Floors
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.CavernHelper.IcyFloor")]
    [CustomEntity("cavern/icyfloor")]
    public class IcyFloor : SceneEntityState
    {
        private readonly List<Sprite> tiles;
        private readonly ISourceEngineContext engine;

        public IcyFloor(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            Depth = 1999;
            Collider = new HitboxColliderGeometry(data.Width, 2, 0f, 6);
            tiles = BuildSprite();
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            tiles.ForEach(delegate(Sprite t)
            {
                t.Play("ice", false, false);
                t.Rotation = -(float)Math.PI / 2;
            });
        }

        private List<Sprite> BuildSprite()
        {
            List<Sprite> list = new();
            int currentWidth = 0;
            while (currentWidth < Width)
            {
                string id;
                if (currentWidth == 0)
                {
                    id = "WallBoosterTop";
                }
                else if ((currentWidth + 16) > Width)
                {
                    id = "WallBoosterBottom";
                }
                else
                {
                    id = "WallBoosterMid";
                }

                Sprite sprite = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.SpriteFactory.Create(id);
                sprite.Position = new Vector2(currentWidth, 8);
                list.Add(sprite);
                Add(sprite);
                currentWidth += 8;
            }

            return list;
        }
    }
}
