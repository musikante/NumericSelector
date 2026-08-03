using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelectorLib.Tests;

/// <summary>
/// Pruebas de los dos modos de interacción: ValueChangeMode e IsDisplayOnly.
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
	private static Escenario Host(Action<NumericSelector>? configurar = null)
	{
		var selector = new NumericSelector { Minimum = 0, Maximum = 100, Value = 50 };
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
		Window Window, NumericSelector Selector, FrameworkElement Bar, Button Otro);

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

	// --- ValueChangeMode ---

	[TestMethod]
	public void Interaction_defaults_leave_the_control_fully_responsive()
	{
		StaTest.Run(() =>
		{
			var selector = new NumericSelector();

			Assert.AreEqual(ValueChangeMode.ChangeOnClick, selector.ValueChangeMode);
			Assert.IsFalse(selector.IsDisplayOnly);
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
			var e = Host(s => s.ValueChangeMode = ValueChangeMode.MustFocusFirst);
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
			var e = Host(s => s.ValueChangeMode = ValueChangeMode.MustFocusFirst);
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
			var e = Host(s => s.ValueChangeMode = ValueChangeMode.MustFocusFirst);
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

	// --- IsDisplayOnly ---

	[TestMethod]
	public void Display_only_takes_the_control_out_of_the_tab_order_and_gives_it_back()
	{
		StaTest.Run(() =>
		{
			var selector = new NumericSelector();
			Assert.IsTrue(selector.Focusable);

			selector.IsDisplayOnly = true;
			Assert.IsFalse(selector.Focusable, "En sólo visualización el control no debe poder enfocarse.");

			selector.IsDisplayOnly = false;
			Assert.IsTrue(selector.Focusable, "Al salir del modo, la focusabilidad tiene que volver sola.");
		});
	}

	[TestMethod]
	public void Display_only_does_not_overrule_a_consumer_that_disabled_focus()
	{
		StaTest.Run(() =>
		{
			// La focusabilidad se quita por coerción, no por asignación, justamente para
			// no pisar la decisión de quien usa el control.
			var selector = new NumericSelector { Focusable = false };

			selector.IsDisplayOnly = true;
			selector.IsDisplayOnly = false;

			Assert.IsFalse(selector.Focusable,
				"Salir del modo no debe encender la focusabilidad que el consumidor había apagado.");
		});
	}

	[TestMethod]
	public void Display_only_releases_the_focus_it_already_had()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				e.Selector.Focus();
				Assert.IsTrue(e.Selector.IsKeyboardFocused);

				e.Selector.IsDisplayOnly = true;

				// Quitar Focusable no suelta por sí solo un foco ya puesto: si esto se rompe,
				// el control queda con el borde de foco encendido y la rueda viva.
				Assert.IsFalse(e.Selector.IsKeyboardFocused,
					"Entrar en sólo visualización tiene que soltar el foco que ya estuviera puesto.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Display_only_ignores_mouse_wheel_and_keyboard()
	{
		StaTest.Run(() =>
		{
			var e = Host();
			try
			{
				// Se sondea ANTES de entrar en el modo: hace falta saber qué valor produciría
				// un gesto que funciona, para poder afirmar que después no produce ninguno.
				var (esperado, distinto) = Sondear(e, ClickIzquierdo);

				e.Selector.IsDisplayOnly = true;
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

	[TestMethod]
	public void Display_only_blocks_the_user_but_not_the_program()
	{
		StaTest.Run(() =>
		{
			var selector = new NumericSelector { IsDisplayOnly = true };
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
