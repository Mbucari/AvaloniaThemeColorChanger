using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AvaloniaThemeColorChanger
{
    public partial class MainWindow : Window
	{
		protected DataGridCollectionView ThemeColors { get; }
		private AvaloniaTheme ExistingTheme { get; } = AvaloniaTheme.GetLiveTheme(App.Current);

		public MainWindow()
        {
            InitializeComponent();
			var workingTheme = (AvaloniaTheme)ExistingTheme.Clone();
			ThemeColors = new(EnumerateThemeItemColors(workingTheme, ActualThemeVariant));

			DataContext = this;
		}

		protected void ResetColors()
			=> ResetTheme(ExistingTheme);

		protected void LoadDefaultColors()
		{
			if (App.DefaultTheme is AvaloniaTheme defaults)
				ResetTheme(defaults);
		}

		private void ResetTheme(AvaloniaTheme theme)
		{
			theme.ApplyTheme(App.Current, ActualThemeVariant);

			foreach (var i in ThemeColors.OfType<ThemeItemColor>())
			{
				i.SuppressSet = true;
				i.ThemeColor = theme.GetColor(ActualThemeVariant, i.ThemeItemName);
				i.SuppressSet = false;
			}
		}

		private static IEnumerable<ThemeItemColor> EnumerateThemeItemColors(AvaloniaTheme workingTheme, ThemeVariant themeVariant)
			=> workingTheme
			.GetThemeColors(themeVariant)
			.Select(kvp => new ThemeItemColor
			{
				ThemeItemName = kvp.Key,
				ThemeColor = kvp.Value,
				ColorSetter = c =>
				{
					workingTheme.SetColor(themeVariant, kvp.Key, c);
					workingTheme.ApplyTheme(App.Current, themeVariant);
				}
			});

		private class ThemeItemColor : INotifyPropertyChanged
		{
			public bool SuppressSet { get; set; }
			public required string ThemeItemName { get; init; }
			public required Action<Color> ColorSetter { get; init; }

			private Color _themeColor;

			public event PropertyChangedEventHandler? PropertyChanged;

			public Color ThemeColor
			{
				get => _themeColor;
				set
				{
					if (!_themeColor.Equals(value))
					{
						_themeColor = value;
						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThemeColor)));
						if (!SuppressSet)
							ColorSetter?.Invoke(_themeColor);
					}
				}
			}
		}
	}
}