using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NumericSelector.Demo
{
	/// <summary>
	/// Convierte un Thickness uniforme a int y viceversa, para poder manejar
	/// ControlBorderPixels con un BoundedNumericSelector (que trabaja con enteros).
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
	/// Casar la selección de un ComboBox por el valor del <see cref="Color"/> y no por la
	/// referencia del brush. Los brushes de la paleta (StaticResource) son instancias
	/// distintas de las que tiene el control; por referencia un SelectedItem nunca casaría
	/// y la caja quedaría vacía.
	/// </summary>
	public class BrushToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value is SolidColorBrush b ? b.Color : Colors.Transparent;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> new SolidColorBrush(value is Color c ? c : Colors.Transparent);
	}

	/// <summary>
	/// Traduce el Value de un BoundedNumericSelector (0..5) al nombre de la propiedad
	/// del Master que ese índice representa. Es la asignación que la próxima utilidad de
	/// color consumirá; por ahora solo da nombre al selector para verlo funcionar.
	/// </summary>
	public class PropertyIndexToNameConverter : IValueConverter
	{
		// Mismo orden que el de los combos de color de abajo.
		private static readonly string[] Propiedades =
		{
			"BarFillColor",
			"BarBorderColor",
			"ControlBorderColor",
			"FocusBorderColor",
			"Foreground",
			"Background",
		};

		// Valor (int) -> nombre de propiedad. Fuera de rango devuelve un texto genérico.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int i = (value is int n) ? n : -1;
			return (i >= 0 && i < Propiedades.Length) ? Propiedades[i] : $"(índice {i} sin propiedad)";
		}

		// No se usa: el selector no recibe el nombre de vuelta.
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Mapea el Value de un BoundedNumericSelector (0..23) a un color de la paleta del demo.
	/// Devuelve el color como "#FFRRGGBB" (para el título del selector) pero también expone
	/// la lista, en el mismo orden, para que el code-behind la use al pintar el Master.
	/// El orden coincide con el de x:Array "Paleta" de MainWindow.xaml.
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

		// Valor (int) -> "#FFRRGGBB" para mostrarlo en el título.
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
}
