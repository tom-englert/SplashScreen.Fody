using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

using FodyTools;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace SplashScreen.Fody
{
    public class ModuleWeaver : AbstractModuleWeaver
    {
        private const string SplashScreenAttributeName = "SplashScreen.SplashScreenAttribute";
        private const string MinimumVisibilityDurationPropertyName = "MinimumVisibilityDuration";
        private const string FadeoutDurationPropertyName = "FadeoutDuration";
        private const string SplashScreenAdapterTypeName = "SplashScreenAdapter";

        private const string SplashResourceName = "splash_a7675be0ade04430a1bd47ee14b34343.png";


        public override bool ShouldCleanReference => true;

        public override IEnumerable<string> GetAssembliesForScanning() => Enumerable.Empty<string>();

        public override void Execute()
        {
            // System.Diagnostics.Debugger.Launch();

            Execute(ModuleDefinition, this, AddinDirectoryPath, AssemblyFilePath, ReferenceCopyLocalPaths);
        }

        private static void Execute(ModuleDefinition moduleDefinition, ILogger logger, string addInDirectoryPath, string assemblyFilePath, IList<string> referenceCopyLocalPaths)
        {
            var entryPoint = moduleDefinition.EntryPoint;

            if (entryPoint == null)
            {
                logger.LogError("No entry point found in target module.");
                return;
            }

            TypeDefinition splashScreenControl;

            try
            {
                splashScreenControl = moduleDefinition.Types.Single(HasSplashScreenAttribute);
            }
            catch (Exception ex)
            {
                logger.LogError("No single class with the [SplashScreen] attribute found: " + ex.Message);
                return;
            }

            byte[] bitmapData;

            try
            {
                var frameworkIdentifier = GetTargetFrameworkName(moduleDefinition)?.Identifier ?? ".NETFramework";

                bitmapData = BitmapGenerator.Generate(logger, addInDirectoryPath, frameworkIdentifier, assemblyFilePath, splashScreenControl.FullName, referenceCopyLocalPaths);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return;
            }

            var splashScreenControlBamlResourceName = splashScreenControl.Name.ToLowerInvariant() + ".baml";

            ResourceHelper.UpdateResources(moduleDefinition, SplashResourceName, bitmapData, splashScreenControlBamlResourceName);

            moduleDefinition.Types.Remove(splashScreenControl);

            var attribute = GetSplashScreenAttribute(splashScreenControl)!;

            var minimumVisibilityDuration = attribute.GetPropertyValue(MinimumVisibilityDurationPropertyName, 4.0);
            var fadeoutDuration = attribute.GetPropertyValue(FadeoutDurationPropertyName, 1.0);

            var referencedModule = attribute.AttributeType.Resolve().Module;

            var adapterType = referencedModule.Types.Single(type => type.Name == SplashScreenAdapterTypeName);

            var importer = new CodeImporter(moduleDefinition);

            adapterType = importer.Import(adapterType);

            importer.ILMerge();

            var adapterTypeConstructor = adapterType.GetConstructors().Single(ctor => ctor.Parameters.Count == 3);

            entryPoint.Body.Instructions.InsertRange(0,
                Instruction.Create(OpCodes.Ldstr, SplashResourceName),
                Instruction.Create(OpCodes.Ldc_R8, minimumVisibilityDuration),
                Instruction.Create(OpCodes.Ldc_R8, fadeoutDuration),
                Instruction.Create(OpCodes.Newobj, moduleDefinition.ImportReference(adapterTypeConstructor)),
                Instruction.Create(OpCodes.Pop)
            );
        }

        private static bool HasSplashScreenAttribute(TypeDefinition type)
        {
            return null != GetSplashScreenAttribute(type);
        }

        private static CustomAttribute? GetSplashScreenAttribute(ICustomAttributeProvider type)
        {
            return type.GetAttribute(SplashScreenAttributeName);
        }

        private static FrameworkName? GetTargetFrameworkName(ModuleDefinition moduleDefinition)
        {
            return moduleDefinition.Assembly
                .CustomAttributes
                .Where(attr => attr.AttributeType.FullName == typeof(TargetFrameworkAttribute).FullName)
                .Select(attr => attr.ConstructorArguments.Select(arg => arg.Value as string).FirstOrDefault())
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => new FrameworkName(name))
                .FirstOrDefault();
        }
    }
}
