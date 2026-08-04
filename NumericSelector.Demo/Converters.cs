using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

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
}
