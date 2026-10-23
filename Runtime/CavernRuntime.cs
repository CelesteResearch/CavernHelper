using System;
using System.Runtime.CompilerServices;
using System.Xml;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Registry;
using Celeste.Everest.Content.Types.Text;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Graphics.Content;

namespace Celeste.Mods.CavernHelper.Runtime
{
    public sealed class CavernRuntime
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, CavernRuntime> Runtimes = new();
        public static CavernRuntime For(ISourceEngineContext engine) => Runtimes.GetValue(engine, value => new(value));
        private readonly ISourceEngineContext engine;
        public SpriteBank SpriteBank { get; private set; }
        private CavernRuntime(ISourceEngineContext engine) => this.engine = engine;

        public void LoadContent()
        {
            var content = ((IEverestContentRegistryEnvironment)engine).SourceContentRegistry;
            if (!content.TryGet<AssetTypeXml>("Graphics/CavernSprites", out var asset))
                throw new InvalidOperationException("CavernHelper requires Graphics/CavernSprites.xml");
            var xml = new XmlDocument();
            using (var stream = asset.Stream) xml.Load(stream);
            SpriteBank = new SpriteBank(engine, ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas, xml);
        }

        public void ClearContent() => SpriteBank = null;
    }
}
