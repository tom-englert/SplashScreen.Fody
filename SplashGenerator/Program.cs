using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SplashGenerator
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var assemblyFilePath = Environment.GetEnvironmentVariable("AssemblyFile");
                var controlTypeName = Environment.GetEnvironmentVariable("ControlType");
                var referenceCopyLocalPaths = Environment.GetEnvironmentVariable("ReferenceLocalPaths")?.Split('|');

                var data = GenerateBase64EncodedBitmap(assemblyFilePath, controlTypeName, referenceCopyLocalPaths);

                Console.Write(data ?? ErrorMessage(@"Error generating stream."));
            }
            catch (Exception ex)
            {
                Console.Write(ErrorMessage(ex));
            }
        }

        private const string ErrorPrefix = "!! ";

        private static string ErrorMessage(object value)
        {
            return ErrorPrefix + value;
        }

        private static string GenerateBase64EncodedBitmap(string assemblyFilePath, string controlTypeName, IEnumerable<string> referenceCopyLocalPaths)
        {
            var assemblyNames = referenceCopyLocalPaths
                .Select(TryGetAssemblyName)
                .Where(assembly => assembly != null)
                .ToDictionaryDistinct(item => item.FullName, StringComparer.OrdinalIgnoreCase);

            AppDomain.CurrentDomain.AssemblyResolve += (sender, e)
                => assemblyNames.TryGetValue(e.Name, out var assemblyName) ? Assembly.LoadFile(new Uri(assemblyName.CodeBase).LocalPath) : null;

            var targetAssembly = Assembly.LoadFile(assemblyFilePath);
            var controlType = GetLoadableTypes(targetAssembly).FirstOrDefault(type => string.Equals(type.FullName, controlTypeName, StringComparison.OrdinalIgnoreCase));
            if (controlType == null)
            {
                return ErrorMessage($"The project does not contain a type named '{controlTypeName}'. Add a user control named {controlTypeName}.xaml as a template for your splash screen.");
            }

            var dispatcher = Dispatcher.CurrentDispatcher;

            var data = default(string);

            dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            {
                var control = CreateControl(controlType);
                if (control == null)
                {
                    data = ErrorMessage($"Type {controlType} is not a UIElement with a default constructor. You need to have a user control named {controlType.Name}.xaml as a template for your splash screen.");
                }
                else
                {
                    dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
                    {
                        data = GenerateBitmap(control);
                    }));
                }
            }));

            dispatcher.BeginInvokeShutdown(DispatcherPriority.ContextIdle);
            Dispatcher.Run();

            return data ?? ErrorMessage("Unknown Error");
        }

        [System.Diagnostics.DebuggerNonUserCode]
        public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Return only the types that could successfully be reflected. The splash screen should be among it.
                return ex.Types.Where(type => type != null);
            }
        }

        private static string GenerateBitmap(UIElement control)
        {
            try
            {
                control.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var desiredSize = control.DesiredSize;
                control.Arrange(new Rect(0, 0, desiredSize.Width, desiredSize.Height));

                var bitmap = new RenderTargetBitmap((int)desiredSize.Width, (int)desiredSize.Height, 96, 96, PixelFormats.Pbgra32);

                bitmap.Render(control);

                var encoder = new PngBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using var stream = new MemoryStream();
                encoder.Save(stream);
                stream.Flush();
                var buffer = stream.GetBuffer();
                return Convert.ToBase64String(buffer);
            }
            catch (Exception ex)
            {
                return ErrorMessage($"Error generating bitmap: {ex}");
            }
        }

        private static UIElement CreateControl(Type controlType)
        {
            try
            {
                return (UIElement)Activator.CreateInstance(controlType);
            }
            catch
            {
                return default;
            }
        }

        private static AssemblyName TryGetAssemblyName(string fileName)
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(fileName);

                if (string.IsNullOrEmpty(assemblyName.CodeBase))
                {
                    assemblyName.CodeBase = new Uri(fileName, UriKind.Absolute).ToString();
                }

                return assemblyName;
            }
            catch
            {
                return null;
            }
        }

        private static Dictionary<TKey, TElement> ToDictionaryDistinct<TKey, TElement>(this IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            var dictionary = new Dictionary<TKey, TElement>(comparer);

            foreach (var element in source)
            {
                dictionary[keySelector(element)] = element;
            }

            return dictionary;
        }
    }
}
