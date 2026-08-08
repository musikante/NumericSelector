using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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
			FontPicker.Maximum = FontIndexConverter.Filtered.Length - 1;

			// FontFamily no implementa igualdad por valor, así que no podemos comparar la
			// instancia del Master con las de la lista; la preseleccionamos por nombre.
			string current = MasterNumericSelector.FontFamily.Source;
			int i = Array.FindIndex(FontIndexConverter.Filtered, f => f.Source == current);
			if (i >= 0) FontPicker.Value = i;

			// FontStyle y FontWeight son structs con igualdad por valor: comparación directa.
			Preselect(FontStylePicker, FontStyleIndexConverter.Styles, MasterNumericSelector.FontStyle);
			Preselect(FontWeightPicker, FontWeightIndexConverter.Weights, MasterNumericSelector.FontWeight);
		}

		// F1 (o el botón del pie) abre la ayuda del demo.
		private void Help_Executed(object sender, ExecutedRoutedEventArgs e) => HelpWindow.Open(this);

		private static void Preselect<T>(BoundedNumericSelector selector, T[] options, T current)
			where T : struct
		{
			int i = Array.IndexOf(options, current);
			if (i >= 0) selector.Value = i;
		}

		// Aplica la fuente elegida al Master (y se la da también al propio picker, para que
		// el cambio se vea en el control que lo produce).
		private void FontPicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyFont();

		private void ApplyFont()
		{
			int i = FontPicker.Value;
			// Con la lista vacía —filtro sin coincidencias— no hay nada que aplicar y se
			// conserva la última fuente puesta.
			if (i < 0 || i >= FontIndexConverter.Filtered.Length) return;

			var font = FontIndexConverter.Filtered[i];
			MasterNumericSelector.FontFamily = font;
			FontPicker.FontFamily = font;
		}

		// Filtra la lista de fuentes que recorre FontPicker. Se aplica en cada tecla.
		private void FontFilter_TextChanged(object sender, TextChangedEventArgs e)
		{
			// Qué fuente estaba elegida, para reencontrarla si sobrevive al filtro. Se guarda
			// el nombre y no la instancia porque FontFamily no tiene igualdad por valor.
			int previousIndex = FontPicker.Value;
			string? chosen = (previousIndex >= 0 && previousIndex < FontIndexConverter.Filtered.Length)
				? FontIndexConverter.Filtered[previousIndex].Source
				: null;

			int matchCount = FontIndexConverter.Filter(FontFilterBox.Text);

			// El rango del control tiene siempre al menos 1 de ancho: con 0 o 1 coincidencias
			// esto pide 0 y la coerción lo sube a 1. Queda a la vista en el DetailText, que
			// para ese índice sobrante avisa que no hay fuente. Es el límite real de la API.
			FontPicker.Maximum = Math.Max(matchCount - 1, 0);

			int i = chosen is null
				? -1
				: Array.FindIndex(FontIndexConverter.Filtered, f => f.Source == chosen);
			FontPicker.Value = i >= 0 ? i : 0;

			// El DetailText se enlaza a Value, así que sólo se recalcula cuando Value cambia.
			// Al filtrar puede cambiar la LISTA sin cambiar el índice, y entonces el nombre
			// mostrado sería el de la fuente anterior: hay que pedir el refresco a mano.
			BindingOperations
				.GetBindingExpression(FontPicker, BoundedNumericSelector.DetailTextProperty)
				?.UpdateTarget();

			// Y por el mismo motivo hay que reaplicar: si el índice no cambió pero la lista sí,
			// ValueChanged no se dispara y la fuente quedaría desfasada del nombre que se lee.
			ApplyFont();
		}

		private void FontStylePicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
		{
			int i = FontStylePicker.Value;
			if (i < 0 || i >= FontStyleIndexConverter.Styles.Length) return;

			var style = FontStyleIndexConverter.Styles[i];
			MasterNumericSelector.FontStyle = style;
			FontStylePicker.FontStyle = style;
		}

		private void FontWeightPicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
		{
			int i = FontWeightPicker.Value;
			if (i < 0 || i >= FontWeightIndexConverter.Weights.Length) return;

			var weight = FontWeightIndexConverter.Weights[i];
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
		// anuncia en su MainText.
		private void ApplyColor(BoundedNumericSelector picker, DependencyProperty property)
		{
			int i = picker.Value;
			if (i < 0 || i >= ColorIndexConverter.Palette.Length) return;

			var brush = new SolidColorBrush(ColorIndexConverter.Palette[i]);
			MasterNumericSelector.SetValue(property, brush);
			picker.SetValue(property, brush);
		}

		private void BarFill_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, BoundedNumericSelector.BarFillProperty);

		private void BarDividerBrush_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, BoundedNumericSelector.BarDividerBrushProperty);

		private void BorderBrush_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, Control.BorderBrushProperty);

		private void FocusBorderBrush_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, BoundedNumericSelector.FocusBorderBrushProperty);

		private void Foreground_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, Control.ForegroundProperty);

		private void Background_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, Control.BackgroundProperty);
	}
}
