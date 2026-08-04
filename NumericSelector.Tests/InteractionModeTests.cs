using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelector.Tests;

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

	// --- ValueChangeMode ---

	[TestMethod]
	public void Interaction_defaults_leave_the_control_fully_responsive()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector();

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
			var selector = new BoundedNumericSelector();
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
			var selector = new BoundedNumericSelector { Focusable = false };

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

	// --- ShowTitleFrame ---
	// Es una propiedad de apariencia, pero sus pruebas viven acá porque la regla que
	// importa es su interacción con el FOCO, y para eso hace falta una ventana real.

	private static Border Celda(BoundedNumericSelector selector, string parte) =>
		(Border)selector.Template.FindName(parte, selector);

	private static Border MarcoDelTitulo(BoundedNumericSelector selector) =>
		Celda(selector, "PART_TitleCell");

	// Se compara el COLOR y no la instancia de la brocha: el convertidor de XAML puede
	// devolver una instancia propia para "Transparent" y una comparación por referencia
	// sería frágil sin agregar nada.
	private static void AssertTransparente(Brush brocha, string mensaje) =>
		Assert.AreEqual(Colors.Transparent, ((SolidColorBrush)brocha).Color, mensaje);

	[TestMethod]
	public void Title_frame_is_drawn_by_default()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.ShowTitleText = true);
			try
			{
				Assert.IsTrue(e.Selector.ShowTitleFrame, "El default conserva el aspecto de siempre.");
				Assert.AreEqual(e.Selector.ControlBorderColor, MarcoDelTitulo(e.Selector).BorderBrush);
				Assert.AreEqual(e.Selector.Background, MarcoDelTitulo(e.Selector).Background);
				Assert.AreEqual(new Thickness(1, 1, 1, 0), MarcoDelTitulo(e.Selector).BorderThickness,
					"Sin inferior: la costura la dibuja el borde superior de la barra.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Turning_off_the_title_frame_makes_border_and_background_transparent()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => { s.ShowTitleText = true; s.ShowTitleFrame = false; });
			try
			{
				var marco = MarcoDelTitulo(e.Selector);

				// Transparent y NO nulo: con nulo la fila del título dejaría de recibir los
				// clics que dan el foco, porque una brocha nula no pinta y no se golpea.
				AssertTransparente(marco.BorderBrush, "El borde del título tiene que quedar transparente.");
				AssertTransparente(marco.Background, "El fondo del título tiene que quedar transparente.");

				// La geometría no se toca: sólo cambia lo que se pinta.
				Assert.AreEqual(new Thickness(1, 1, 1, 0), marco.BorderThickness,
					"Apagar el marco no debe alterar el grosor reservado.");
				Assert.AreEqual(e.Selector.Background, Celda(e.Selector, "PART_BarCell").Background,
					"Sólo se apaga la celda del título; la barra conserva su fondo.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Focus_does_not_light_up_a_title_that_has_no_frame()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => { s.ShowTitleText = true; s.ShowTitleFrame = false; });
			try
			{
				e.Selector.Focus();
				Assert.IsTrue(e.Selector.IsKeyboardFocused);

				// La regla acordada: si el marco del título está apagado, el foco no se lo
				// devuelve de sorpresa. Está escrita en la condición de un MultiTrigger y no
				// en el orden de declaración, justamente para que esta prueba la sostenga.
				AssertTransparente(MarcoDelTitulo(e.Selector).BorderBrush,
					"Con el marco apagado, el foco no debe encender el borde del título.");

				Assert.AreEqual(e.Selector.FocusBorderColor, Celda(e.Selector, "PART_BarCell").BorderBrush,
					"Las celdas de datos siguen siendo las que señalan el foco.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Focus_still_lights_up_a_title_that_has_a_frame()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.ShowTitleText = true);
			try
			{
				e.Selector.Focus();

				// Contrapunto de la prueba anterior: sin ésta, apagar por error el teñido
				// del título en TODOS los casos pasaría inadvertido.
				Assert.AreEqual(e.Selector.FocusBorderColor, MarcoDelTitulo(e.Selector).BorderBrush,
					"Con marco, el título se sigue tiñendo al enfocar.");
			}
			finally { e.Window.Close(); }
		});
	}

	// --- Trazo separador del casillero del valor ---

	private static Border TrazoDelValor(BoundedNumericSelector selector) =>
		Celda(selector, "PART_ValueSideCell");

	/// <summary>
	/// La regla de todo el modelo: la caja del valor tiene prioridad y lleva sus cuatro
	/// lados; el vecino cede el filo que comparten. Si los dos lo dibujaran, la costura
	/// mediría el doble.
	/// </summary>
	[TestMethod]
	public void Beside_bar_the_value_box_owns_the_shared_edge()
	{
		StaTest.Run(() =>
		{
			var e = Host(s => s.ControlBorderPixels = new Thickness(2));
			try
			{
				Assert.AreEqual(new Thickness(2), TrazoDelValor(e.Selector).BorderThickness,
					"El casillero del valor lleva los cuatro lados.");
				Assert.AreEqual(new Thickness(2, 2, 0, 2), Celda(e.Selector, "PART_BarCell").BorderThickness,
					"La barra cede el lado derecho, que es el que toca al casillero.");
			}
			finally { e.Window.Close(); }
		});
	}

	/// <summary>
	/// En WithTitle la caja sube y se lleva su marco sin el inferior; la etiqueta cede el
	/// derecho a esa caja y el inferior a la barra, que dibuja una costura uniforme a lo
	/// ancho; y la barra, sola abajo, recupera los cuatro lados.
	/// </summary>
	[TestMethod]
	public void With_title_moves_the_box_up_and_the_neighbours_yield()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.ControlBorderPixels = new Thickness(2);
				s.ShowTitleText = true;
				s.ValuePlacement = ValuePlacement.WithTitle;
			});
			try
			{
				Assert.AreEqual(new Thickness(2, 2, 2, 0), Celda(e.Selector, "PART_ValueTopCell").BorderThickness);
				Assert.AreEqual(new Thickness(2, 2, 0, 0), MarcoDelTitulo(e.Selector).BorderThickness);
				Assert.AreEqual(new Thickness(2), Celda(e.Selector, "PART_BarCell").BorderThickness);
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void The_separator_disappears_where_the_value_column_collapses()
	{
		StaTest.Run(() =>
		{
			// En OnBar y WithTitle la columna del valor mide 0. Sin esto quedaría un trazo
			// vertical suelto pegado al final de la barra.
			foreach (var donde in new[] { ValuePlacement.OnBar, ValuePlacement.WithTitle })
			{
				var e = Host(s => { s.ShowTitleText = true; s.ValuePlacement = donde; });
				try
				{
					Assert.AreEqual(new Thickness(0), TrazoDelValor(e.Selector).BorderThickness,
						$"En {donde} no debe quedar trazo separador.");
				}
				finally { e.Window.Close(); }
			}
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
				Assert.AreEqual(e.Selector.ControlBorderColor, TrazoDelValor(e.Selector).BorderBrush);

				e.Selector.Focus();

				// Es parte del mismo marco: si no se tiñera, al enfocar se vería un recuadro
				// azul con una raya negra en el medio.
				Assert.AreEqual(e.Selector.FocusBorderColor, TrazoDelValor(e.Selector).BorderBrush);
			}
			finally { e.Window.Close(); }
		});
	}

	// --- ShowValueFrame ---
	// Lo que importa acá es la cláusula que cierra el contorno: si la caja del valor deja de
	// pintar, el vecino tiene que recuperar el lado que le había cedido, o el rectángulo
	// queda abierto justo sobre el ancho del número.

	/// <summary>
	/// El alcance está acotado a WithTitle a propósito: en BesideBar la caja del valor es
	/// parte del rectángulo principal, y apagarla daría otro aspecto (barra encajonada y
	/// número afuera) en vez de una variante del mismo.
	/// </summary>
	[TestMethod]
	public void Unframing_the_value_has_no_effect_outside_with_title()
	{
		StaTest.Run(() =>
		{
			foreach (var donde in new[] { ValuePlacement.BesideBar, ValuePlacement.OnBar })
			{
				var e = Host(s =>
				{
					s.ControlBorderPixels = new Thickness(2);
					s.ShowTitleText = true;
					s.ValuePlacement = donde;
					s.ShowValueFrame = false;
				});
				try
				{
					Assert.AreEqual(e.Selector.ControlBorderColor, TrazoDelValor(e.Selector).BorderBrush,
						$"En {donde} el casillero conserva su marco.");
					Assert.AreEqual(e.Selector.ControlBorderColor, Celda(e.Selector, "PART_BarCell").BorderBrush,
						$"En {donde} la barra no cambia.");
				}
				finally { e.Window.Close(); }
			}
		});
	}

	[TestMethod]
	public void Unframing_the_value_in_with_title_leaves_the_number_loose()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.ControlBorderPixels = new Thickness(2);
				s.ShowTitleText = true;
				s.ValuePlacement = ValuePlacement.WithTitle;
				s.ShowValueFrame = false;
			});
			try
			{
				var caja = Celda(e.Selector, "PART_ValueTopCell");
				AssertTransparente(caja.BorderBrush, "La caja de arriba deja de pintar su borde.");
				AssertTransparente(caja.Background, "Y su fondo.");

				// Se apaga la pintura, no el grosor: el número no se mueve.
				Assert.AreEqual(new Thickness(2, 2, 2, 0), caja.BorderThickness);
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void With_title_the_edge_goes_back_to_the_title_only_when_it_is_framed()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.ControlBorderPixels = new Thickness(2);
				s.ShowTitleText = true;
				s.ValuePlacement = ValuePlacement.WithTitle;
				s.ShowValueFrame = false;
			});
			try
			{
				Assert.AreEqual(new Thickness(2, 2, 2, 0), MarcoDelTitulo(e.Selector).BorderThickness,
					"Con el título enmarcado, recupera el derecho para cerrar el contorno.");

				// Con el título sin marco no hay contorno que cerrar, y cambiar el grosor sólo
				// correría su texto: por eso la recuperación no debe ocurrir.
				e.Selector.ShowTitleFrame = false;
				e.Window.UpdateLayout();

				Assert.AreEqual(new Thickness(2, 2, 0, 0), MarcoDelTitulo(e.Selector).BorderThickness,
					"Sin marco de título, el grosor se queda como estaba.");
			}
			finally { e.Window.Close(); }
		});
	}

	[TestMethod]
	public void Focus_does_not_light_up_a_value_box_that_has_no_frame()
	{
		StaTest.Run(() =>
		{
			var e = Host(s =>
			{
				s.ShowTitleText = true;
				s.ValuePlacement = ValuePlacement.WithTitle;
				s.ShowValueFrame = false;
			});
			try
			{
				e.Selector.Focus();
				Assert.IsTrue(e.Selector.IsKeyboardFocused);

				AssertTransparente(Celda(e.Selector, "PART_ValueTopCell").BorderBrush,
					"Con el marco apagado, el foco no debe encender la caja del valor.");
				Assert.AreEqual(e.Selector.FocusBorderColor, Celda(e.Selector, "PART_BarCell").BorderBrush,
					"La barra sigue señalando el foco.");
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
	public void Display_only_blocks_the_user_but_not_the_program()
	{
		StaTest.Run(() =>
		{
			var selector = new BoundedNumericSelector { IsDisplayOnly = true };
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
