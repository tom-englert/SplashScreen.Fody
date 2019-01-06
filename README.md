### This is an add-in for [Fody](https://github.com/Fody/Fody/) 

[![Build status](https://ci.appveyor.com/api/projects/status/TODO)](https://ci.appveyor.com/project/tom-englert/splashscreen-fody) 
[![NuGet Status](http://img.shields.io/nuget/v/SplashScreen.Fody.svg?style=flat-square)](https://www.nuget.org/packages/SplashScreen.Fody)

![Icon](Icon.png)

The default [WPF Splash Screen](https://docs.microsoft.com/en-us/dotnet/framework/wpf/app-development/how-to-add-a-splash-screen-to-a-wpf-application) 
implementation only supports a static bitmap, which is very unhandy.
With this add-in you can easily design your splash screen as a WPF Control, utilizing all WPF features, as you would expect for a WPF application.

It also gives you some extra control over the behavior without the need to write extra code.
All you have to provide is a WPF Control with the splash screen design. 
You can use all design features of WPF, e.g. file and version info can be read dynamically via binding. 
However due to the fact that the final splash is a bitmap, animations are not supported.

### How to use

To have a dynamically designable splash screen in your application, simply add a WPF UserControl to your 
applications assembly, design it, and apply the `[SplashScreen]` attribute to it:

```C#
/// <summary>
/// Interaction logic for MySplashScreen.xaml
/// </summary>
[SplashScreen(MinimumVisibilityDuration = 4, FadeoutDuration = 1)]
public partial class MySplashScreen
{
    public MySplashScreen()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the file version info.
    /// </summary>
    public FileVersionInfo FileVersionInfo { get; } = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
}
```

You can also control the minimum visibility duration and fadeout duration via this attribute.

### What it does

- Generates a bitmap image from your control and embed it into the assembly.
- Removes the user control and it's associated xaml resource.
- Merges the code to control the splash screen into your assembly, so you don't have any additional dependencies.
- Injects the code to initialize the splash screen into your applications entry point.

### Controlling the splash behavior

Beside configuring the minimum visibility and default fadeout duration, you can also close the splash screen manually.
The `SplashScreenAdapter` class has two static methods to achieve this, one to fade out the splash, and one to close it immediately:
```C#
SplashScreenAdapter.CloseSplashScreen();
```
or
```C#
SplashScreenAdapter.CloseSplashScreen(TimeSpan.FromSeconds(5));
```

### Things to consider
- Do **not** add a static image with the build action `SplashScreen` to your project, 
  as described [here](https://docs.microsoft.com/en-us/dotnet/framework/wpf/app-development/how-to-add-a-splash-screen-to-a-wpf-application)
- The WPF control marked with the `[SplashScreen]` attribute will be removed, so do **not** use it anywhere else.
- If showing an error message, make sure to close the splash screen immediately, 
  using `SplashScreenAdapter.CloseSplashScreen()`, before you show a message box. 
  Do not use fade out, since during fade out it again becomes the active window, and you might end up 
  with the problem described here: [MessageBox with exception details immediately disappears if use splash screen in WPF 4.0](https://stackoverflow.com/questions/3891719/messagebox-with-exception-details-immediately-disappears-if-use-splash-screen-in)