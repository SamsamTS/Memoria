using System;
using System.IO;

namespace Memoria
{
    public sealed class KeyItemLoader : SingleFileExporter
    {
        private const String Prefix = "$key";

        protected override string TypeName => nameof(KeyItemLoader);
        protected override string ExportPath => ModTextResources.Export.KeyItems;

        protected override TxtEntry[] PrepareEntries()
        {
            String[] itemNames = EmbadedSentenseLoader.LoadSentense(EmbadedTextResources.KeyItemNames);
            String[] itemHelps = EmbadedSentenseLoader.LoadSentense(EmbadedTextResources.KeyItemHelps);
            String[] itemDescs = EmbadedSentenseLoader.LoadSentense(EmbadedTextResources.KeyItemDescriptions);

            return KeyItemFormatter.Build(Prefix, itemNames, itemHelps, itemDescs);
        }
    }
}