using System.Windows;
using System.Windows.Input;

namespace NumericSelector.Demo
{
	/// <summary>
	/// The demo help (F1). It explains why the test bench is built out of the control itself
	/// and lists the mouse and keyboard gestures.
	/// </summary>
	/// <remarks>
	/// It opens NON-modal on purpose: half of what it explains are gestures, and they have to
	/// be reachable in the main window with the help in sight. That is what <see cref="Open"/>
	/// takes care of, and it also keeps copies from piling up.
	/// </remarks>
	public partial class HelpWindow : Window
	{
		private static HelpWindow? _instance;

		public HelpWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Shows the help for <paramref name="owner"/>. If it was already open it is brought to
		/// the front instead of opening another one.
		/// </summary>
		public static void Open(Window owner)
		{
			if (_instance != null)
			{
				if (_instance.WindowState == WindowState.Minimized)
					_instance.WindowState = WindowState.Normal;
				_instance.Activate();
				return;
			}

			_instance = new HelpWindow { Owner = owner };
			_instance.Closed += (_, _) => _instance = null;
			_instance.Show();
		}

		// F1 closes as well as opens, so that the same key works both ways. Esc is resolved by
		// the button with IsCancel, which needs no code.
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.F1)
			{
				Close();
				e.Handled = true;
				return;
			}
			base.OnKeyDown(e);
		}

		private void Close_Click(object sender, RoutedEventArgs e) => Close();
	}
}
