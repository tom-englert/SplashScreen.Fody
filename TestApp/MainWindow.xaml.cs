namespace TestApp
{
    using System;

    using SplashScreen;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            SplashScreenAdapter.CloseSplashScreen(TimeSpan.FromSeconds(5));
        }
    }
}
