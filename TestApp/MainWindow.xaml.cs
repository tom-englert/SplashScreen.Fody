namespace TestApp
{
    using System;
    using System.Reflection;
    using System.Windows.Input;

    using SplashScreen;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Content = Assembly.GetEntryAssembly()?.Location;
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SplashScreenAdapter.CloseSplashScreen(TimeSpan.FromSeconds(5));
        }
    }
}
