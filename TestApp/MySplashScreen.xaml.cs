namespace TestApp
{
    using SplashScreen;
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    [SplashScreen(MinimumVisibilityDuration = double.MaxValue, FadeoutDuration = 0.5)]
    public partial class MySplashScreen
    {
        public MySplashScreen()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the file description.
        /// </summary>
        public FileVersionInfo FileVersionInfo { get; } = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
    }
}
