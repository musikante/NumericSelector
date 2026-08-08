using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NumericSelector.Demo
{
	/// <summary>
	/// Convierte un Thickness uniforme a int y viceversa, para poder manejar
	/// BorderWidth con un BoundedNumericSelector (que trabaja con enteros).
	/// </summary>
	public class ThicknessToIntConverter : IValueConverter
	{
		// Origen (Thickness) -> destino (int) : tomamos el lado izquierdo como representante.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value is Thickness t ? (int)Math.Round(t.Left) : 0;

		// Destino (int) -> origen (Thickness) : borde uniforme.
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> new Thickness(System.Convert.ToDouble(value, culture));
	}

	/// <summary>
	/// Mapea el Value de un BoundedNumericSelector (0..23) a un color de la paleta del demo.
	/// Devuelve el color como "#FFRRGGBB" (para el DetailText del selector) pero también expone
	/// la lista, en el mismo orden, para que el code-behind la use al pintar el Master.
	/// Es la única fuente de la paleta: el XAML ya no declara una copia en paralelo.
	/// </summary>
	public class ColorIndexConverter : IValueConverter
	{
		public static readonly Color[] Colores =
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

		// Valor (int) -> "#FFRRGGBB" para mostrarlo en el DetailText.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			if (i < 0 || i >= Colores.Length)
				return $"(#{i})";
			Color c = Colores[i];
			return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
		}

		// No se usa: el selector no recibe el color de vuelta.
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Mapea el Value de un BoundedNumericSelector (0..FontCount-1) a una fuente del sistema.
	/// La lista sale de <see cref="Fonts.SystemFontFamilies"/> ordenada por nombre, y se
	/// expone estáticamente para que XAML y code-behind usen exactamente la misma colección.
	/// </summary>
	public class FontIndexConverter : IValueConverter
	{
		public static readonly FontFamily[] Familias =
			Fonts.SystemFontFamilies
				.OrderBy(f => f.Source, StringComparer.CurrentCultureIgnoreCase)
				.ToArray();

		// Valor (int) -> nombre de la fuente, para mostrarlo en el DetailText.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Familias.Length) ? Familias[i].Source : $"(#{i})";
		}

		// No se usa: el selector no recibe la fuente de vuelta (el índice habla por él).
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Mapea el Value de un BoundedNumericSelector (0..2) a un FontStyle. Igual patrón que
	/// <see cref="FontIndexConverter"/>: lista estática compartida entre XAML y code-behind.
	/// </summary>
	public class FontStyleIndexConverter : IValueConverter
	{
		public static readonly FontStyle[] Estilos =
		{
			FontStyles.Normal,
			FontStyles.Italic,
			FontStyles.Oblique,
		};

		// Valor (int) -> nombre del estilo, para mostrarlo en el DetailText.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Estilos.Length) ? Nombre(Estilos[i]) : $"(#{i})";
		}

		private static string Nombre(FontStyle style) => style switch
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
	/// Mapea el Value de un BoundedNumericSelector (0..4) a un FontWeight. Igual patrón que
	/// <see cref="FontIndexConverter"/>: lista estática compartida entre XAML y code-behind.
	/// </summary>
	public class FontWeightIndexConverter : IValueConverter
	{
		public static readonly FontWeight[] Pesos =
		{
			FontWeights.Light,
			FontWeights.Normal,
			FontWeights.SemiBold,
			FontWeights.Bold,
			FontWeights.Black,
		};

		// Valor (int) -> nombre del peso, para mostrarlo en el DetailText.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Pesos.Length) ? Nombre(Pesos[i]) : $"(#{i})";
		}

		private static string Nombre(FontWeight weight) => weight switch
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
	/// Mapea el <see cref="MouseInteractionBehavior"/> del Master a un índice entero y de vuelta
	/// (0 = <see cref="MouseInteractionBehavior.ChangeOnClick"/>, 1 =
	/// <see cref="MouseInteractionBehavior.MustFocusFirst"/>), para poder manejar esta propiedad
	/// con un selector numérico. Lista estática compartida entre XAML y code-behind.
	/// </summary>
	public class MouseBehaviorIndexConverter : IValueConverter
	{
		public static readonly MouseInteractionBehavior[] Modos =
		{
			MouseInteractionBehavior.ChangeOnClick,
			MouseInteractionBehavior.MustFocusFirst,
		};

		// Source (MouseInteractionBehavior) -> Target (int).
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> Array.IndexOf(Modos, value);

		// Target (int) -> Source (MouseInteractionBehavior).
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Modos.Length) ? Modos[i] : Modos[0];
		}
	}

	/// <summary>
	/// Nombre del modo seleccionado (int -> "ChangeOnClick"/"MustFocusFirst"), para mostrarlo
	/// en el DetailText del selector de <see cref="MouseInteractionBehavior"/>.
	/// </summary>
	public class MouseBehaviorTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < MouseBehaviorIndexConverter.Modos.Length)
				? MouseBehaviorIndexConverter.Modos[i].ToString()
				: $"(#{i})";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Mapea el <see cref="UserInteractionMode"/> del Master a un índice entero y de vuelta
	/// (0 = <see cref="UserInteractionMode.Interactive"/>, 1 =
	/// <see cref="UserInteractionMode.ReadOnly"/>), para poder manejar esta propiedad con un
	/// selector numérico. Lista estática compartida entre XAML y code-behind.
	/// </summary>
	public class InteractionModeIndexConverter : IValueConverter
	{
		public static readonly UserInteractionMode[] Modos =
		{
			UserInteractionMode.Interactive,
			UserInteractionMode.ReadOnly,
		};

		// Source (UserInteractionMode) -> Target (int).
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> Array.IndexOf(Modos, value);

		// Target (int) -> Source (UserInteractionMode).
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Modos.Length) ? Modos[i] : Modos[0];
		}
	}

	/// <summary>
	/// Nombre del modo seleccionado (int -> "Interactive"/"ReadOnly"), para mostrarlo en el
	/// DetailText del selector de <see cref="UserInteractionMode"/>.
	/// </summary>
	public class InteractionModeTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < InteractionModeIndexConverter.Modos.Length)
				? InteractionModeIndexConverter.Modos[i].ToString()
				: $"(#{i})";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Mapea el <see cref="ValueBoxDock"/> del Master a un índice entero y de vuelta
	/// (0 = <see cref="ValueBoxDock.Left"/>, 1 = <see cref="ValueBoxDock.Right"/>), para
	/// poder manejar esta propiedad con un selector numérico. La lista sigue el eje de
	/// la barra del selector (izquierda→0, derecha→1), de modo que el clic en la parte
	/// izquierda seleccione <see cref="ValueBoxDock.Left"/>. El default del control
	/// sigue siendo <see cref="ValueBoxDock.Right"/> (lo fija la metadata de la DP, no
	/// el orden de esta lista). Lista estática compartida entre XAML y code-behind.
	/// </summary>
	public class ValueBoxDockIndexConverter : IValueConverter
	{
		public static readonly ValueBoxDock[] Modos =
		{
			ValueBoxDock.Left,
			ValueBoxDock.Right,
		};

		// Source (ValueBoxDock) -> Target (int).
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> Array.IndexOf(Modos, value);

		// Target (int) -> Source (ValueBoxDock).
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Modos.Length) ? Modos[i] : Modos[0];
		}
	}

	/// <summary>
	/// Nombre del lado seleccionado (int -> "Right"/"Left"), para mostrarlo en el
	/// DetailText del selector de <see cref="ValueBoxDock"/>.
	/// </summary>
	public class ValueBoxDockTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < ValueBoxDockIndexConverter.Modos.Length)
				? ValueBoxDockIndexConverter.Modos[i].ToString()
				: $"(#{i})";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Mapea <see cref="ValueFollowsDetail"/> del Master (bool) a un índice entero y de
	/// vuelta (0 = false, 1 = true), para poder manejar esta propiedad con un selector
	/// numérico.
	/// </summary>
	public class ValueFollowsDetailIndexConverter : IValueConverter
	{
		public static readonly bool[] Valores = { false, true };

		// Source (bool) -> Target (int).
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> (value is bool b && b) ? 1 : 0;

		// Target (int) -> Source (bool).
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Valores.Length) ? Valores[i] : Valores[0];
		}
	}

	/// <summary>
	/// Descripción del estado (int -> "Keep Value with Caption"/"Value follows Detail"),
	/// para mostrarlo en el DetailText del selector de <see cref="ValueFollowsDetail"/>.
	/// </summary>
	public class ValueFollowsDetailTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return i switch
			{
				0 => "false: (Keep Value with Caption)",
				1 => "true: (Value Follows Detail)",
				_ => $"(#{i})",
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Mapea <see cref="ShowDetail"/> del Master (bool) a un índice entero y de vuelta
	/// (0 = false, 1 = true), para poder manejar esta propiedad con un selector numérico.
	/// </summary>
	public class ShowDetailIndexConverter : IValueConverter
	{
		public static readonly bool[] Valores = { false, true };

		// Source (bool) -> Target (int).
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> (value is bool b && b) ? 1 : 0;

		// Target (int) -> Source (bool).
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Valores.Length) ? Valores[i] : Valores[0];
		}
	}

	/// <summary>
	/// Descripción del estado (int -> "Hide Detail Row"/"Show Detail Row"), para mostrarlo
	/// en el DetailText del selector de <see cref="ShowDetail"/>.
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
