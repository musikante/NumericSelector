using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace NumericSelector
{
	// Usamos 'partial' para indicar que esta clase se define en múltiples archivos.
	public partial class BoundedNumericSelector : Control
	{
		// --- Campos de Instancia ---
		// Referencias a las partes de la plantilla que la interacción dibuja o escucha: la
		// barra y los dos textos del valor. Las piezas que sólo arman el aspecto (el grid
		// principal, la leyenda sobre la barra y el texto del detalle) viven resolviéndose
		// en el XAML, sin necesitar campo propio aquí.
		private Grid? _barGrid;
		private Border? _barRect;
		private TextBlock? _valueText;
		private UIElement? _valueDetail;
		private Point _valueDragStart;
		// Fraccion del ancho de la barra que ocupa cada zona lateral del click derecho
		// (izquierda -> Minimum, derecha -> Maximum). El centro restante -> ResetValue.
		private const double RightClickEdgeZone = 0.3;

// Si el control ya tenia el foco cuando empezo la pulsacion actual. Se toma en
// OnPreviewMouseDown (fase tunel) porque para cuando corren los handlers de las
		// partes (fase burbuja) el Focus() de esa misma pulsacion YA se aplico y consultar
		// IsKeyboardFocused ahi daria siempre true: verificado, la guarda no filtraria nada.
		private bool _hadFocusOnPress;

		// --- Constructor de Instancia ---
		public BoundedNumericSelector()
		{
			// WPF formatea los bindings según FrameworkElement.Language, cuyo valor por
			// defecto es "en-US" sin importar la configuración regional de Windows: por eso
			// un StringFormat N0 mostraría "1,000" donde corresponde "1.000".
			// Tiene que ser un valor ASIGNADO y no un default de metadata, porque quien
			// formatea es cada TextBlock de la plantilla y la herencia no propaga defaults.
			// Con SetCurrentValue el control adopta la cultura del sistema donde corra, y
			// quien lo use puede pisarlo asignando Language en la instancia.
			SetCurrentValue(LanguageProperty,
				XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag));
		}

		// --- Ciclo de Vida y Manejo de Plantilla ---
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// OnApplyTemplate puede ejecutarse más de una vez (por ejemplo si se reemplaza
			// la plantilla): soltamos las suscripciones de las partes anteriores antes de
			// tomar las nuevas, para no duplicar handlers.
			DetachTemplateParts();

			// Obtener referencias a los elementos visuales de la plantilla.
			_barGrid = GetTemplateChild("PART_BarGrid") as Grid;
			_barRect = GetTemplateChild("PART_BarRect") as Border;
			_valueText = GetTemplateChild("PART_ValueText") as TextBlock;
			_valueDetail = GetTemplateChild("PART_ValueDetailText") as UIElement;

			AttachTemplateParts();

			// Estado inicial: el relleno y los cursores.
			UpdateBarFill(Value);
			UpdateCursors();
		}

		// Las partes de la plantilla son hijas del control, así que estas suscripciones no
		// generan fugas (el ciclo control <-> hijos se recolecta junto). Por eso NO se
		// desuscriben en Unloaded: hacerlo dejaría el control inerte si se lo vuelve a
		// cargar (cambio de pestaña, por ejemplo) ya que OnApplyTemplate no se repite.
		private void AttachTemplateParts()
		{
			if (_valueText != null)
			{
				_valueText.MouseLeftButtonDown += ValueText_MouseLeftButtonDown;
				_valueText.MouseMove += ValueText_MouseMove;
				_valueText.MouseLeftButtonUp += ValueText_MouseLeftButtonUp;
			}

			// El valor en la fila de detalle (descendió con ValueFollowsDetail) usa los mismos gestos.
			if (_valueDetail != null)
			{
				_valueDetail.MouseLeftButtonDown += ValueText_MouseLeftButtonDown;
				_valueDetail.MouseMove += ValueText_MouseMove;
				_valueDetail.MouseLeftButtonUp += ValueText_MouseLeftButtonUp;
			}

			// Recalcular el relleno de la barra cuando cambie el espacio disponible,
			// y habilitar la interaccion de mouse sobre la barra.
			if (_barGrid != null)
			{
				_barGrid.SizeChanged += BarGrid_SizeChanged;
				_barGrid.MouseLeftButtonDown += BarGrid_MouseLeftButtonDown;
				_barGrid.MouseMove += BarGrid_MouseMove;
				_barGrid.MouseLeftButtonUp += BarGrid_MouseLeftButtonUp;
				_barGrid.MouseRightButtonUp += BarGrid_MouseRightButtonUp;
			}
		}

		private void DetachTemplateParts()
		{
			if (_valueText != null)
			{
				_valueText.MouseLeftButtonDown -= ValueText_MouseLeftButtonDown;
				_valueText.MouseMove -= ValueText_MouseMove;
				_valueText.MouseLeftButtonUp -= ValueText_MouseLeftButtonUp;
			}

			if (_valueDetail != null)
			{
				_valueDetail.MouseLeftButtonDown -= ValueText_MouseLeftButtonDown;
				_valueDetail.MouseMove -= ValueText_MouseMove;
				_valueDetail.MouseLeftButtonUp -= ValueText_MouseLeftButtonUp;
			}

			if (_barGrid != null)
			{
				_barGrid.SizeChanged -= BarGrid_SizeChanged;
				_barGrid.MouseLeftButtonDown -= BarGrid_MouseLeftButtonDown;
				_barGrid.MouseMove -= BarGrid_MouseMove;
				_barGrid.MouseLeftButtonUp -= BarGrid_MouseLeftButtonUp;
				_barGrid.MouseRightButtonUp -= BarGrid_MouseRightButtonUp;
			}
		}

		// Reacciona a los cambios de estado que exigen redibujar cursores o soltar el foco.
		// El ancho del control NO se recalcula acá: el piso y el crecimiento los resuelve
		// la medición natural del contenido en MeasureOverride (el sizer oculto del casillero
		// ya reserva el ancho del número más largo, y BaseWidth define el ancho base).
		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.Property == InteractionModeProperty || e.Property == MouseBehaviorProperty)
			{
				UpdateCursors();
			}

			// IsEnabled=false impide GANAR el foco, pero no suelta el que ya estuviera
			// puesto (ver ReleaseKeyboardFocusIfHeld). Sin esto, deshabilitar un control
			// enfocado lo dejaba respondiendo al teclado y con el marco de foco encendido.
			if (e.Property == IsEnabledProperty && !IsEnabled)
			{
				ReleaseKeyboardFocusIfHeld();
			}
		}

