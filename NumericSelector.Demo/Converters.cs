using System;
using System.Globalization;
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
}
