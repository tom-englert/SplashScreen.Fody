namespace SplashScreen.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    public class BitmapGenerator : MemoryStream
    {
        private Exception _exception;

        public BitmapGenerator([NotNull] string assemblyFilePath, [NotNull] string controlTypeName, [NotNull] IEnumerable<string> referenceCopyLocalPaths)
        {
            var assemblyNames = referenceCopyLocalPaths
                .Select(TryGetAssemblyName)
                .Where(assembly => assembly != null)
                .ToDictionary(item => item.FullName);

            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => assemblyNames.TryGetValue(e.Name, out var assemblyName) ? Assembly.Load(assemblyName) : null;

            var thread = new Thread(() => { GenerateInStaThread(assemblyFilePath, controlTypeName); }) { Name = "STA helper thread" };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (_exception != null)
            {
                throw _exception;
            }
        }

        [NotNull]
        internal static byte[] Generate([NotNull] string addInDirectory, [NotNull] string assemblyFilePath, [NotNull] string controlTypeName, [NotNull] IList<string> referenceCopyLocalPaths)
        {
            const string friendlyName = "Temporary domain for SplashScreen.Fody";

            var appDomain = AppDomain.CreateDomain(friendlyName, null, addInDirectory, string.Empty, false);

            try
            {
                var assemblyFullName = typeof(BitmapGenerator).Assembly.FullName;
                var typeName = typeof(BitmapGenerator).FullName;

                const BindingFlags bindingFlags = BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

                var arguments = new object[] { assemblyFilePath, controlTypeName, referenceCopyLocalPaths };

                var target = appDomain.CreateInstanceAndUnwrap(assemblyFullName, typeName, true, bindingFlags, null, arguments, CultureInfo.CurrentCulture, null);

                return ((MemoryStream)target).GetBuffer();

            }
            catch (Exception ex)
            {
                throw ex.GetBaseException();
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        private void GenerateInStaThread([NotNull] string assemblyFilePath, [NotNull] string controlTypeName)
        {
            try
            {
                var targetAssembly = Assembly.LoadFile(assemblyFilePath);
                var controlType = targetAssembly.GetTypes().FirstOrDefault(type => string.Equals(type.FullName, controlTypeName, StringComparison.OrdinalIgnoreCase));

                if (controlType == null)
                    throw new InvalidOperationException($"The project does not contain a type named '{controlTypeName}'. Add a user control named {controlTypeName}.xaml as a template for your splash screen.");

                var dispatcher = Dispatcher.CurrentDispatcher;

                UIElement control = null;

                dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => control = CreateControl(controlType)));
                dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() => GenerateBitmap(control)));
                dispatcher.BeginInvokeShutdown(DispatcherPriority.ContextIdle);

                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        private void GenerateBitmap([NotNull] UIElement control)
        {
            control.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var desiredSize = control.DesiredSize;
            control.Arrange(new Rect(0, 0, desiredSize.Width, desiredSize.Height));

            var bitmap = new RenderTargetBitmap((int)desiredSize.Width, (int)desiredSize.Height, 96, 96, PixelFormats.Pbgra32);

            bitmap.Render(control);

            var encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(this);
        }

        [NotNull]
        private static UIElement CreateControl([NotNull] Type controlType)
        {
            try
            {
                return (UIElement)Activator.CreateInstance(controlType);
            }
            catch
            {
                throw new InvalidOperationException($"Type {controlType} is not a UIElement with a default constructor. You need to have a user control named {controlType.Name}.xaml as a template for your splash screen.");
            }
        }

        private static AssemblyName TryGetAssemblyName([NotNull] string fileName)
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