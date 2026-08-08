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

			// FontStyle y FontWeight son structs con igualdad por valor: comparación directa.
			Preseleccionar(FontStylePicker, FontStyleIndexConverter.Estilos, MasterNumericSelector.FontStyle);
			Preseleccionar(FontWeightPicker, FontWeightIndexConverter.Pesos, MasterNumericSelector.FontWeight);
		}

		private static void Preseleccionar<T>(BoundedNumericSelector selector, T[] opciones, T actual)
			where T : struct
		{
			int i = Array.IndexOf(opciones, actual);
			if (i >= 0) selector.Value = i;
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

		private void FontStylePicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
		{
			int i = FontStylePicker.Value;
			if (i < 0 || i >= FontStyleIndexConverter.Estilos.Length) return;

			var style = FontStyleIndexConverter.Estilos[i];
			MasterNumericSelector.FontStyle = style;
			FontStylePicker.FontStyle = style;
		}

		private void FontWeightPicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
		{
			int i = FontWeightPicker.Value;
			if (i < 0 || i >= FontWeightIndexConverter.Pesos.Length) return;

			var weight = FontWeightIndexConverter.Pesos[i];
			MasterNumericSelector.FontWeight = weight;
			FontWeightPicker.FontWeight = weight;
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

		// Aplica el color elegido por un picker a su propiedad sobre el Master (el sujeto
		// de prueba) y sobre el propio picker, para que el cambio se vea en el control que
		// lo produce (no hace falta darle foco al Master para apreciar p. ej.
		// FocusBorderBrush). Cada picker está enganchado a una sola propiedad, la misma que
		// anuncia en su CaptionText.
		private void AplicarColor(BoundedNumericSelector picker, DependencyProperty property)
		{
			int i = picker.Value;
			if (i < 0 || i >= ColorIndexConverter.Colores.Length) return;

			var brush = new SolidColorBrush(ColorIndexConverter.Colores[i]);
			MasterNumericSelector.SetValue(property, brush);
			picker.SetValue(property, brush);
		}

		private void BarFill_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> AplicarColor((BoundedNumericSelector)sender, BoundedNumericSelector.BarFillProperty);

		private void BarDividerBrush_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> AplicarColor((BoundedNumericSelector)sender, BoundedNumericSelector.BarDividerBrushProperty);

		private void BorderBrush_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> AplicarColor((BoundedNumericSelector)sender, Control.BorderBrushProperty);

		private void FocusBorderBrush_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> AplicarColor((BoundedNumericSelector)sender, BoundedNumericSelector.FocusBorderBrushProperty);

		private void Foreground_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> AplicarColor((BoundedNumericSelector)sender, Control.ForegroundProperty);

		private void Background_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> AplicarColor((BoundedNumericSelector)sender, Control.BackgroundProperty);
	}
}
