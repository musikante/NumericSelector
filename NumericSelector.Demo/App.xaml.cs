using System.Windows;

namespace NumericSelector.Demo
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		// Nota: acá NO se toca FrameworkElement.Language. El propio BoundedNumericSelector ajusta
		// su default a la cultura del sistema (ver su constructor estático), así que el
		// formato de números sale correcto sin que la aplicación haga nada.
	}
}
