using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;

namespace AvaloniaThemeColorChanger
{
    public partial class App : Application
    {
        public static new Application Current => Application.Current ?? throw new InvalidOperationException();
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            //Get the default (compile-time) theme colors
            DefaultTheme = AvaloniaTheme.GetLiveTheme(this);

            //Clone the default theme so we can keep track of the defaults
			//Make runtime changes to the default theme and apply
			((AvaloniaTheme)DefaultTheme.Clone())
				.SetColor(ActualThemeVariant, c => c.Accent, Colors.DarkOrchid)
                .SetColor(ActualThemeVariant, c => c.AltHigh, Colors.AliceBlue)
			    .SetColor(ActualThemeVariant, c => c.RegionColor, Colors.AntiqueWhite)
			    .ApplyTheme(this, ActualThemeVariant);
		}

        public static AvaloniaTheme? DefaultTheme { get; private set; }

        public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

			base.OnFrameworkInitializationCompleted();
        }
    }
}