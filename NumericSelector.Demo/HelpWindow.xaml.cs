using System.Windows;
using System.Windows.Input;

namespace NumericSelector.Demo
{
	/// <summary>
	/// Ayuda del demo (F1). Explica por qué el banco de pruebas está hecho con el propio
	/// control y lista los gestos de mouse y teclado.
	/// </summary>
	/// <remarks>
	/// Se abre SIN modal a propósito: la mitad de lo que explica son gestos, y hay que poder
	/// probarlos en la ventana principal con la ayuda a la vista. De eso se encarga
	/// <see cref="Mostrar"/>, que además evita que se acumulen copias.
	/// </remarks>
	public partial class HelpWindow : Window
	{
		private static HelpWindow? _abierta;

		public HelpWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Muestra la ayuda de <paramref name="owner"/>. Si ya estaba abierta la trae al
		/// frente en vez de abrir otra.
		/// </summary>
		public static void Mostrar(Window owner)
		{
			if (_abierta != null)
			{
				if (_abierta.WindowState == WindowState.Minimized)
					_abierta.WindowState = WindowState.Normal;
				_abierta.Activate();
				return;
			}

			_abierta = new HelpWindow { Owner = owner };
			_abierta.Closed += (_, _) => _abierta = null;
			_abierta.Show();
		}

		// F1 cierra además de abrir, para que la misma tecla sirva de ida y vuelta. El Esc lo
		// resuelve el botón con IsCancel, que no necesita código.
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

		private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
	}
}
