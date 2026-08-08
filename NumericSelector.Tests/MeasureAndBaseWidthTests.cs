using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelector.Tests;

[TestClass]
public class MeasureAndBaseWidthTests
{
	// Registra el layout completo una vez para que el Template del control esté aplicado
	// (sin plantilla, MeasureOverride de un Control mide 0 y las pruebas serían vacuas).
	private static void ShowWindow(UIElement content)
	{
		var window = new Window { Width = 300, Height = 200, Content = content, ShowInTaskbar = false };
		window.Show();
		Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
		window.UpdateLayout();
	}

	// El control no debe reportar más ancho que el que le ofrece el contenedor: el ancho
	// natural de la leyenda puede ser enorme, pero si sólo hay 120 píxeles el control se
	// mide a 120 (y la CharacterEllipsis de la plantilla trunca la leyenda, en vez de
	// desbordar).
	[TestMethod]
	public void DesiredWidth_never_exceeds_constrained_width()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector
			{
				Minimum = 0, Maximum = 100000, Value = 50000,
				ShowDetail = true,
				CaptionText = "Una leyenda muy larga que de ninguna manera cabe en un contenedor de ciento veinte píxeles de ancho",
				DetailText = "Otro detalle bastante extenso para asegurar que el contenido exceda por mucho el espacio disponible",
			};

			ShowWindow(selector);

			// Medición directa con un ancho fijo de 120: lo que reporta el DesiredSize no
			// debe superarlo, es decir nunca pide más de lo disponible (no desborda).
			selector.Measure(new Size(120, double.PositiveInfinity));

			Assert.IsTrue(selector.DesiredSize.Width <= 120,
				$"DesiredWidth {selector.DesiredSize.Width} supera el hueco de 120");
		});
	}

	// BaseWidth es un piso: con mucho espacio el control mantiene al menos ese ancho,
	// aunque el contenido natural sea más angosto. Con espacio infinito no se recorta nada.
	[TestMethod]
	public void BaseWidth_acts_as_floor_when_there_is_room()
	{
		StaTest.Run(() =>
		{
var selector = new BoundedNumericSelector
			{
				BaseWidth = 300,
				Minimum = 0, Maximum = 100, Value = 50,
				CaptionText = "Corto",
			};

			ShowWindow(selector);

			// Medida con ancho infinito (p. ej. dentro de un StackPanel ancho).
			selector.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

			Assert.IsTrue(selector.DesiredSize.Width >= 300,
				$"DesiredWidth {selector.DesiredSize.Width} quedó por debajo del piso BaseWidth 300");
		});
	}
}
