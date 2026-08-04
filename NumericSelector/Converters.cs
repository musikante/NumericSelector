using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NumericSelector
{
	/// <summary>
	/// Devuelve el mismo <see cref="Thickness"/> recibido pero con el lado inferior en cero.
	/// </summary>
	/// <remarks>
	/// La plantilla lo usa para la sección del título: sus bordes son los mismos que los de
	/// la sección de datos salvo el inferior, porque la línea que separa ambas secciones la
	/// dibuja el borde SUPERIOR de la sección de datos. Sin esto habría línea doble en la
	/// costura. No se puede resolver con un TemplateBinding pelado porque BorderThickness es
	/// un único Thickness de cuatro lados y hay que anular uno solo.
	/// </remarks>
	[ValueConversion(typeof(Thickness), typeof(Thickness))]
	public sealed class ThicknessWithoutBottomConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value is Thickness t ? new Thickness(t.Left, t.Top, t.Right, 0) : new Thickness(0);

		// La conversión pierde el lado inferior, así que no tiene inversa: el binding es de
		// una sola dirección y devolver DoNothing evita que un uso distraído escriba basura.
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> Binding.DoNothing;
	}
}
