using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace SplashGenerator
{
    internal static class BitmapGenerator
    {
        public static string? Generate(GeneratorParameters generatorParameters)
        {
            var referenceCopyLocalPaths = generatorParameters.ReferenceCopyLocalPaths ?? throw new ArgumentException("ReferenceCopyLocalPath");
            var assemblyFilePath = generatorParameters.AssemblyFilePath ?? throw new ArgumentException("AssemblyFilePath");
            var controlTypeName = generatorParameters.ControlTypeName ?? throw new ArgumentException("ControlTypeName");

            var assemblyNames = referenceCopyLocalPaths
                .Select(TryGetAssemblyName)
                .Where(assembly => assembly != null)
                .ToDictionary(item => item!.FullName);

            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => assemblyNames.TryGetValue(e.Name!, out var assemblyName) ? Assembly.Load(assemblyName!) : null;

            var targetAssembly = Assembly.LoadFile(assemblyFilePath);
            var controlType = targetAssembly.GetTypes().FirstOrDefault(type => string.Equals(type.FullName, controlTypeName, StringComparison.OrdinalIgnoreCase));

            if (controlType == null)
                throw new InvalidOperationException($"The project does not contain a type named '{controlTypeName}'. Add a user control named {controlTypeName}.xaml as a template for your splash screen.");

            var dispatcher = Dispatcher.CurrentDispatcher;

            var stream = default(string);

            dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {
                var control = CreateControl(controlType);
                if (control == null)
                    throw new InvalidOperationException($"Type {controlType} is not a UIElement with a default constructor. You need to have a user control named {controlType.Name}.xaml as a template for your splash screen.");

                dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() => stream = GenerateBitmap(control)));
            }));

            dispatcher.BeginInvokeShutdown(DispatcherPriority.ContextIdle);
            Dispatcher.Run();

            return stream;
        }

        private static string GenerateBitmap(UIElement control)
        {
            control.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var desiredSize = control.DesiredSize;
            control.Arrange(new Rect(0, 0, desiredSize.Width, desiredSize.Height));

            var bitmap = new RenderTargetBitmap((int)desiredSize.Width, (int)desiredSize.Height, 96, 96, PixelFormats.Pbgra32);

            bitmap.Render(control);

            var encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Flush();
                var buffer = stream.GetBuffer();
                return Convert.ToBase64String(buffer);
            }
        }

        private static UIElement? CreateControl(Type controlType)
        {
            try
            {
                return (UIElement?)Activator.CreateInstance(controlType);
            }
            catch
            {
                return default;
            }
        }

        private static AssemblyName? TryGetAssemblyName(string fileName)
        {
            try
            {
                return AssemblyName.GetAssemblyName(fileName);
            }
            catch
            {
                return null;
            }
        }
    }
}