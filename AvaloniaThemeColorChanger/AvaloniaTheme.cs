using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AvaloniaThemeColorChanger;

public class AvaloniaTheme : ICloneable
{
	/// <summary>The two theme variants supported by Fluent themes</summary>
	private static readonly FrozenSet<ThemeVariant> FluentVariants = [ThemeVariant.Light, ThemeVariant.Dark];

	/// <summary>Reusable color pallets for the two theme variants</summary>
	private static readonly FrozenDictionary<ThemeVariant, ColorPaletteResources> ColorPalettes
		= FluentVariants.ToFrozenDictionary(t => t, _ => new ColorPaletteResources());

	/// <summary>Theme color overrides</summary>
	private readonly Dictionary<ThemeVariant, Dictionary<string, Color>> ThemeColors;

	private AvaloniaTheme()
	{
		ThemeColors = FluentVariants.ToDictionary(t => t, _ => new Dictionary<string, Color>());
	}

	public Color GetColor(ThemeVariant themeVariant, string itemName)
	{
		ValidateThemeVariant(themeVariant);
		return ThemeColors[themeVariant].TryGetValue(itemName, out var color) ? color : default;
	}

	public void SetColor(string? themeVariant, Expression<Func<ColorPaletteResources, Color>> colorSelector, Color color)
		=> SetColor(FromVariantName(themeVariant), colorSelector, color);

	public void SetColor(ThemeVariant themeVariant, Expression<Func<ColorPaletteResources, Color>> colorSelector, Color color)
	{
		ValidateThemeVariant(themeVariant);

		if (colorSelector.Body.NodeType is ExpressionType.MemberAccess &&
			colorSelector.Body is MemberExpression memberExpression &&
			memberExpression.Member is PropertyInfo colorProperty &&
			colorProperty.DeclaringType == typeof(ColorPaletteResources))
		{
			SetColor(themeVariant, colorProperty.Name, color);
		}
	}

	public void SetColor(string? themeVariant, string itemName, Color itemColor)
		=> SetColor(FromVariantName(themeVariant), itemName, itemColor);

	public void SetColor(ThemeVariant themeVariant, string itemName, Color itemColor)
	{
		ValidateThemeVariant(themeVariant);
		ThemeColors[themeVariant][itemName] = itemColor;
	}

	public FrozenDictionary<string, Color> GetThemeColors(ThemeVariant themeVariant)
	{
		ValidateThemeVariant(themeVariant);
		return ThemeColors[themeVariant].ToFrozenDictionary();
	}

	/// <summary> Get the currently-active theme colors. </summary>
	public static AvaloniaTheme GetLiveTheme(Application app)
	{
		var theme = new AvaloniaTheme();

		foreach (var themeVariant in FluentVariants)
		{
			//Get the fluent theme colors
			foreach (var p in GetColorResourceProperties())
			{
				var color = (Color)p.GetValue(ColorPalettes[themeVariant])!;

				//The color isn't being overridden, so get the static resource value.
				if (color == default)
				{
					var staticResourceName = p.Name == nameof(ColorPaletteResources.RegionColor) ? "SystemRegionColor" : $"System{p.Name}Color";
					if (app.TryGetResource(staticResourceName, themeVariant, out var colorObj) && colorObj is Color c)
						color = c;
				}

				theme.ThemeColors[themeVariant][p.Name] = color;
			}
		}
		return theme;
	}

	public void ApplyTheme(Application app, string? themeVariant)
		=> ApplyTheme(app, FromVariantName(themeVariant));

	public void ApplyTheme(Application app, ThemeVariant themeVariant)
	{
		app.RequestedThemeVariant = themeVariant;
		themeVariant = app.ActualThemeVariant;
		ValidateThemeVariant(themeVariant);

		bool fluentColorChanged = false;

		//Set the fluent theme colors
		foreach (var p in GetColorResourceProperties())
		{
			if (ThemeColors[themeVariant].TryGetValue(p.Name, out var color) && color != default)
			{
				if (p.GetValue(ColorPalettes[themeVariant]) is not Color c || c != color)
				{
					p.SetValue(ColorPalettes[themeVariant], color);
					fluentColorChanged = true;
				}
			}
		}

		if (fluentColorChanged)
		{
			var oldFluent = app.Styles.OfType<FluentTheme>().Single();
			app.Styles.Remove(oldFluent);

			//We must make a new fluent theme and add it to the app for
			//the changes to the ColorPaletteResources to take effect.
			//Changes to the Libation-specific resources are instant.
			var newFluent = new FluentTheme();

			foreach (var kvp in ColorPalettes)
				newFluent.Palettes[kvp.Key] = kvp.Value;

			app.Styles.Add(newFluent);
		}
	}

	public object Clone()
	{
		var clone = new AvaloniaTheme();
		foreach (var t in ThemeColors)
		{
			clone.ThemeColors[t.Key] = t.Value.ToDictionary();
		}
		return clone;
	}

	private static IEnumerable<PropertyInfo> GetColorResourceProperties()
		=> typeof(ColorPaletteResources).GetProperties().Where(p => p.PropertyType == typeof(Color));

	[System.Diagnostics.StackTraceHidden]
	private static void ValidateThemeVariant(ThemeVariant themeVariant)
	{
		if (!FluentVariants.Contains(themeVariant))
			throw new InvalidOperationException("FluentTheme.Palettes only supports Light and Dark variants.");
	}

	private static ThemeVariant FromVariantName(string? variantName)
		=> variantName switch
		{
			nameof(ThemeVariant.Dark) => ThemeVariant.Dark,
			nameof(ThemeVariant.Light) => ThemeVariant.Light,
			_ => ThemeVariant.Default
		};
}
