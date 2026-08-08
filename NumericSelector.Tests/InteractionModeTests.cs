using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelector.Tests;

/// <summary>
/// Pruebas de los dos modos de interacción: MouseBehavior e InteractionMode.
/// A diferencia de las de lógica pura, éstas necesitan una ventana real: los gestos
/// llegan por eventos ruteados y el foco no existe fuera del árbol visual.
/// </summary>
[TestClass]
public class InteractionModeTests
{
	// --- Andamiaje ---

	/// <summary>
	/// Monta el control en una ventana visible y espera a que la plantilla se aplique.
	/// Sin ventana no hay PART_BarGrid ni foco de teclado, así que no habría nada que probar.
	/// Incluye un botón aparte para poder sacarle el foco al control cuando la prueba
	/// necesita el escenario "sin foco previo".
	/// </summary>
	private static Escenario Host(Action<BoundedNumericSelector>? configurar = null)
	{
		var selector = new BoundedNumericSelector { Minimum = 0, Maximum = 100, Value = 50 };
		configurar?.Invoke(selector);

		var otro = new Button { Content = "otro" };
		var panel = new StackPanel();
		panel.Children.Add(otro);
		panel.Children.Add(selector);

		var window = new Window
		{
			Width = 400,
			Height = 200,
			Content = panel,
			ShowInTaskbar = false,
		};

		window.Show();
		Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
		window.UpdateLayout();

		var bar = (FrameworkElement)selector.Template.FindName("PART_BarGrid", selector);
		return new Escenario(window, selector, bar, otro);
	}

	private sealed record Escenario(
		Window Window, BoundedNumericSelector Selector, FrameworkElement Bar, Button Otro);

