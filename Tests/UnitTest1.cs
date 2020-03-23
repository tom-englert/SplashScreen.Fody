using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var assemblyFilePath = typeof(TestApp.App).Assembly.Location;
            var controlTypeName = typeof(TestApp.MySplashScreen).FullName;
            var referenceCopyLocalPaths = new List<string>();
            var arguments = new[] {assemblyFilePath, controlTypeName}.Concat(referenceCopyLocalPaths);

            var startInfo = new ProcessStartInfo(typeof(SplashGenerator.Program).Assembly.Location)
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
            File.WriteAllBytes(@"c:\temp\test.png", binary);
        }
    }
}
