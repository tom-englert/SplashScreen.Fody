using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Xunit;

namespace Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var parameters = new SplashGenerator.GeneratorParameters
            {
                AssemblyFilePath = typeof(TestApp.App).Assembly.Location,
                ControlTypeName = typeof(TestApp.MySplashScreen).FullName,
                ReferenceCopyLocalPaths = new List<string>()
            };

            var startInfo = new ProcessStartInfo(typeof(SplashGenerator.GeneratorParameters).Assembly.Location)
            {
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            var process = Process.Start(startInfo);
            process.StandardInput.WriteLine(JsonConvert.SerializeObject(parameters, Formatting.None));
            var data = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var binary = Convert.FromBase64String(data);
            File.WriteAllBytes(@"c:\temp\test.png", binary);
        }
    }
}
