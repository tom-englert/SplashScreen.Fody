namespace SplashScreen.Fody
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System.Resources;

    using JetBrains.Annotations;

    using Mono.Cecil;

    internal static class ResourceHelper
    {
        public static void UpdateResources([NotNull] ModuleDefinition module, [NotNull] string resourceName, [NotNull] byte[] resourceData, [NotNull] params string[] resourcesToRemove)
        {
            var moduleResources = module.Resources;

            var originalResource = moduleResources
                .OfType<EmbeddedResource>()
                .FirstOrDefault(r => r.Name.EndsWith(@".g.resources", StringComparison.OrdinalIgnoreCase));

            if (originalResource == null)
                throw new InvalidOperationException($"The assembly '{module.FileName}' does not seem to be a valid WPF executable, it does not contain WPF resources.");

            var newResourceData = UpdateResources(originalResource.GetResourceStream(), resourceName, resourceData, resourcesToRemove);
            var newResource = new EmbeddedResource(originalResource.Name, originalResource.Attributes, newResourceData);

            moduleResources.Remove(originalResource);
            moduleResources.Add(newResource);
        }

        [NotNull]
        private static byte[] UpdateResources([NotNull] Stream originalResource, [NotNull] string resourceName, [NotNull] byte[] resourceData, [NotNull] string[] resourcesToRemove)
        {
            using (var targetStream = new MemoryStream())
            {
                using (var writer = new ResourceWriter(targetStream))
                {
                    using (var reader = new ResourceReader(originalResource))
                    {
                        writer.AddResource(resourceName, new MemoryStream(resourceData));

                        foreach (DictionaryEntry item in reader)
                        {
                            var key = (string)item.Key;

                            if (resourceName.Equals(key, StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidOperationException($"Target assembly already contains a resource named '{resourceName}'");
                            }

                            if (resourcesToRemove.Contains(key))
                            {
                                continue;
                            }

                            writer.AddResource(key, (Stream)item.Value);
                        }
                    }
                }

                return targetStream.GetBuffer();
            }
        }
    }
}