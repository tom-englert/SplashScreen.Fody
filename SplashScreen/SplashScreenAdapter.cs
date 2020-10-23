// ReSharper disable IdentifierTypo
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace SplashScreen
{
    /// <summary>
    /// Adapter class to control the <see cref="System.Windows.SplashScreen"/>.
    /// </summary>
    public class SplashScreenAdapter
    {
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;

        private static SplashScreenAdapter _adapterInstance;

        private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };

        private readonly double _minimumVisibilityDuration;
        private readonly double _fadeoutDuration;
        private readonly DateTime _startTime = DateTime.Now;

        private static bool _splashScreenCloseRequested;
        private System.Windows.SplashScreen _physicalInstance;

        internal SplashScreenAdapter(string splashBitmapResourceName, double minimumVisibilityDuration, double fadeoutDuration)
        {
            if (_splashScreenCloseRequested)
                return;

            _minimumVisibilityDuration = minimumVisibilityDuration;
            _fadeoutDuration = fadeoutDuration;

            _physicalInstance = new System.Windows.SplashScreen(splashBitmapResourceName);
            _physicalInstance.Show(false, true);

            var hWndSplash = NativeMethods.GetActiveWindow();
            NativeMethods.SetActiveWindow(IntPtr.Zero);

            Hook.HookWindow(hWndSplash);

            _timer.Tick += Timer_Tick;
            _timer.Start();

            NativeMethods.SetActiveWindow(IntPtr.Zero);

            _adapterInstance = this;
        }

        /// <summary>
        /// Closes the splash screen immediately. Use this before showing error messages, else the message window might disappear with the fading out splash screen.
        /// </summary>
        public static void CloseSplashScreen()
        {
            CloseSplashScreen(TimeSpan.Zero);
        }

        /// <summary>
        /// Closes the splash screen using the specified fadeout duration. Use this before showing error messages, else the message window might disappear with the fading out splash screen.
        /// </summary>
        public static void CloseSplashScreen(TimeSpan fadeoutDuration)
        {
            _splashScreenCloseRequested = true;
            _adapterInstance?.Close(fadeoutDuration);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_splashScreenCloseRequested)
            {
                if (Application.Current?.MainWindow?.IsLoaded != true)
                    return;

                if ((DateTime.Now - _startTime).TotalSeconds <= _minimumVisibilityDuration)
                    return;
            }

            Hook.UnHookWindow();

            _timer.Stop();

            _physicalInstance?.Close(TimeSpan.FromSeconds(_fadeoutDuration));
            _physicalInstance = null;
        }

        private void Close(TimeSpan fadeoutDuration)
        {
            _physicalInstance?.Close(fadeoutDuration);
            _physicalInstance = null;
            _adapterInstance = null;
        }

        private static class Hook
        {
            private delegate IntPtr WinProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

            private const int GWL_WNDPROC = -4;

            private const uint WM_ENABLE = 0x000A;
            private const uint WM_NCLBUTTONDOWN = 0x00A1;
            private const uint WM_LBUTTONDOWN = 0x0201;
            private const uint WM_ACTIVATEAPP = 0x001C;
            private const uint WM_ACTIVATE = 0x0006;


            private static readonly WinProc _windowProcDelegate = WindowProc;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            private static readonly IntPtr _windowProc = Marshal.GetFunctionPointerForDelegate(_windowProcDelegate);

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            private static IntPtr _oldWndProc;

            private static IntPtr _winHook;

            //Implementation

            public static void HookWindow(IntPtr winHandle)
            {
                if (winHandle == IntPtr.Zero)
                    return;

                _winHook = winHandle;

                _oldWndProc = NativeMethods.SetWindowLongPtr(_winHook, GWL_WNDPROC, _windowProc);
            }

            [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
            public static void UnHookWindow()
            {
                if (_winHook == IntPtr.Zero)
                    return;

                NativeMethods.SetWindowLongPtr(_winHook, GWL_WNDPROC, _oldWndProc);

                _winHook = IntPtr.Zero;
            }

            private static IntPtr WindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
            {
                switch (uMsg)
                {
                    case WM_LBUTTONDOWN:
                    case WM_NCLBUTTONDOWN:
                        _splashScreenCloseRequested = true;
                        return (IntPtr)1;

                    case WM_ENABLE:
                        if (wParam == IntPtr.Zero)
                        {
                            // ensure splash is always enabled, else we can't click it away.
                            NativeMethods.EnableWindow(hWnd, true);
                            return (IntPtr)1;
                        }
                        break;

                    case WM_ACTIVATEAPP:
                        if (wParam == IntPtr.Zero)
                        {
                            NativeMethods.SetWindowPos(hWnd, (IntPtr)HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
                        }
                        break;

                    case WM_ACTIVATE:
                        if (wParam == IntPtr.Zero)
                        {
                            NativeMethods.SetWindowPos(hWnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
                        }
                        break;

                }

                return NativeMethods.CallWindowProc(_oldWndProc, hWnd, uMsg, wParam, lParam);
            }
        }

        private static class NativeMethods
        {
            // This helper static method is required because the 32-bit version of user32.dll does not contain this API
            // (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
            // to the correct function (GetWindowLong in 32-bit mode and GetWindowLongPtr in 64-bit mode)
            [SuppressMessage("Microsoft.Interoperability", "CA1400:PInvokeEntryPointsShouldExist")]
            public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newValue)
            {
                return IntPtr.Size == 8
                    ? SetWindowLong64(hWnd, nIndex, newValue)
                    : new IntPtr(SetWindowLong32(hWnd, nIndex, newValue.ToInt32()));
            }

            [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
            private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

            [SuppressMessage("Microsoft.Interoperability", "CA1400:PInvokeEntryPointsShouldExist", Justification = "Only called on 64 bit")]
            [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
            private static extern IntPtr SetWindowLong64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

            [DllImport("user32.dll")]
            public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr GetActiveWindow();

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EnableWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SetActiveWindow(IntPtr hWnd);
        }
    }
}


