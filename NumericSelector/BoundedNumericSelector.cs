using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace NumericSelector
{
	// Usamos 'partial' para indicar que esta clase se define en múltiples archivos.
	public partial class BoundedNumericSelector : Control
	{
		// --- Campos de Instancia ---
		// Referencias a las partes de la plantilla que la interacción dibuja o escucha: la
		// barra y los dos textos del valor. Las piezas que sólo arman el aspecto (el grid
		// principal, el título y la leyenda) viven resolviéndose en el XAML, sin necesitar
		// campo propio aquí.
		private Grid? _barGrid;
		private Border? _barRect;
		private TextBlock? _valueText;
		private UIElement? _valueDetail;
		private Point _valueDragStart;
		// Width que tenía antes de entrar en AutoGrow, para restituirlo al volver a Fixed.
		// No hace falta marca de agua aparte: en AutoGrow el propio Width la cumple, porque
		// sólo se le asignan valores mayores (igual que en el LNSlider de VB6).
		private double _widthBeforeGrow = double.NaN;
		// Ancho pedido por MeasureOverride y todavía no aplicado (ver RequestGrowTo).
		private double _pendingGrow = double.NaN;

		// Fraccion del ancho de la barra que ocupa cada zona lateral del click derecho
		// (izquierda -> Minimum, derecha -> Maximum). El centro restante -> ResetValue.
		private const double RightClickEdgeZone = 0.3;

// Si el control ya tenia el foco cuando empezo la pulsacion actual. Se toma en
// OnPreviewMouseDown (fase tunel) porque para cuando corren los handlers de las
		// partes (fase burbuja) el Focus() de esa misma pulsacion YA se aplico y consultar
		// IsKeyboardFocused ahi daria siempre true: verificado, la guarda no filtraria nada.
		private bool _hadFocusOnPress;

		// Contexto de medición de texto: la tipografía "global" del control (parte),
		// armada una sola vez por cambio de fuente/idioma y reutilizada en todas las
		// mediciones. El DPI no se guarda acá: se obtiene por tanda, en vivo.
		private TextMeasureContext? _measureContext;

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

			// Contexto de medición y piso inicial, acordes a la fuente y el rango por defecto.
			RebuildMeasureContext();
			UpdateMinimumRequiredWidth();
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

		// --- Ancho mínimo: el que necesita el valor ---

		// Padding horizontal del casillero del valor en la plantilla (4 + 4).
		private const double ValueBoxPadding = 8;

		// El valor es lo único irrenunciable del control: la leyenda puede recortarse con
		// elipsis y la barra puede achicarse, pero un número cortado es un dato ilegible.
		// Por eso el piso del ancho sale del número más largo del rango.
		// Se calcula acá y NO en MeasureOverride: mutar propiedades de layout durante la
		// medición es la trampa que ya nos costó varias rondas.
		// La cultura de formato es la de Language, no la del sistema operativo: son
		// independientes (Language puede pisarse por instancia) y lo que mide el piso tiene
		// que coincidir con lo que el binding con StringFormat termina mostrando.
		private CultureInfo FormatCulture => ((XmlLanguage)GetValue(LanguageProperty)).GetSpecificCulture();

		private void UpdateMinimumRequiredWidth()
		{
			string min = Minimum.ToString("N0", FormatCulture);
			string max = Maximum.ToString("N0", FormatCulture);

			// El DPI se lee una sola vez por tanda de recálculo y se comparte entre todas
			// las mediciones de la tanda: cada monitor puede tener el suyo y no conviene
			// guardarlo, pero medir dos textos seguidos en la misma pasada con escalas
			// distintas no tiene sentido.
			double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

			// Alcanza con los dos extremos: entre los negativos el más ancho es el más
			// negativo (Minimum) y entre los positivos el más grande (Maximum), porque la
			// cantidad de dígitos crece con el valor absoluto y el signo es constante.
			// Verificado además de forma empírica sobre rangos con cambio de signo y cruces
			// de separador de miles.
			double texto = Math.Max(
				TextMeasure.Measure(_measureContext!, dpi, min).Width,
				TextMeasure.Measure(_measureContext!, dpi, max).Width);

			// +1 de tolerancia, en el espíritu del LNSlider: que sobre no molesta, que falte
			// corta el número. FormattedText y TextBlock pueden diferir por fracciones.
			// El último sumando es el trazo que separa la barra del casillero. Sólo se dibuja
			// cuando el casillero está junto a la barra, pero el piso se calcula igual para
			// todas las disposiciones: reservar de más no molesta —la barra absorbe la
			// diferencia— y así el piso no depende de la disposición ni hay que recalcularlo
			// al cambiarla.
			double piso = Math.Ceiling(texto) + 1 + ValueBoxPadding
						+ ControlBorderPixels.Left + ControlBorderPixels.Right
						+ ControlBorderPixels.Left;

			SetCurrentValue(MinWidthProperty, piso);
		}

		// Arma el contexto de medición con la tipografía "global" del control y la cultura
		// de formato. Se reconstruye sólo cuando cambia alguno de esos insumos (ver
		// OnPropertyChanged): el Typeface es la parte cara de la medición y por eso se
		// amortiza, mientras que el DPI se pasa por tanda sin cachear.
		private void RebuildMeasureContext()
		{
			_measureContext = new TextMeasureContext(
				new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
				FontSize,
				FormatCulture,
				FlowDirection);
		}

		// Recalcula el piso ante todo lo que cambie el ancho del número: el rango y la fuente.
		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			// La tipografía y la cultura que miden los textos viven en el contexto: se
			// reconstruye sólo cuando cambian esos insumos. El DPI no: se obtiene por tanda.
			if (e.Property == FontSizeProperty || e.Property == FontFamilyProperty ||
				e.Property == FontStyleProperty || e.Property == FontWeightProperty ||
				e.Property == FontStretchProperty ||
				// La cultura (de Language) y la dirección entran en la medición del texto.
				e.Property == LanguageProperty || e.Property == FlowDirectionProperty)
			{
				RebuildMeasureContext();
			}

			if (e.Property == MinimumProperty || e.Property == MaximumProperty ||
				e.Property == ControlBorderPixelsProperty ||
				e.Property == FontSizeProperty || e.Property == FontFamilyProperty ||
				e.Property == FontStyleProperty || e.Property == FontWeightProperty ||
				e.Property == FontStretchProperty ||
				// La cultura cambia el separador de miles y el signo negativo, y la
				// dirección de escritura entra en la medición del texto.
				e.Property == LanguageProperty || e.Property == FlowDirectionProperty)
			{
				UpdateMinimumRequiredWidth();
			}

			if (e.Property == IsReadOnlyProperty || e.Property == ValueChangeModeProperty)
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

		// AutoGrow, calcado del LNSlider (VB6): en vez de negociar con el sistema de layout,
		// se calcula el ancho que el contenido necesita y se asigna Width directamente, sólo
		// si hace falta más que el actual ('Only Width stretching is allowed').
		//   CalcWidth = TitleWidth + ValueWidth
		//   If CalcWidth > UserControl.Width Then UserControl.Width = CalcWidth
		// El propio Width hace de marca de agua, porque nunca se le asigna un valor menor.
		protected override Size MeasureOverride(Size constraint)
		{
			if (StretchMode != StretchMode.AutoGrow)
				return base.MeasureOverride(constraint);

			// Ancho que el contenido necesita. Se mide con ancho infinito a propósito: la
			// barra vive en una columna '*', que ante un ancho finito se estira a todo lo
			// disponible y daría como "necesario" el ancho del contenedor entero. Con ancho
			// infinito las columnas '*' se comportan como Auto.
			Size natural = base.MeasureOverride(new Size(double.PositiveInfinity, constraint.Height));

			// La DECISIÓN de crecer se toma con el ancho natural pelado; la holgura se suma
			// sólo al destino. Si la holgura entrara en la comparación, un contenido que ya
			// mide casi lo mismo que el control pediría crecer indefinidamente, de a 2px por
			// pasada (fue exactamente el lazo que hizo que el control tapara la pantalla).
			double bare = Math.Ceiling(natural.Width);

			if (double.IsNaN(Width) || bare > Width)
				RequestGrowTo(bare + 2);

			return base.MeasureOverride(constraint);
		}

		// El ancho NO se puede asignar desde adentro de MeasureOverride: WPF descarta la
		// invalidación que provoca (el elemento ya se está midiendo), así que el control
		// quedaba medido con el ancho viejo — crecía un carácter y se congelaba, con Width
		// diciendo un número y ActualWidth otro. Por eso se difiere a después del layout.
		private void RequestGrowTo(double width)
		{
			// Si ya hay pedido en curso por un ancho igual o mayor, no encolamos otro.
			if (!double.IsNaN(_pendingGrow) && width <= _pendingGrow) return;
			_pendingGrow = width;

			Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
			{
				double w = _pendingGrow;
				_pendingGrow = double.NaN;

				// Siempre un número concreto: asignar NaN rompería la conversión de un
				// binding que el consumidor tuviera sobre Width.
				if (StretchMode == StretchMode.AutoGrow && (double.IsNaN(Width) || w > Width))
					SetCurrentValue(WidthProperty, w);
			}));
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
			if (IsReadOnly || !IsEnabled)
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

		// Doble-click en el numero -> ResetValue (gesto heredado del LNSlider VB6).
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
			// nada. IsReadOnly si: sin el, un foco heredado la dejaria viva.
			if (IsReadOnly || !IsKeyboardFocused)
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

			if (IsReadOnly)
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
		// Ni quitar Focusable (IsReadOnly) ni IsEnabled=false lo sueltan por su cuenta:
		// verificado en los dos casos, IsKeyboardFocused seguía en true. Y mientras el
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
		//   IsReadOnly                -> Arrow (gana sobre todo lo demas)
		//   MustFocusFirst y sin foco    -> Arrow (el proximo click solo va a enfocar)
		//   resto                        -> el cursor del gesto
		// En ChangeOnClick NO depende del foco: ahi el gesto funciona igual sin el, y
		// mostrar Arrow seria mentir.
		private void UpdateCursors()
		{
			bool gestures = !IsReadOnly &&
				(ValueChangeMode == ValueChangeMode.ChangeOnClick || IsKeyboardFocused);

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
			!IsReadOnly &&
			(ValueChangeMode == ValueChangeMode.ChangeOnClick || _hadFocusOnPress);

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

		// --- Medición de texto ---
	}
}
