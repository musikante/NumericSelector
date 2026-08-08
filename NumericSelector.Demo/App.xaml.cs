using System.Windows;

namespace NumericSelector.Demo
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		// Note: FrameworkElement.Language is NOT touched here. BoundedNumericSelector itself
		// adjusts its default to the system culture (see its static constructor), so number
		// formatting comes out right without the application doing anything.
	}
}
