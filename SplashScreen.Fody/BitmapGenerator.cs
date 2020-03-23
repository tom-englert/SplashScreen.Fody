using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace SplashScreen.Fody
{
    public class BitmapGenerator
    {
        [NotNull]
        internal static byte[] Generate(string addInDirectoryPath, [NotNull] string frameworkIdentifier, [NotNull] string assemblyFilePath, [NotNull] string controlTypeName, [NotNull] IList<string> referenceCopyLocalPaths)
        {
            try
            {
                var root = addInDirectoryPath;
                var generatorPath = Path.Combine(root, frameworkIdentifier, "SplashGenerator.exe");
                var arguments = new[] {assemblyFilePath, controlTypeName}.Concat(referenceCopyLocalPaths);

                var startInfo = new ProcessStartInfo(generatorPath)
                {
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    Arguments = string.Join(" ",arguments.Select(arg => "\"" + arg + "\""))
                };

                var process = Process.Start(startInfo);
                var data = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var binary = Convert.FromBase64String(data);

                return binary;

            }
            catch (Exception ex)
            {
                throw ex.GetBaseException();
            }
        }

    }
}