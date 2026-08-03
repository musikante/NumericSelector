using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NumericSelectorLib_Test
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
			// FontFamily no implementa igualdad por valor, así que el ComboBox no puede
			// preseleccionar la fuente actual del Master por sí solo: la buscamos por nombre.
			string actual = MasterNumericSelector.FontFamily.Source;
			FontFamilyPicker.SelectedItem = FontFamilyPicker.Items
				.OfType<FontFamily>()
				.FirstOrDefault(f => f.Source == actual);
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
	}
}
