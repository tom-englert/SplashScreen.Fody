using System;
using System.Collections.Generic;

namespace SplashGenerator
{
    [Serializable]
    public class GeneratorParameters
    {
        public string? AssemblyFilePath { get; set; }
        public string? ControlTypeName { get; set; }
        public  ICollection<string>? ReferenceCopyLocalPaths { get; set; }
    }
}