// --- Medición ---

// margen de seguridad ("yapa") sobre el ancho medido de los textos. El redondeo de
		// WPF sobre los glifos (UseLayoutRounding + las mediciones fraccionarias de
		// FormattedText) hace que el ancho "natural" que reporta la plantilla no alcance por
		// un pelín al ancho que el render pide en unos pocos píxeles: el resultado es el
		// Caption/Detail con CharacterEllipsis a un tamaño de fuente y sin ella un peldaño
		// más arriba, aunque el control tenga hueco de sobra. Si el control crece desde
		// BaseWidth o del contenido, pedirle algunos píxeles extra al layout (que sólo se
		// conceden cuando el contenedor tiene espacio) aleja el texto del límite de la
		// elipsis. Cuando el hueco es escaso el clamp de abajo sigue recortando igual.
		private const double MeasureSlack = 3.0;

		// El control crece desde BaseWidth (o desde el ancho natural del contenido si es NaN)
		// para acomodar su texto, pero NUNCA pide más ancho que el hueco que le da el panel:
		// así su marco (los bordes) no se sale del contenedor y no se recorta. Si el texto no
		// cabe en ese hueco, la CharacterEllipsis de la leyenda entra a tallar. BaseWidth no
		// es un constraint duro de WPF (no fuerza el tamaño, sólo es un piso).
		protected override Size MeasureOverride(Size constraint)
		{
			// Ancho natural del contenido. Se mide con ancho infinito a propósito: la barra
			// vive en una columna '*', que ante un ancho finito se estiraría a todo lo
			// disponible y daría como "necesario" el ancho del contenedor entero. Con ancho
			// infinito las columnas '*' se comportan como Auto.
			Size natural = base.MeasureOverride(new Size(double.PositiveInfinity, constraint.Height));

			// El ancho natural redondeado hacia arriba, más la yapa: así el largo que se
			// reserva siempre alcanza para el texto medido por el render, y un cambio
			// de un píxel en la medición no decide solo.
			double reserved = Math.Ceiling(natural.Width) + MeasureSlack;

			// Piso = BaseWidth si el consumidor lo fijó; el crecimiento nunca reduce el
			// contenido, así que también vale el ancho natural si es más grande.
			double width = Math.Max(
				double.IsNaN(BaseWidth) ? 0 : BaseWidth,
				reserved);

			// No crecer más de lo que el contenedor ofrece: si hay hueco crece (desde el
			// piso BaseWidth); si no, el marco queda a lo disponible y la leyenda se trunca
			// con elipsis. Vacío o infinito en la restricción de ancho se deja pasar tal cual.
			if (!double.IsPositiveInfinity(constraint.Width) && !double.IsNaN(constraint.Width)
				&& width > constraint.Width)
			{
				width = constraint.Width;
			}

			// Se vuelve a medir con el ancho definido por el piso para que la plantilla
			// distribuya las columnas (la barra '*' llena y la leyenda trunca con elipsis si
			// falta espacio), pero el DesiredSize devuelto es el MAYOR entre el piso pedido
			// y lo que la plantilla midió: la columna '*' no reporta ancho en el DesiredSize
			// del Grid, así que sin este máximo el piso de BaseWidth sería invisible para un
			// StackPanel y el control se encogería a su contenido pese a tener hueco.
			Size measured = base.MeasureOverride(new Size(width, constraint.Height));
			return new Size(Math.Max(width, measured.Width), Math.Max(measured.Height, natural.Height));
		}

		// --- Manejo de Eventos de Interfaz de Usuario (de instancia) ---

		// Lógica específica de la instancia para el manejo de teclas.
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			// Ni en solo-visualizacion ni deshabilitado deberia llegar tecla alguna, porque
			// en ambos casos se suelta el foco y sin foco no hay teclado. La guarda es de una
			// linea y cierra el caso de un foco que llegue por algun otro camino.
			// El modo MustFocusFirst NO entra aca: es una regla de mouse. Si el control
			// tiene el foco para recibir la tecla, el requisito ya esta cumplido.
			if (InteractionMode == UserInteractionMode.ReadOnly || !IsEnabled)
				return;

			switch (e.Key)
			{
				case Key.Left:
				case Key.Down:
				case Key.Subtract:
				case Key.OemMinus:
					Value -= SmallChange;
					e.Handled = true;
					break;
				case Key.Right:
				case Key.Up:
				case Key.Add:
				case Key.OemPlus:
					Value += SmallChange;
					e.Handled = true;
					break;
				case Key.PageDown:
					Value -= LargeChange;
					e.Handled = true;
					break;
				case Key.PageUp:
					Value += LargeChange;
					e.Handled = true;
					break;
				case Key.Home:
					Value = Minimum;
					e.Handled = true;
					break;
				case Key.End:
					Value = Maximum;
					e.Handled = true;
					break;
				case Key.Delete:
				case Key.Insert:
					Value = ResetValue;
					e.Handled = true;
					break;
			}
		}

		private void BarGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateBarFill(Value);
		}

		// --- Interacción de mouse ---

		// Convierte una posicion del mouse (dentro de la barra) al valor entero
		// correspondiente, a lo largo del eje horizontal (0 = izquierda, 1 = derecha).
		private int ValueFromPosition(Point p)
		{
			if (_barGrid == null)
				return Minimum;

			double w = _barGrid.ActualWidth;
			double ratio = w <= 0 ? 0 : Math.Clamp(p.X / w, 0, 1);
			return RatioToValue(ratio);
		}

		private int RatioToValue(double ratio)
		{
			ratio = Math.Clamp(ratio, 0, 1);
			// El rango se calcula en long para que un Minimum/Maximum extremo no desborde.
			long range = (long)Maximum - Minimum;
			long value = Minimum + (long)Math.Round(ratio * range);
			return (int)Math.Clamp(value, Minimum, Maximum);
		}

		private void BarGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// Sin captura: si el gesto no esta habilitado tampoco debe poder arrastrarse
			// sin soltar el boton desde ese primer click.
			if (!MouseGesturesAllowed)
				return;

			_barGrid?.CaptureMouse();
			Value = ValueFromPosition(e.GetPosition(_barGrid));
			e.Handled = true;
		}

		private void BarGrid_MouseMove(object sender, MouseEventArgs e)
		{
			// La captura ya implica que el gesto arranco habilitado; se revalida por si el
			// modo cambio por codigo en medio del arrastre.
			if (!MouseGesturesAllowed)
				return;

			if (_barGrid != null && _barGrid.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
				Value = ValueFromPosition(e.GetPosition(_barGrid));
		}

		private void BarGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (_barGrid != null && _barGrid.IsMouseCaptured)
				_barGrid.ReleaseMouseCapture();
		}

		// Click derecho por zonas: izquierda -> Minimum, centro -> ResetValue, derecha -> Maximum.
		private void BarGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (!MouseGesturesAllowed)
				return;

			if (_barGrid == null)
				return;

			double width = _barGrid.ActualWidth;
			if (width <= 0)
				return;

			double ratio = Math.Clamp(e.GetPosition(_barGrid).X / width, 0, 1);

			if (ratio < RightClickEdgeZone) Value = Minimum;
			else if (ratio > 1 - RightClickEdgeZone) Value = Maximum;
			else Value = ResetValue;

			e.Handled = true;
		}

		// Doble-click en el numero -> ResetValue.
		// Un solo click y arrastre vertical -> ajusta el valor de a SmallChange.
		private void ValueText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (!MouseGesturesAllowed)
				return;

			if (e.ClickCount == 2)
			{
				Value = ResetValue;
				e.Handled = true;
				return;
			}

			if (sender is UIElement el)
			{
				el.CaptureMouse();
				_valueDragStart = e.GetPosition(el);
			}
			e.Handled = true;
		}

		private void ValueText_MouseMove(object sender, MouseEventArgs e)
		{
			if (!MouseGesturesAllowed)
				return;

			if (sender is not UIElement el || !el.IsMouseCaptured)
				return;

			Point current = e.GetPosition(el);
			double delta = _valueDragStart.Y - current.Y; // arrastrar hacia arriba sube el valor

			if (delta >= 1) { Value += SmallChange; _valueDragStart = current; }
			else if (delta <= -1) { Value -= SmallChange; _valueDragStart = current; }
		}

		private void ValueText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (sender is UIElement el && el.IsMouseCaptured)
				el.ReleaseMouseCapture();
		}

		// Rueda del mouse -> +/- SmallChange, con dos recaudos para no "comerse" el scroll
		// de un ScrollViewer contenedor (MouseWheel es un evento de burbuja: si lo marcamos
		// como manejado, el ScrollViewer nunca se entera y la lista no se desplaza):
		//   1) Solo actuamos si el control tiene el foco. Asi, pasar el mouse por encima
		//      mientras se scrollea una lista no altera valores por accidente.
		//   2) Solo marcamos Handled si el valor realmente cambio. En los topes la rueda
		//      sigue sirviendo para scrollear en vez de quedar muerta.
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);

			// La rueda ya exigia foco en los dos modos, asi que MustFocusFirst no le agrega
			// nada. InteractionMode=ReadOnly si: sin él, un foco heredado la dejaria viva.
			if (InteractionMode == UserInteractionMode.ReadOnly || !IsKeyboardFocused)
				return;

			int before = Value;
			Value += e.Delta > 0 ? SmallChange : -SmallChange;
			e.Handled = Value != before;
		}

		// Cualquier click (izquierdo, derecho o central) en cualquier parte del control
		// le da el foco. Usa el evento tunel para adelantarse a los handlers hijos.
		protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseDown(e);

			// Antes de enfocar: los handlers de la fase burbuja necesitan saber si el foco
			// ya estaba puesto ANTES de esta pulsacion (ver _hadFocusOnPress).
			_hadFocusOnPress = IsKeyboardFocused;

			if (InteractionMode == UserInteractionMode.ReadOnly)
				return;

			Focus();
		}

		// El foco entra en la decision del cursor (ver UpdateCursors), asi que hay que
		// repintarlo cuando llega y cuando se va.
		protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnGotKeyboardFocus(e);
			UpdateCursors();
		}

		protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnLostKeyboardFocus(e);
			UpdateCursors();
		}

		// Soltar el foco de teclado si el control lo tiene puesto.
		// Ni quitar Focusable (InteractionMode.ReadOnly) ni IsEnabled=false lo sueltan por
		// su cuenta: verificado en los dos casos, IsKeyboardFocused seguía en true. Y mientras el
		// control lo conserve el teclado le SIGUE llegando, porque las teclas se rutean al
		// elemento enfocado y no al que está bajo el puntero: medido, una flecha movía el
		// valor de un control deshabilitado. Además el marco de foco queda encendido, que
		// en un control deshabilitado es directamente mentira.
		// El mouse no necesita este cuidado: con IsEnabled=false el hit-test de entrada ya
		// deja de devolver partes del control (verificado con InputHitTest), y como la rueda
		// también se rutea desde el elemento bajo el puntero, queda cubierta por lo mismo.
		private void ReleaseKeyboardFocusIfHeld()
		{
			if (!IsKeyboardFocused)
				return;

			// Al ancestro enfocable: el foco tiene que ir a algún lado, y devolvérselo
			// a la ventana lo saca del control sin robárselo a otro control concreto.
			var scope = FocusManager.GetFocusScope(this);
			FocusManager.SetFocusedElement(scope, null);
			Keyboard.ClearFocus();
		}

		// --- Cursores ---

		// El cursor dice la verdad sobre si el gesto va a hacer algo:
		//   InteractionMode.ReadOnly        -> Arrow (gana sobre todo lo demas)
		//   MustFocusFirst y sin foco      -> Arrow (el proximo click solo va a enfocar)
		//   resto                           -> el cursor del gesto
		// En ChangeOnClick NO depende del foco: ahi el gesto funciona igual sin el, y
		// mostrar Arrow seria mentir.
		private void UpdateCursors()
		{
			bool gestures = InteractionMode != UserInteractionMode.ReadOnly &&
				(MouseBehavior == MouseInteractionBehavior.ChangeOnClick || IsKeyboardFocused);

			// Arrastre horizontal sobre la barra.
			if (_barGrid != null)
				_barGrid.Cursor = gestures ? Cursors.SizeWE : Cursors.Arrow;

			// El casillero del valor se arrastra en vertical en cualquier orientacion.
			var valueCursor = gestures ? Cursors.SizeNS : Cursors.Arrow;
			if (_valueText != null) _valueText.Cursor = valueCursor;
			if (_valueDetail is FrameworkElement fed) fed.Cursor = valueCursor;

			// WPF resuelve el cursor durante el movimiento del mouse. Este cambio ocurre
			// con el puntero QUIETO (se hace click, llega el foco, la mano no se movio),
			// asi que hay que pedir la reevaluacion a mano o el cursor nuevo no se veria
			// hasta el proximo movimiento, justo cuando el aviso mas importa.
			if (IsMouseOver)
				Mouse.UpdateCursor();
		}

		// --- Habilitación de los gestos ---

		// Unico punto de decision para todos los gestos de mouse: en solo-visualizacion no
		// actua ninguno, y en MustFocusFirst sólo actuan si el control ya tenia el foco al
		// empezar la pulsacion (asi el click que enfoca no cambia ademas el valor).
		// Quitar Focusable NO alcanza para frenar el mouse: verificado, los handlers de las
		// partes siguen corriendo igual y el valor cambiaba.
		private bool MouseGesturesAllowed =>
			InteractionMode != UserInteractionMode.ReadOnly &&
			(MouseBehavior == MouseInteractionBehavior.ChangeOnClick || _hadFocusOnPress);

		// Actualiza el tamaño del rectangulo de relleno para que represente
		// la proporcion del valor actual dentro del rango [Minimum, Maximum].
		private void OnValueChangedHandler(int newValue)
		{
			UpdateBarFill(newValue);
			// El texto del valor se actualiza automaticamente por binding a Value.
		}

		private void UpdateBarFill(int value)
		{
			if (_barRect == null || _barGrid == null)
				return;

			// El rango se calcula en long para que un Minimum/Maximum extremo no desborde.
			long range = (long)Maximum - Minimum;
			double ratio = (range > 0) ? Math.Clamp((double)(value - (long)Minimum) / range, 0, 1) : 0;

			// Eje horizontal: el relleno crece hacia la derecha en proporcion al valor.
			// El alto se fija por codigo porque el rectangulo vive dentro de un Canvas
			// (ver Generic.xaml), que no estira a sus hijos.
			_barRect.Width = _barGrid.ActualWidth * ratio;
			_barRect.Height = _barGrid.ActualHeight;
		}
	}
}
