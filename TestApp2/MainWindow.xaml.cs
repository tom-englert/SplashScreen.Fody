using System;
using System.Windows.Input;
using SplashScreen;

namespace TestApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SplashScreenAdapter.CloseSplashScreen(TimeSpan.FromSeconds(5));
        }
    }
}
