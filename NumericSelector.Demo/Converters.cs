using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NumericSelector.Demo
{
	/// <summary>
	/// Converts a uniform Thickness to an int and back, so that BorderThickness can be driven
	/// with a BoundedNumericSelector (which works with integers).
	/// </summary>
	public class ThicknessToIntConverter : IValueConverter
	{
		// Source (Thickness) -> target (int): we take the left side as the representative.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value is Thickness t ? (int)Math.Round(t.Left) : 0;

		// Target (int) -> source (Thickness): a uniform border.
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> new Thickness(System.Convert.ToDouble(value, culture));
	}

	/// <summary>
	/// Maps the Value of a BoundedNumericSelector (0..23) to a color of the demo palette.
	/// It returns the color as "#FFRRGGBB" (for the selector's DetailText) but it also exposes
	/// the list, in the same order, so that the code-behind can use it to paint the Master.
	/// It is the single source of the palette: the XAML no longer declares a parallel copy.
	/// </summary>
	public class ColorIndexConverter : IValueConverter
	{
		public static readonly Color[] Palette =
		{
			Colors.Black,
			Colors.DimGray,
			Colors.Gray,
			Colors.Silver,
			Colors.LightGray,
			Colors.White,
			Colors.DarkRed,
			Colors.Crimson,
			Colors.Red,
			Colors.Orange,
			Colors.DarkOrange,
			Colors.Gold,
			Colors.Yellow,
			Colors.DarkGreen,
			Colors.SeaGreen,
			Colors.Green,
			Colors.LimeGreen,
			Colors.Teal,
			Colors.DarkBlue,
			Colors.RoyalBlue,
			Colors.DodgerBlue,
			Colors.SkyBlue,
			Colors.Purple,
			Colors.Transparent,
		};

		// Value (int) -> "#FFRRGGBB", to show it in the DetailText.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			if (i < 0 || i >= Palette.Length)
				return $"(#{i})";
			Color c = Palette[i];
			return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
		}

		// Not used: the selector does not get the color back.
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Maps the Value of a BoundedNumericSelector (0..n-1) to a system font.
	/// </summary>
	/// <remarks>
	/// There are TWO lists and the distinction matters: <see cref="All"/> is immutable and is
	/// the source of truth; <see cref="Filtered"/> is the one the filter let through and the
	/// only one the picker and this converter index into. If they drifted apart, the selector
	/// would show the name of one font and apply another. That is why <see cref="Filtered"/>
	/// is only ever written from <see cref="Filter"/>.
	/// </remarks>
	public class FontIndexConverter : IValueConverter
	{
		/// <summary>Every system font, sorted by name. It never changes.</summary>
		public static readonly FontFamily[] All =
			Fonts.SystemFontFamilies
				.OrderBy(f => f.Source, StringComparer.CurrentCultureIgnoreCase)
				.ToArray();

		/// <summary>The ones that survived the filter. It starts out being all of them.</summary>
		public static FontFamily[] Filtered { get; private set; } = All;

		/// <summary>
		/// Leaves in <see cref="Filtered"/> the fonts whose name contains <paramref name="text"/>
		/// (case insensitive) and returns how many are left. An empty filter returns all of
		/// them.
		/// </summary>
		public static int Filter(string? text)
		{
			string pattern = text?.Trim() ?? "";
			Filtered = pattern.Length == 0
				? All
				: All.Where(f => f.Source.Contains(pattern, StringComparison.CurrentCultureIgnoreCase))
					   .ToArray();
			return Filtered.Length;
		}

		// Value (int) -> the name of the font, to show it in the DetailText.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			if (Filtered.Length == 0)
				return "(no match)";

			// The range of the control is always at least 1 wide, so with a single match index 1
			// exists for the selector but not in the list. It is said out loud, not papered over:
			// the bench is there to show the real limits of the API.
			return (i >= 0 && i < Filtered.Length) ? Filtered[i].Source : $"(no font at #{i})";
		}

		// Not used: the selector does not get the font back (the index speaks for it).
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Maps the Value of a BoundedNumericSelector (0..2) to a FontStyle. Same pattern as
	/// <see cref="FontIndexConverter"/>: a static list shared between XAML and code-behind.
	/// </summary>
	public class FontStyleIndexConverter : IValueConverter
	{
		public static readonly FontStyle[] Styles =
		{
			FontStyles.Normal,
			FontStyles.Italic,
			FontStyles.Oblique,
		};

		// Value (int) -> the name of the style, to show it in the DetailText.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Styles.Length) ? Describe(Styles[i]) : $"(#{i})";
		}

		private static string Describe(FontStyle style) => style switch
		{
			_ when style == FontStyles.Normal => "Normal",
			_ when style == FontStyles.Italic => "Italic",
			_ when style == FontStyles.Oblique => "Oblique",
			_ => style.ToString(),
		};

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Maps the Value of a BoundedNumericSelector (0..4) to a FontWeight. Same pattern as
	/// <see cref="FontIndexConverter"/>: a static list shared between XAML and code-behind.
	/// </summary>
	public class FontWeightIndexConverter : IValueConverter
	{
		public static readonly FontWeight[] Weights =
		{
			FontWeights.Light,
			FontWeights.Normal,
			FontWeights.SemiBold,
			FontWeights.Bold,
			FontWeights.Black,
		};

		// Value (int) -> the name of the weight, to show it in the DetailText.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Weights.Length) ? Describe(Weights[i]) : $"(#{i})";
		}

		private static string Describe(FontWeight weight) => weight switch
		{
			_ when weight == FontWeights.Light => "Light",
			_ when weight == FontWeights.Normal => "Normal",
			_ when weight == FontWeights.SemiBold => "SemiBold",
			_ when weight == FontWeights.Bold => "Bold",
			_ when weight == FontWeights.Black => "Black",
			_ => weight.ToString(),
		};

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Maps the Master's <see cref="MouseInteractionBehavior"/> to an integer index and back
	/// (0 = <see cref="MouseInteractionBehavior.ChangeOnClick"/>, 1 =
	/// <see cref="MouseInteractionBehavior.MustFocusFirst"/>), so that this property can be
	/// driven with a numeric selector. A static list shared between XAML and code-behind.
	/// </summary>
	public class MouseBehaviorIndexConverter : IValueConverter
	{
		public static readonly MouseInteractionBehavior[] Modes =
		{
			MouseInteractionBehavior.ChangeOnClick,
			MouseInteractionBehavior.MustFocusFirst,
		};

		// Source (MouseInteractionBehavior) -> Target (int).
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> Array.IndexOf(Modes, value);

		// Target (int) -> Source (MouseInteractionBehavior).
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Modes.Length) ? Modes[i] : Modes[0];
		}
	}

	/// <summary>
	/// The name of the selected mode (int -> "ChangeOnClick"/"MustFocusFirst"), to show it in
	/// the DetailText of the <see cref="MouseInteractionBehavior"/> selector.
	/// </summary>
	public class MouseBehaviorTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < MouseBehaviorIndexConverter.Modes.Length)
				? MouseBehaviorIndexConverter.Modes[i].ToString()
				: $"(#{i})";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Maps the Master's <see cref="UserInteractionMode"/> to an integer index and back
	/// (0 = <see cref="UserInteractionMode.Interactive"/>, 1 =
	/// <see cref="UserInteractionMode.ReadOnly"/>), so that this property can be driven with a
	/// numeric selector. A static list shared between XAML and code-behind.
	/// </summary>
	public class InteractionModeIndexConverter : IValueConverter
	{
		public static readonly UserInteractionMode[] Modes =
		{
			UserInteractionMode.Interactive,
			UserInteractionMode.ReadOnly,
		};

		// Source (UserInteractionMode) -> Target (int).
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> Array.IndexOf(Modes, value);

		// Target (int) -> Source (UserInteractionMode).
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Modes.Length) ? Modes[i] : Modes[0];
		}
	}

	/// <summary>
	/// The name of the selected mode (int -> "Interactive"/"ReadOnly"), to show it in the
	/// DetailText of the <see cref="UserInteractionMode"/> selector.
	/// </summary>
	public class InteractionModeTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < InteractionModeIndexConverter.Modes.Length)
				? InteractionModeIndexConverter.Modes[i].ToString()
				: $"(#{i})";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Maps the Master's <see cref="ValueBoxDock"/> to an integer index and back
	/// (0 = <see cref="ValueBoxDock.Left"/>, 1 = <see cref="ValueBoxDock.Right"/>), so that
	/// this property can be driven with a numeric selector. The list follows the axis of the
	/// selector's bar (left→0, right→1), so that clicking on the left part selects
	/// <see cref="ValueBoxDock.Left"/>. The control's default is still
	/// <see cref="ValueBoxDock.Right"/> (that is set by the metadata of the DP, not by the
	/// order of this list). A static list shared between XAML and code-behind.
	/// </summary>
	public class ValueBoxDockIndexConverter : IValueConverter
	{
		public static readonly ValueBoxDock[] Modes =
		{
			ValueBoxDock.Left,
			ValueBoxDock.Right,
		};

		// Source (ValueBoxDock) -> Target (int).
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> Array.IndexOf(Modes, value);

		// Target (int) -> Source (ValueBoxDock).
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Modes.Length) ? Modes[i] : Modes[0];
		}
	}

	/// <summary>
	/// The name of the selected side (int -> "Left"/"Right"), to show it in the DetailText of
	/// the <see cref="ValueBoxDock"/> selector. The order is that of
	/// <see cref="ValueBoxDockIndexConverter.Modes"/>, which follows the axis of the bar
	/// (left→0), not that of the enum.
	/// </summary>
	public class ValueBoxDockTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < ValueBoxDockIndexConverter.Modes.Length)
				? ValueBoxDockIndexConverter.Modes[i].ToString()
				: $"(#{i})";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Maps a bool of the Master to an integer index and back (0 = false, 1 = true), so that a
	/// boolean property can be driven with a numeric selector. It is generic on purpose:
	/// <c>ShowDetail</c> and <c>ValueFollowsDetail</c> share it, because there is nothing in
	/// the conversion that belongs to either of them. What does belong to each —the text
	/// describing every state— lives in their own TextConverter.
	/// </summary>
	public class BoolIndexConverter : IValueConverter
	{
		public static readonly bool[] Values = { false, true };

		// Source (bool) -> Target (int).
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> (value is bool b && b) ? 1 : 0;

		// Target (int) -> Source (bool).
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Values.Length) ? Values[i] : Values[0];
		}
	}

	/// <summary>
	/// A description of the state (int -> "Keep Value with MainText"/"Value follows Detail"),
	/// to show it in the DetailText of the <see cref="ValueFollowsDetail"/> selector.
	/// </summary>
	public class ValueFollowsDetailTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return i switch
			{
				0 => "false: (Keep Value with MainText)",
				1 => "true: (Value Follows Detail)",
				_ => $"(#{i})",
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// A description of the state (int -> "Hide Detail Row"/"Show Detail Row"), to show it in
	/// the DetailText of the <see cref="ShowDetail"/> selector.
	/// </summary>
	public class ShowDetailTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return i switch
			{
				0 => "false: (Hide Detail Row)",
				1 => "true: (Show Detail Row)",
				_ => $"(#{i})",
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}
