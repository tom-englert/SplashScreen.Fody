using System.Diagnostics;
using System.Reflection;
using SplashScreen;

namespace TestApp2
{
    public class Dummy
    {

    }

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

        public string Product { get; } = "MyProduct ™";
    }
}
