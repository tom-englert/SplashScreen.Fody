namespace SplashScreen.Fody
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using FodyTools;

    using global::Fody;

    using JetBrains.Annotations;

    using Mono.Cecil;

    public class ModuleWeaver : AbstractModuleWeaver
    {
        const string splashResourceName = "Splash_A7675BE0ADE04430A1BD47EE14B34343.png";

        public override void Execute()
        {
            System.Diagnostics.Debugger.Launch();

            var splashScreenControl = ModuleDefinition.Types.FirstOrDefault(HasSplashScreenAttribute);

            if (splashScreenControl == null)
            {
                throw new WeavingException("No class with the [SplashScreen] attribute found.");
            }

            var bitmapData = BitmapGenerator.Generate(AddinDirectoryPath, AssemblyFilePath, splashScreenControl.FullName, ReferenceCopyLocalPaths);

            ResourceHelper.AddResource(ModuleDefinition, splashResourceName, bitmapData);
        }

        [NotNull]
        public override IEnumerable<string> GetAssembliesForScanning() => Enumerable.Empty<string>();

        private bool HasSplashScreenAttribute(TypeDefinition type)
        {
            return null != type.GetAttribute("SplashScreen.SplashScreenAttribute");
        }
    }
}
