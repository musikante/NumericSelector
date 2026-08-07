using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NumericSelector;

namespace NumericSelector.Demo
{
	public partial class MainWindow : Window
	{
		private int _valueChangedCount;

		public MainWindow()
		{
			InitializeComponent();
			Loaded += MainWindow_Loaded;
		}
private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// El selector de fuente recorre la lista de fuentes del sistema; `Maximum` no es
			// constante para esta máquina, así que se fija en runtime una vez conocida la lista.
			FontPicker.Maximum = FontIndexConverter.Familias.Length - 1;

			// FontFamily no implementa igualdad por valor, así que no podemos comparar la
			// instancia del Master con las de la lista; la preseleccionamos por nombre.
			string actual = MasterNumericSelector.FontFamily.Source;
			int i = Array.FindIndex(FontIndexConverter.Familias, f => f.Source == actual);
			if (i >= 0) FontPicker.Value = i;
		}

		// Aplica la fuente elegida al Master (y se la da también al propio picker, para que
		// el cambio se vea en el control que lo produce).
		private void FontPicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
		{
			int i = FontPicker.Value;
			if (i < 0 || i >= FontIndexConverter.Familias.Length) return;

			var font = FontIndexConverter.Familias[i];
			MasterNumericSelector.FontFamily = font;
			FontPicker.FontFamily = font;
		}

		private void Master_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
		{
			// El evento ya se dispara mientras el XAML asigna Value="50", es decir antes de
			// que InitializeComponent() haya creado los elementos que vienen más abajo.
			// Sin este guard, EventLog todavía es null y la ventana revienta al abrirse.
			if (EventLog is null) return;

			_valueChangedCount++;
			EventLog.Text = $"#{_valueChangedCount}   {e.OldValue} -> {e.NewValue}";
		}

		// Aplica el color elegido a la propiedad del Master seleccionada con PropertyPicker
		// Y además pinta esa misma propiedad en el propio picker: así el cambio se ve en el
		// control que lo produce (no hace falta darle foco al Master para apreciar p. ej.
		// FocusBorderColor). La lista traduce el índice de PropertyIndexToNameConverter a la
		// DependencyProperty de color concreta (todas son del tipo Brush).
		private static readonly DependencyProperty[] PropiedadesDeColor =
		{
			BoundedNumericSelector.BarFillColorProperty,
			BoundedNumericSelector.BarBorderColorProperty,
			BoundedNumericSelector.ControlBorderColorProperty,
			BoundedNumericSelector.FocusBorderColorProperty,
			Control.ForegroundProperty,
			Control.BackgroundProperty,
		};

		private void ColorPicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
		{
			int propertyIndex = PropertyPicker.Value;

			// Seguridad: no explotar si el índice queda fuera de rango (p. ej. si en el
			// futuro PropertyPicker pudiera excederse).
			if (propertyIndex < 0 || propertyIndex >= PropiedadesDeColor.Length)
				return;

			var target = PropiedadesDeColor[propertyIndex];
			Color color = ColorIndexConverter.Colores[ColorPicker.Value];
			var brush = new SolidColorBrush(color);

			// El cambio en el Master (el sujeto de prueba) y en el propio selector de color.
			MasterNumericSelector.SetValue(target, brush);
			((BoundedNumericSelector)sender).SetValue(target, brush);
		}
	}
}
