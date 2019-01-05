namespace SplashScreen.Fody
{
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;

    using FodyTools;

    using global::Fody;

    using JetBrains.Annotations;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    public class ModuleWeaver : AbstractModuleWeaver
    {
        const string splashResourceName = "splash_a7675be0ade04430a1bd47ee14b34343.png";

        public override void Execute()
        {
            // System.Diagnostics.Debugger.Launch();

            var splashScreenControl = ModuleDefinition.Types.SingleOrDefault(HasSplashScreenAttribute);

            if (splashScreenControl == null)
            {
                throw new WeavingException("No class with the [SplashScreen] attribute found.");
            }

            var bitmapData = BitmapGenerator.Generate(AddinDirectoryPath, AssemblyFilePath, splashScreenControl.FullName, ReferenceCopyLocalPaths);

            ResourceHelper.AddResource(ModuleDefinition, splashResourceName, bitmapData, (splashScreenControl.Name + ".baml").ToLowerInvariant());

            ModuleDefinition.Types.Remove(splashScreenControl);

            var importer = new CodeImporter(ModuleDefinition);

            var attribute = GetSplashScreenAttribute(splashScreenControl);

            var minimumVisibilityDuration = attribute.GetPropertyValue("MinimumVisibilityDuration", 4.0);
            var fadeoutDuration = attribute.GetPropertyValue("FadeoutDuration", 1.0);

            var referencedModule = attribute.AttributeType.Resolve().Module;

            var adapterType = referencedModule.Types.Single(type => type.Name == "SplashScreenAdapter");

            adapterType = importer.Import(adapterType);

            importer.ILMerge();

            var adapterTypeConstructor = adapterType.GetConstructors().Single(ctor => ctor.Parameters.Count == 3);

            var entryPoint = ModuleDefinition.EntryPoint;

            if (entryPoint == null)
            {
                throw new WeavingException("No entry point found in target module.");
            }

            entryPoint.Body.Instructions.InsertRange(0,
                Instruction.Create(OpCodes.Ldstr, splashResourceName),
                Instruction.Create(OpCodes.Ldc_R8, minimumVisibilityDuration),
                Instruction.Create(OpCodes.Ldc_R8, fadeoutDuration),
                Instruction.Create(OpCodes.Newobj, ModuleDefinition.ImportReference(adapterTypeConstructor)),
                Instruction.Create(OpCodes.Pop)
                );
        }

        [NotNull]
        public override IEnumerable<string> GetAssembliesForScanning() => Enumerable.Empty<string>();

        private bool HasSplashScreenAttribute(TypeDefinition type)
        {
            return null != GetSplashScreenAttribute(type);
        }

        private static CustomAttribute GetSplashScreenAttribute(TypeDefinition type)
        {
            return type.GetAttribute("SplashScreen.SplashScreenAttribute");
        }
    }
}