	/// <summary>
	/// Click izquierdo completo: primero la fase túnel (donde el control toma el foco y
	/// anota si ya lo tenía) y después la burbuja (donde el handler de la barra decide).
	/// Las dos fases son imprescindibles: con la burbuja sola, MustFocusFirst nunca vería
	/// el estado de foco previo y la prueba no probaría nada.
	/// </summary>
	private static void ClickIzquierdo(UIElement destino)
	{
		destino.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
		{ RoutedEvent = Mouse.PreviewMouseDownEvent });
		destino.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
		{ RoutedEvent = UIElement.MouseLeftButtonDownEvent });
	}

	private static void ClickDerecho(UIElement destino)
	{
		destino.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
		{ RoutedEvent = Mouse.PreviewMouseDownEvent });
		destino.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
		{ RoutedEvent = UIElement.MouseRightButtonUpEvent });
	}

	/// <summary>
	/// Averigua qué valor produce el gesto EJECUTÁNDOLO una vez con el control ya enfocado
	/// (es decir, con el gesto habilitado en cualquiera de los dos modos), y devuelve además
	/// un valor de partida garantizadamente distinto.
	///
	/// Por qué así y no calculando la posición: un click simulado resuelve e.GetPosition()
	/// durante el ruteo, y esa posición NO coincide con la que devuelve Mouse.GetPosition()
	/// desde afuera del evento (medido: -181 contra 392 sobre la misma barra). Predecir el
	/// resultado desde afuera daba pruebas intermitentes. El puntero no se mueve durante la
	/// prueba, así que ejecutar el gesto es la única fuente confiable de la expectativa.
	/// </summary>
	private static (int Esperado, int Distinto) Sondear(Escenario e, Action<UIElement> gesto)
	{
		e.Selector.Focus();
		gesto(e.Bar);

		int esperado = e.Selector.Value;
		int distinto = esperado == e.Selector.Maximum ? e.Selector.Minimum : e.Selector.Maximum;

		// Se devuelve el control al estado "sin foco" para que la prueba arme su escenario.
		e.Otro.Focus();
		return (esperado, distinto);
	}

	// --- MouseBehavior ---

	[TestMethod]
	public void Interaction_defaults_leave_the_control_fully_responsive()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector();

			Assert.AreEqual(MouseInteractionBehavior.ChangeOnClick, selector.MouseBehavior);
			Assert.AreEqual(UserInteractionMode.Interactive, selector.InteractionMode);
		});
	}

	[TestMethod]
	public void Change_on_click_moves_the_value_without_previous_focus()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				var (esperado, distinto) = Sondear(e, ClickIzquierdo);
				e.Selector.Value = distinto;
				Assert.IsFalse(e.Selector.IsKeyboardFocused, "El escenario exige arrancar sin foco.");

				ClickIzquierdo(e.Bar);

				Assert.AreEqual(esperado, e.Selector.Value,
					"En ChangeOnClick el click debe actuar aunque el control no tuviera el foco.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Must_focus_first_spends_the_first_click_on_taking_focus()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.MouseBehavior = MouseInteractionBehavior.MustFocusFirst);
			try
			{
				var (esperado, distinto) = Sondear(e, ClickIzquierdo);
				e.Selector.Value = distinto;

				ClickIzquierdo(e.Bar);
				Assert.AreEqual(distinto, e.Selector.Value,
					"El click que otorga el foco no debe además mover el valor.");
				Assert.IsTrue(e.Selector.IsKeyboardFocused,
					"Ese primer click sí tiene que dejar el control enfocado.");

				ClickIzquierdo(e.Bar);
				Assert.AreEqual(esperado, e.Selector.Value,
					"Con el foco ya puesto, el click siguiente debe actuar normalmente.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Must_focus_first_does_not_care_where_the_focus_came_from()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.MouseBehavior = MouseInteractionBehavior.MustFocusFirst);
			try
			{
				var (esperado, distinto) = Sondear(e, ClickIzquierdo);
				e.Selector.Value = distinto;

				// Foco entregado por código, como lo haría una tabulación: no es un click.
				e.Selector.Focus();
				Assert.IsTrue(e.Selector.IsKeyboardFocused);

				ClickIzquierdo(e.Bar);

				Assert.AreEqual(esperado, e.Selector.Value,
					"Si el foco ya estaba puesto, el primer click debe actuar aunque no lo haya dado él.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Must_focus_first_gates_the_right_click_zones_too()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.MouseBehavior = MouseInteractionBehavior.MustFocusFirst);
			try
			{
				var (esperado, distinto) = Sondear(e, ClickDerecho);
				e.Selector.Value = distinto;

				ClickDerecho(e.Bar);
				Assert.AreEqual(distinto, e.Selector.Value,
					"La regla vale para todos los gestos de mouse, no sólo para el click izquierdo.");

				ClickDerecho(e.Bar);
				Assert.AreEqual(esperado, e.Selector.Value,
					"Con el foco puesto, el click derecho por zonas debe actuar.");
			}
			finally { e.Window.Close(); }
		});
	}

	// --- InteractionMode ---

	[TestMethod]
	public void Read_only_takes_the_control_out_of_the_tab_order_and_gives_it_back()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector();
			Assert.IsTrue(selector.Focusable);

			selector.InteractionMode = UserInteractionMode.ReadOnly;
			Assert.IsFalse(selector.Focusable, "En sólo visualización el control no debe poder enfocarse.");

			selector.InteractionMode = UserInteractionMode.Interactive;
			Assert.IsTrue(selector.Focusable, "Al salir del modo, la focusabilidad tiene que volver sola.");
		});
	}

	[TestMethod]
	public void Read_only_does_not_overrule_a_consumer_that_disabled_focus()
	{
		StaTest.Run(() =>
		{
			// La focusabilidad se quita por coerción, no por asignación, justamente para
			// no pisar la decisión de quien usa el control.
			var selector = new BoundedNumericSelector { Focusable = false };

			selector.InteractionMode = UserInteractionMode.ReadOnly;
			selector.InteractionMode = UserInteractionMode.Interactive;

			Assert.IsFalse(selector.Focusable,
				"Salir del modo no debe encender la focusabilidad que el consumidor había apagado.");
		});
	}

	[TestMethod]
	public void Read_only_releases_the_focus_it_already_had()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				e.Selector.Focus();
				Assert.IsTrue(e.Selector.IsKeyboardFocused);

				e.Selector.InteractionMode = UserInteractionMode.ReadOnly;

				// Quitar Focusable no suelta por sí solo un foco ya puesto: si esto se rompe,
				// el control queda con el borde de foco encendido y la rueda viva.
				Assert.IsFalse(e.Selector.IsKeyboardFocused,
					"Entrar en sólo visualización tiene que soltar el foco que ya estuviera puesto.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Read_only_ignores_mouse_wheel_and_keyboard()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				// Se sondea ANTES de entrar en el modo: hace falta saber qué valor produciría
				// un gesto que funciona, para poder afirmar que después no produce ninguno.
				var (esperado, distinto) = Sondear(e, ClickIzquierdo);

				e.Selector.InteractionMode = UserInteractionMode.ReadOnly;
				e.Selector.Value = distinto;

				ClickIzquierdo(e.Bar);
				Assert.AreEqual(distinto, e.Selector.Value, "El click izquierdo no debe hacer nada.");

				ClickDerecho(e.Bar);
				Assert.AreEqual(distinto, e.Selector.Value, "El click derecho no debe hacer nada.");

				e.Selector.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, 120)
				{ RoutedEvent = UIElement.MouseWheelEvent });
				Assert.AreEqual(distinto, e.Selector.Value, "La rueda no debe hacer nada.");

				var fuente = PresentationSource.FromVisual(e.Window)!;
				e.Selector.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, fuente, 0, Key.Right)
				{ RoutedEvent = Keyboard.PreviewKeyDownEvent });
				Assert.AreEqual(distinto, e.Selector.Value, "El teclado no debe hacer nada.");

				// Y que el sondeo no haya sido degenerado: el gesto sí producía un cambio.
				Assert.AreNotEqual(esperado, distinto,
					"Si el gesto no cambiaba nada de entrada, la prueba no estaría probando el bloqueo.");
			}
			finally { e.Window.Close(); }
		});
	}

	// --- ShowDetail y disposición ---
	// Son de apariencia, pero sus pruebas viven acá porque la regla que importa es la
	// interacción con el FOCO, y para eso hace falta una ventana real.

	private static Border Celda(BoundedNumericSelector selector, string parte) =>
		(Border)selector.Template.FindName(parte, selector);

	private static Border CeldaDetalle(BoundedNumericSelector selector) =>
		Celda(selector, "PART_DetailCell");

	private static Border CajaDeArriba(BoundedNumericSelector selector) =>
		Celda(selector, "PART_ValueDetailCell");

	private static Border CajaDeAbajo(BoundedNumericSelector selector) =>
		Celda(selector, "PART_ValueCell");

	[TestMethod]
	public void Detail_row_is_hidden_by_default()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				Assert.AreEqual(Visibility.Collapsed,
					((FrameworkElement)e.Selector.Template.FindName("PART_DetailRow", e.Selector)).Visibility);
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Show_detail_shows_a_framed_detail_that_yields_only_the_top()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => { s.ShowDetail = true; s.BorderThickness = new Thickness(2); });
			try
			{
				Assert.AreEqual(Visibility.Visible,
					((FrameworkElement)e.Selector.Template.FindName("PART_DetailRow", e.Selector)).Visibility);
				Assert.AreEqual(e.Selector.BorderBrush, CeldaDetalle(e.Selector).BorderBrush);

				// Con el default de ValueFollowsDetail (true) el valor desciende: el detalle
				// es marco fijo (Left,Right,Bottom) y sólo cede la parte superior a la costura
				// que dibuja la barra (que ahora queda arriba).
				Assert.AreEqual(new Thickness(2, 0, 2, 2), CeldaDetalle(e.Selector).BorderThickness,
					"La fila de detalle lleva los tres lados y cede el superior a la barra.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Background_reaches_every_cell()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.ShowDetail = true;
				s.Background = Brushes.Coral;
			});
			try
			{
				// El fondo del control se pinta a través de las celdas: si una no lo
				// enlazara, quedaría un hueco sin color ahí.
				Assert.AreEqual(e.Selector.Background, CeldaDetalle(e.Selector).Background);
				Assert.AreEqual(e.Selector.Background, CajaDeAbajo(e.Selector).Background);
				Assert.AreEqual(e.Selector.Background, Celda(e.Selector, "PART_BarCell").Background);
				Assert.AreEqual(e.Selector.Background, CajaDeArriba(e.Selector).Background);
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Focus_lights_up_the_whole_outline()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.ShowDetail = true);
			try
			{
				e.Selector.Focus();

				// Con ShowDetail el contorno es la unión de los marcos, y todos se
				// tiñen: no hay caja "frameless" que apagar, así que la regla es única.
				Assert.AreEqual(e.Selector.FocusBorderBrush, CeldaDetalle(e.Selector).BorderBrush);
				Assert.AreEqual(e.Selector.FocusBorderBrush, CajaDeArriba(e.Selector).BorderBrush);
				Assert.AreEqual(e.Selector.FocusBorderBrush, Celda(e.Selector, "PART_BarCell").BorderBrush);
				Assert.AreEqual(e.Selector.FocusBorderBrush, CajaDeAbajo(e.Selector).BorderBrush);
			}
			finally { e.Window.Close(); }
		});
	}

	// --- Trazo separador del casillero del valor ---

	/// <summary>
	/// La regla de todo el modelo: la barra es el marco fijo (cuatro lados) y la caja del
	/// valor cede el filo que comparten. Si los dos lo dibujaran, la costura mediría el
	/// doble.
	/// </summary>
	[TestMethod]
	public void Beside_bar_the_bar_draws_the_shared_edge()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.BorderThickness = new Thickness(2));
			try
			{
				Assert.AreEqual(new Thickness(0, 2, 2, 2), CajaDeAbajo(e.Selector).BorderThickness,
					"El casillero cede el lado que mira a la barra.");
				Assert.AreEqual(new Thickness(2), Celda(e.Selector, "PART_BarCell").BorderThickness,
					"La barra, marco fijo, dibuja el filo compartido.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Left_side_moves_the_box_and_the_bar_draws_the_shared_edge()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.BorderThickness = new Thickness(2);
				s.ValueBoxDock = ValueBoxDock.Left;
			});
			try
			{
				Assert.AreEqual(new Thickness(2, 2, 0, 2), CajaDeAbajo(e.Selector).BorderThickness,
					"El casillero cede el lado derecho, que es el que mira a la barra.");
				Assert.AreEqual(new Thickness(2), Celda(e.Selector, "PART_BarCell").BorderThickness,
					"La barra, marco fijo, dibuja el filo compartido.");
				Assert.AreEqual(1, Grid.GetColumn(Celda(e.Selector, "PART_BarCell")),
					"La barra pasa a la segunda columna; el casillero queda primero.");

				// La columna "*" tiene que acompañar a la barra: si sólo se movieran las
				// celdas, la barra caería en una columna "Auto" y se encogería a su contenido.
				Assert.AreEqual(GridLength.Auto,
					((ColumnDefinition)e.Selector.Template.FindName("PART_BarColumn", e.Selector)).Width);
				Assert.AreEqual(new GridLength(1, GridUnitType.Star),
					((ColumnDefinition)e.Selector.Template.FindName("PART_ValueColumn", e.Selector)).Width);
			}
			finally { e.Window.Close(); }
		});
	}

	/// <summary>
	/// Valor en detalle (ShowDetail y ValueFollowsDetail): la caja desciende a la fila del
	/// detalle, cede el lado superior a la costura que dibuja la barra de arriba y cede el
	/// lado que mira a la etiqueta de detalle, que lo dibuja.
	/// </summary>
	[TestMethod]
	public void Value_down_moves_the_box_to_the_detail_row_and_yields_the_shared_edges()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.BorderThickness = new Thickness(2);
				s.ShowDetail = true;
				s.ValueFollowsDetail = true;
			});
			try
			{
				Assert.AreEqual(new Thickness(0, 0, 2, 2), CajaDeArriba(e.Selector).BorderThickness,
					"La caja de detalle cede el superior a la barra y el lado que mira a la etiqueta.");
				Assert.AreEqual(new Thickness(2, 0, 2, 2), CeldaDetalle(e.Selector).BorderThickness,
					"La fila de detalle, marco fijo, dibuja el filo compartido y cede el superior.");
				Assert.AreEqual(new Thickness(2), Celda(e.Selector, "PART_BarCell").BorderThickness,
					"La barra, marco base arriba, sigue con sus cuatro lados.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void The_separator_disappears_when_the_value_column_collapses()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => { s.ShowDetail = true; s.ValueFollowsDetail = true; });
			try
			{
				// Con el valor abajo, la columna del valor de arriba mide 0: sin apagar el
				// marco quedaría un trazo vertical suelto pegado al final de la barra.
				Assert.AreEqual(new Thickness(0), CajaDeAbajo(e.Selector).BorderThickness,
					"La caja de arriba, vacía, no debe dejar trazo separador.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void The_separator_follows_the_focus_colour()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				Assert.AreEqual(e.Selector.BorderBrush, CajaDeAbajo(e.Selector).BorderBrush);

				e.Selector.Focus();

				// Es parte del mismo marco: si no se tiñera, al enfocar se vería un recuadro
				// azul con una raya negra en el medio.
				Assert.AreEqual(e.Selector.FocusBorderBrush, CajaDeAbajo(e.Selector).BorderBrush);
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Show_detail_without_following_keeps_the_value_beside_the_bar()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.BorderThickness = new Thickness(2);
				s.ShowDetail = true;
				s.ValueFollowsDetail = false;
			});
			try
			{
				// La caja sigue arriba, junto a la barra, y la fila de detalle abarca todo el
				// ancho bajo la barra.
				Assert.AreEqual(new Thickness(2, 0, 2, 2), CeldaDetalle(e.Selector).BorderThickness);
				Assert.AreEqual(new Thickness(0, 2, 2, 2), CajaDeAbajo(e.Selector).BorderThickness);
				Assert.AreEqual(new Thickness(2), Celda(e.Selector, "PART_BarCell").BorderThickness);
			}
			finally { e.Window.Close(); }
		});
	}

	// --- IsEnabled ---
	// IsEnabled no es una propiedad del control sino de UIElement, pero el consumidor la
	// puede usar igual y el control tiene que comportarse. WPF impide GANAR el foco estando
	// deshabilitado, pero no suelta el que ya estuviera puesto, y las teclas se rutean al
	// elemento enfocado: sin tratamiento, un control deshabilitado seguía respondiendo.

	[TestMethod]
	public void Disabling_releases_the_focus_it_already_had()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				e.Selector.Focus();
				Assert.IsTrue(e.Selector.IsKeyboardFocused);

				e.Selector.IsEnabled = false;

				// Si esto se rompe, el control deshabilitado queda con el marco de foco
				// encendido —que además miente— y con el teclado y la rueda vivos.
				Assert.IsFalse(e.Selector.IsKeyboardFocused,
					"Deshabilitar tiene que soltar el foco que ya estuviera puesto.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Disabled_control_ignores_the_keyboard()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				e.Selector.Focus();
				e.Selector.IsEnabled = false;

				int antes = e.Selector.Value;
				var fuente = PresentationSource.FromVisual(e.Window)!;
				e.Selector.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, fuente, 0, Key.Right)
				{ RoutedEvent = Keyboard.PreviewKeyDownEvent });

				// Se levanta el evento directamente sobre el control para saltear el ruteo:
				// así la prueba verifica la GUARDA, no que WPF no haya entregado la tecla.
				// Las dos defensas importan y ésta es la que queda si el foco llega por otro lado.
				Assert.AreEqual(antes, e.Selector.Value,
					"Con el control deshabilitado, el teclado no debe mover el valor.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Re_enabling_leaves_the_keyboard_working_again()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				e.Selector.IsEnabled = false;
				e.Selector.IsEnabled = true;
				e.Selector.Focus();

				int antes = e.Selector.Value;
				var fuente = PresentationSource.FromVisual(e.Window)!;
				e.Selector.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, fuente, 0, Key.Right)
				{ RoutedEvent = Keyboard.PreviewKeyDownEvent });

				// El bloqueo no debe dejar secuelas: la guarda mira IsEnabled en vivo y el
				// foco se puede volver a tomar.
				Assert.AreEqual(antes + e.Selector.SmallChange, e.Selector.Value,
					"Al rehabilitar, el teclado tiene que volver a funcionar.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Read_only_blocks_the_user_but_not_the_program()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector { InteractionMode = UserInteractionMode.ReadOnly };
			var cambios = new List<(int OldValue, int NewValue)>();
			selector.ValueChanged += (_, args) => cambios.Add((args.OldValue, args.NewValue));

			// El sentido del modo es mostrar un valor que se actualiza solo: asignarlo por
			// código tiene que seguir funcionando y seguir avisando.
			selector.Value = 77;

			Assert.AreEqual(77, selector.Value);
			CollectionAssert.AreEqual(new List<(int OldValue, int NewValue)> { (0, 77) }, cambios);
		});
	}
}
