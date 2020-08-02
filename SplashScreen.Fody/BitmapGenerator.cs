using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

using Fody;

using FodyTools;

namespace SplashScreen.Fody
{
    public static class BitmapGenerator
    {
        internal static byte[] Generate(ILogger logger, string addInDirectoryPath, string frameworkIdentifier, string assemblyFilePath, string controlTypeName, IList<string> referenceCopyLocalPaths)
        {
            try
            {
                var root = addInDirectoryPath;
                var generatorFolder = Path.Combine(root, frameworkIdentifier);
                var assemblyParentFolderName = Path.GetFileName(Path.GetDirectoryName(assemblyFilePath));
                var runtimeSpecificGeneratorFolder = Path.Combine(generatorFolder, assemblyParentFolderName);
                if (Directory.Exists(runtimeSpecificGeneratorFolder))
                {
                    generatorFolder = runtimeSpecificGeneratorFolder;
                }

                var generatorPath = Path.Combine(generatorFolder, "SplashGenerator.exe");
                logger.LogInfo($"Bitmap generator: {generatorPath}");

                var arguments = new[] { assemblyFilePath, controlTypeName }.Concat(referenceCopyLocalPaths);

                var startInfo = new ProcessStartInfo(generatorPath)
                {
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    Arguments = string.Join(" ", arguments.Select(arg => "\"" + arg + "\""))
                };

                var process = Process.Start(startInfo);
                var data = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if ((process.ExitCode != 0) || string.IsNullOrEmpty(data))
                {
                    throw new WeavingException("Unknown error generating the splash bitmap.");
                }

                if (data.StartsWith("!! "))
                {
                    throw new WeavingException("Bitmap generator failed: " + data.Substring(3));
                }

                try
                {
                    var binary = Convert.FromBase64String(data);
                    return binary;
                }
                catch
                {
                    throw new WebException("Bitmap generator returned unexpected data: "+ data);
                }
            }
            catch (Exception ex)
            {
                throw ex is WeavingException ? ex : ex.GetBaseException();
            }
        }
    }
}