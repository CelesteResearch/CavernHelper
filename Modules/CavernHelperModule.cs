using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Interop;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Areas;
using Celeste.Mods.CavernHelper.Entities.Explosives;
using Celeste.Mods.CavernHelper.Entities.Fields;
using Celeste.Mods.CavernHelper.Interop;
using Celeste.Mods.CavernHelper.Runtime;
using Celeste.Simulation.Gameplay.Player.Dash;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.CavernHelper.Modules
{
    public sealed class CavernHelperModule(ISourceEngineContext engine) : SourceEverestModule(engine)
    {
        private readonly List<IDisposable> registrations = new();

        public override void Load()
        {
            try
            {
                registrations.Add(((ISourceModInteropEnvironment)SourceEngine).ModInterop.RegisterOwned(
                    new SourceModInteropActualTypeBinding(typeof(CavernExports), new CavernExports())));
                registrations.Add(
                    SourceEngine.EntityPools.RegisterScoped(() => new CrystalBombRecoverBlast(SourceEngine)));
                registrations.Add(new Hook(typeof(OriginalLevelLoaderScene).GetMethod("LoadingThread",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    (Action<Action<OriginalLevelLoaderScene>, OriginalLevelLoaderScene>)LoadingThread));
                registrations.Add(new Hook(
                    typeof(PlayerBoostSequence).GetMethod(nameof(PlayerBoostSequence.BoostBegin)),
                    (Action<Action<PlayerBoostSequence>, PlayerBoostSequence>)BoostBegin));
            }
            catch
            {
                Unload();
                throw;
            }
        }

        public override void LoadContent(bool firstLoad) => CavernRuntime.For(SourceEngine).LoadContent();

        public override void Unload()
        {
            for (int i = registrations.Count - 1; i >= 0; i--) registrations[i].Dispose();
            registrations.Clear();
            CavernRuntime.For(SourceEngine).ClearContent();
        }

        private void LoadingThread(Action<OriginalLevelLoaderScene> orig, OriginalLevelLoaderScene loader)
        {
            if (ReferenceEquals(loader.EngineContext, SourceEngine))
                loader.Level.SourceScene.QueueSourceEntity(new CrystalBombFieldRenderer(SourceEngine));
            orig(loader);
        }

        private void BoostBegin(Action<PlayerBoostSequence> orig, PlayerBoostSequence sequence)
        {
            var player = sequence.Player;
            if (ReferenceEquals(player.EngineContext, SourceEngine)
                && ((IOriginalAreaDataResolver)SourceEngine).Get(((OriginalLevelSceneRoot)player.Scene).Session.AreaID)
                .SID
                == "exudias/2/CavernoftheAncients")
            {
                player.RefillDash();
                player.RefillStamina();
            }
            else orig(sequence);
        }
    }
}
