using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace NumericSelector
{
	// La clase es 'partial' y pertenece al mismo namespace.
	public partial class BoundedNumericSelector : Control
	{
		// --- Constructor Estático (si es necesario) ---
		// Este constructor estático se ejecuta una sola vez cuando la clase es cargada por primera vez.
		// Es el lugar ideal para registrar el estilo por defecto.
		static BoundedNumericSelector()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(typeof(BoundedNumericSelector)));

			// En IsDisplayOnly el control no debe poder recibir el foco. Se hace por
			// COERCIÓN y no asignando Focusable: así el valor de abajo (el del estilo, o el
			// que haya puesto el consumidor) queda intacto y vuelve solo al salir del modo.
			// Asignarlo obligaría a recordar a qué valor volver, y pisaría a quien tuviera
			// sus propias razones para dejarlo en false.
			FocusableProperty.OverrideMetadata(typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(true, null, CoerceFocusable));
		}

		private static object CoerceFocusable(DependencyObject d, object baseValue) =>
			((BoundedNumericSelector)d).IsDisplayOnly ? false : baseValue;

		// --- Evento Ruteado ValueChanged ---
		// Equivalente moderno del evento Change del control original en VB6.
		public static readonly RoutedEvent ValueChangedEvent =
			EventManager.RegisterRoutedEvent(
				nameof(ValueChanged),
				RoutingStrategy.Bubble,
				typeof(RoutedPropertyChangedEventHandler<int>),
				typeof(BoundedNumericSelector));

		/// <summary>
		/// Se produce cuando cambia el valor del selector.
		/// </summary>
		public event RoutedPropertyChangedEventHandler<int> ValueChanged
		{
			add => AddHandler(ValueChangedEvent, value);
			remove => RemoveHandler(ValueChangedEvent, value);
		}

		private void RaiseValueChanged(int oldValue, int newValue)
		{
			RaiseEvent(new RoutedPropertyChangedEventArgs<int>(oldValue, newValue, ValueChangedEvent));
		}

		// --- Definiciones de Propiedades de Dependencia ---

		// Nota: no hay propiedad para el ancho de la columna del valor. Ese ancho se calcula
		// siempre, a partir de los medidores ocultos de la plantilla (el número más largo del
		// rango). Una propiedad que lo pisara sólo podría empeorarlo: si se quedaba corta,
		// cortaba el número. El colapso de la columna cuando el valor no va en el casillero
		// lo resuelven los triggers de ValuePlacement en Generic.xaml.

		// Propiedad para el título (fila superior).
		public static readonly DependencyProperty TitleTextProperty =
			DependencyProperty.Register(
				nameof(TitleText),
				typeof(string),
				typeof(BoundedNumericSelector),
				new PropertyMetadata("Default Title Text")); // Valor por defecto.

		/// <summary>
		/// Obtiene o establece el título que se muestra en la fila superior (si ShowTitleText está activo).
		/// </summary>
		public string TitleText
		{
			get => (string)GetValue(TitleTextProperty);
			set => SetValue(TitleTextProperty, value);
		}

		// Propiedad para la leyenda del control (se dibuja sobre la barra).
		public static readonly DependencyProperty LegendTextProperty =
			DependencyProperty.Register(
				nameof(LegendText),
				typeof(string),
				typeof(BoundedNumericSelector),
				new PropertyMetadata("Legend")); // Valor por defecto.

		/// <summary>
		/// Obtiene o establece la leyenda que describe al control. Hoy se dibuja sobre la barra,
		/// aprovechando el relleno como fondo (cuidar el contraste de colores).
		/// </summary>
		public string LegendText
		{
			get => (string)GetValue(LegendTextProperty);
			set => SetValue(LegendTextProperty, value);
		}

		// Propiedad para el valor numérico del selector.
		// Es 'int' porque el control es de entrada numérica discreta (herencia del LNSlider VB6).
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(
				nameof(Value),
				typeof(int),
				typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(
					0, // Valor por defecto
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, // Para TwoWay binding
					OnValueChangedCallback, // Callback estático cuando el valor cambia
					CoerceIntoRange, // Acota el valor al rango [Minimum, Maximum]
					false // isAnimationProhibited
				));

		/// <summary>
		/// Obtiene o establece el valor actual del selector.
		/// </summary>
		public int Value
		{
			get => (int)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value); // CoerceValueCallback se encarga de acotar
		}

		// Propiedad para el valor mínimo. Actúa como ancla del rango: Maximum se coacciona
		// para no quedar por debajo suyo (así Minimum nunca puede superar a Maximum).
		public static readonly DependencyProperty MinimumProperty =
			DependencyProperty.Register(
				nameof(Minimum),
				typeof(int),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(0, OnMinimumChanged, CoerceMinimum)); // Valor por defecto

		/// <summary>
		/// Obtiene o establece el valor mínimo permitido.
		/// </summary>
		public int Minimum
		{
			get => (int)GetValue(MinimumProperty);
			set => SetValue(MinimumProperty, value);
		}

		// Propiedad para el valor máximo. Se coacciona a >= Minimum.
		public static readonly DependencyProperty MaximumProperty =
			DependencyProperty.Register(
				nameof(Maximum),
				typeof(int),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(100, OnMaximumChanged, CoerceMaximum)); // Valor por defecto

		/// <summary>
		/// Obtiene o establece el valor máximo permitido.
		/// </summary>
		public int Maximum
		{
			get => (int)GetValue(MaximumProperty);
			set => SetValue(MaximumProperty, value);
		}

		// Propiedad para el cambio pequeño (ej: por flechas o rueda del ratón).
		public static readonly DependencyProperty SmallChangeProperty =
			DependencyProperty.Register(
				nameof(SmallChange),
				typeof(int),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(1, null, CoerceStep)); // Valor por defecto; nunca < 1

		/// <summary>
		/// Obtiene o establece la cantidad que se incrementa o decrementa el valor con un cambio pequeño.
		/// Se coacciona a un mínimo de 1 (un paso de 0 dejaría el control inerte).
		/// </summary>
		public int SmallChange
		{
			get => (int)GetValue(SmallChangeProperty);
			set => SetValue(SmallChangeProperty, value);
		}

		// Propiedad para el cambio grande (ej: por PageUp/PageDown).
		public static readonly DependencyProperty LargeChangeProperty =
			DependencyProperty.Register(
				nameof(LargeChange),
				typeof(int),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(10, null, CoerceStep)); // Valor por defecto; nunca < 1

		/// <summary>
		/// Obtiene o establece la cantidad que se incrementa o decrementa el valor con un cambio grande.
		/// Se coacciona a un mínimo de 1.
		/// </summary>
		public int LargeChange
		{
			get => (int)GetValue(LargeChangeProperty);
			set => SetValue(LargeChangeProperty, value);
		}

		// Propiedad para el valor de reseteo.
		public static readonly DependencyProperty ResetValueProperty =
			DependencyProperty.Register(
				nameof(ResetValue),
				typeof(int),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(50, null, CoerceIntoRange)); // Valor por defecto; acotado al rango

		/// <summary>
		/// Obtiene o establece el valor al que se resetea (doble-click en el número, Delete o click derecho al centro).
		/// </summary>
		public int ResetValue
		{
			get => (int)GetValue(ResetValueProperty);
			set => SetValue(ResetValueProperty, value);
		}

		// Propiedad para el color del borde del control.
		public static readonly DependencyProperty ControlBorderColorProperty =
			DependencyProperty.Register(
				nameof(ControlBorderColor),
				typeof(Brush),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(Brushes.Black));

		/// <summary>
		/// Obtiene o establece el color del borde del control.
		/// </summary>
		public Brush ControlBorderColor
		{
			get => (Brush)GetValue(ControlBorderColorProperty);
			set => SetValue(ControlBorderColorProperty, value);
		}

		// Propiedad para el color del borde cuando el control tiene el foco.
		public static readonly DependencyProperty FocusBorderColorProperty =
			DependencyProperty.Register(
				nameof(FocusBorderColor),
				typeof(Brush),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(Brushes.DodgerBlue));

		/// <summary>
		/// Obtiene o establece el color que toman los marcos de ambas secciones (título y
		/// datos) cuando el control tiene el foco.
		/// </summary>
		public Brush FocusBorderColor
		{
			get => (Brush)GetValue(FocusBorderColorProperty);
			set => SetValue(FocusBorderColorProperty, value);
		}

		// Propiedad para el grosor del borde del control en píxeles.
		public static readonly DependencyProperty ControlBorderPixelsProperty =
			DependencyProperty.Register(
				nameof(ControlBorderPixels),
				typeof(Thickness), // Usar Thickness para el grosor del borde
				typeof(BoundedNumericSelector),
				new PropertyMetadata(new Thickness(1), OnLayoutPropertyChanged)); // Asociar callback para redibujar si cambia.

		/// <summary>
		/// Obtiene o establece el grosor del borde del control.
		/// </summary>
		public Thickness ControlBorderPixels
		{
			get => (Thickness)GetValue(ControlBorderPixelsProperty);
			set => SetValue(ControlBorderPixelsProperty, value);
		}

		// Propiedad para el color de relleno de la barra.
		public static readonly DependencyProperty BarFillColorProperty =
			DependencyProperty.Register(
				nameof(BarFillColor),
				typeof(Brush),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(Brushes.Orange));

		/// <summary>
		/// Obtiene o establece el color de relleno de la barra (la porción que representa el valor).
		/// </summary>
		public Brush BarFillColor
		{
			get => (Brush)GetValue(BarFillColorProperty);
			set => SetValue(BarFillColorProperty, value);
		}

		// Propiedad para el color del contorno de la barra.
		public static readonly DependencyProperty BarBorderColorProperty =
			DependencyProperty.Register(
				nameof(BarBorderColor),
				typeof(Brush),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(Brushes.Black));

		/// <summary>
		/// Obtiene o establece el color del contorno de la barra.
		/// </summary>
		public Brush BarBorderColor
		{
			get => (Brush)GetValue(BarBorderColorProperty);
			set => SetValue(BarBorderColorProperty, value);
		}

		// Propiedad para el color del texto sobre el relleno (parte "encendida" de la barra).
		public static readonly DependencyProperty FillForegroundProperty =
			DependencyProperty.Register(
				nameof(FillForeground),
				typeof(Brush),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(Brushes.White));

		/// <summary>
		/// Obtiene o establece el color del número cuando queda sobre el relleno de la barra
		/// (modo OnBar). Sobre el fondo sin relleno se usa Foreground.
		/// </summary>
		public Brush FillForeground
		{
			get => (Brush)GetValue(FillForegroundProperty);
			set => SetValue(FillForegroundProperty, value);
		}

		// Propiedad para habilitar/deshabilitar el modo de dos filas (TitleText).
		public static readonly DependencyProperty ShowTitleTextProperty =
			DependencyProperty.Register(
				nameof(ShowTitleText),
				typeof(bool),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(false, OnShowTitleTextChanged)); // Coacciona ValuePlacement + actualiza layout

		/// <summary>
		/// Obtiene o establece un valor que indica si el control debe mostrar la fila
		/// superior con el título.
		/// </summary>
		public bool ShowTitleText
		{
			get => (bool)GetValue(ShowTitleTextProperty);
			set => SetValue(ShowTitleTextProperty, value);
		}

		// Propiedad para la ubicación del número (Value) dentro del control.
		public static readonly DependencyProperty ValuePlacementProperty =
			DependencyProperty.Register(
				nameof(ValuePlacement),
				typeof(ValuePlacement),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(
					ValuePlacement.BesideBar, // Valor por defecto (comportamiento clásico)
					OnLayoutPropertyChanged,    // al cambiar, re-evaluar el layout
					CoerceValuePlacement));     // WithTitle requiere ShowTitleText

		/// <summary>
		/// Obtiene o establece dónde se muestra el número: casillero a la derecha (BesideBar),
		/// sobre la barra (OnBar) o en la línea superior junto al título (WithTitle).
		/// WithTitle requiere ShowTitleText; si no, se degrada a BesideBar.
		/// </summary>
		public ValuePlacement ValuePlacement
		{
			get => (ValuePlacement)GetValue(ValuePlacementProperty);
			set => SetValue(ValuePlacementProperty, value);
		}

		// Propiedad para el comportamiento del ancho frente al contenido.
		public static readonly DependencyProperty StretchModeProperty =
			DependencyProperty.Register(
				nameof(StretchMode),
				typeof(StretchMode),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(StretchMode.Fixed, OnStretchModeChanged));

		/// <summary>
		/// Obtiene o establece si el control mantiene un ancho fijo (y recorta el texto que
		/// no entra) o si se ensancha para acomodar el contenido sin volver a achicarse.
		/// </summary>
		public StretchMode StretchMode
		{
			get => (StretchMode)GetValue(StretchModeProperty);
			set => SetValue(StretchModeProperty, value);
		}

		// Propiedad para exigir (o no) el foco antes de que el mouse cambie el valor.
		public static readonly DependencyProperty ValueChangeModeProperty =
			DependencyProperty.Register(
				nameof(ValueChangeMode),
				typeof(ValueChangeMode),
				typeof(BoundedNumericSelector),
				// Default ChangeOnClick: es el comportamiento que el control ya tenía.
				new PropertyMetadata(ValueChangeMode.ChangeOnClick));

		/// <summary>
		/// Obtiene o establece si los gestos de mouse actúan siempre (ChangeOnClick) o si
		/// exigen que el control tenga el foco (MustFocusFirst), en cuyo caso el click que
		/// le da el foco sólo enfoca.
		/// </summary>
		public ValueChangeMode ValueChangeMode
		{
			get => (ValueChangeMode)GetValue(ValueChangeModeProperty);
			set => SetValue(ValueChangeModeProperty, value);
		}

		// Propiedad para el modo "sólo visualización".
		public static readonly DependencyProperty IsDisplayOnlyProperty =
			DependencyProperty.Register(
				nameof(IsDisplayOnly),
				typeof(bool),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(false, OnIsDisplayOnlyChanged));

		/// <summary>
		/// Obtiene o establece el modo de sólo visualización: el control conserva todo su
		/// aspecto y sigue reflejando los cambios que reciba por sus propiedades, pero no
		/// responde al mouse ni al teclado y no puede recibir el foco.
		/// Se distingue de IsEnabled=false en que no altera la apariencia. Y de
		/// TextBox.IsReadOnly (que sí deja enfocar) en que acá el control queda fuera del
		/// recorrido de tabulación.
		/// Bloquea al usuario, no al programa: asignar Value por código sigue funcionando
		/// y sigue disparando ValueChanged.
		/// </summary>
		public bool IsDisplayOnly
		{
			get => (bool)GetValue(IsDisplayOnlyProperty);
			set => SetValue(IsDisplayOnlyProperty, value);
		}

		private static void OnIsDisplayOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var selector = (BoundedNumericSelector)d;

			// Re-evaluar Focusable con el modo nuevo (ver CoerceFocusable).
			selector.CoerceValue(FocusableProperty);

			// Focusable=false NO suelta el foco que ya estuviera puesto: verificado, el
			// control quedaba con IsKeyboardFocused=true (borde de foco encendido y, peor,
			// la rueda habilitada, porque la rueda mira justamente esa propiedad).
			if (selector.IsDisplayOnly)
				selector.ReleaseKeyboardFocusIfHeld();
		}

		// --- Propiedades de Fuente ---
		// FontFamily, FontStyle, FontWeight y FontSize se heredan de Control; no se
		// redeclaran para no ocultar los miembros del framework (advertencia CS0108).
		// El XAML las consume con TemplateBinding y Control ya las propaga a la plantilla.

		// --- Callbacks Estáticos de Propiedades de Dependencia ---

		// Callback para cambios en el valor. Se invoca cuando la propiedad 'Value' cambia.
		private static void OnValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			// El valor ya llega acotado por CoerceValueCallback; aquí refrescamos la
			// parte visual y notificamos el cambio con el evento ValueChanged.
			if (d is BoundedNumericSelector selector)
			{
				int newValue = (int)e.NewValue;
				selector.OnValueChangedHandler(newValue);
				selector.RaiseValueChanged((int)e.OldValue, newValue);
			}
		}

		// Acota al rango [Minimum, Maximum] el valor propuesto. La usan tanto Value como
		// ResetValue. Al ser coerción, WPF re-evalúa siempre desde el valor *base*: si el
		// rango se angosta el valor se muestra acotado, y si luego se ensancha vuelve a
		// aplicarse el que el usuario había asignado (no se pierde su intención).
		private static object CoerceIntoRange(DependencyObject d, object baseValue)
		{
			if (d is BoundedNumericSelector selector)
			{
				// El Math.Max es defensivo: Math.Clamp lanza ArgumentException si min > max,
				// y aunque la coerción de Maximum ya impide ese estado, no queremos que un
				// orden de inicialización inesperado pueda tirar la aplicación.
				int min = selector.Minimum;
				int max = Math.Max(min, selector.Maximum);
				return Math.Clamp((int)baseValue, min, max);
			}
			return baseValue;
		}

		// El rango tiene que tener al menos 1 de ancho: con Minimum == Maximum el control
		// queda inservible (la barra nunca se llena, el valor no se puede mover y los pasos
		// se quedan sin tope contra el cual acotarse).
		// La restricción es MUTUA y simétrica: cada extremo se frena a un paso del otro y
		// NO lo arrastra. Como la coerción re-evalúa desde el valor base, si después se
		// separa el otro extremo, éste recupera el valor que se le había pedido (por eso
		// funciona el caso XAML `Minimum="200" Maximum="300"` con Maximum aún en 100).
		private static object CoerceMaximum(DependencyObject d, object baseValue)
		{
			long v = (int)baseValue;
			if (d is BoundedNumericSelector selector)
				v = Math.Max(v, (long)selector.Minimum + 1);
			return (int)Math.Clamp(v, int.MinValue + 1, int.MaxValue);
		}

		private static object CoerceMinimum(DependencyObject d, object baseValue)
		{
			long v = (int)baseValue;
			if (d is BoundedNumericSelector selector)
				v = Math.Min(v, (long)selector.Maximum - 1);
			return (int)Math.Clamp(v, int.MinValue, int.MaxValue - 1);
		}

		// Los pasos (SmallChange/LargeChange) van de 1 hasta el ancho del rango: un paso
		// mayor que el rango completo no aporta nada (salta de un extremo al otro igual que
		// el ancho del rango) y dejaría la propiedad exhibiendo un número imposible.
		private static object CoerceStep(DependencyObject d, object baseValue)
		{
			int step = Math.Max((int)baseValue, 1);

			if (d is BoundedNumericSelector selector)
			{
				long span = (long)selector.Maximum - selector.Minimum;
				if (span >= 1) step = (int)Math.Min(step, span);
			}

			return step;
		}

		// El crecimiento lo hace MeasureOverride asignando Width. Acá sólo se recuerda el
		// ancho previo para restituirlo al volver a Fixed (nunca se restituye NaN: escribir
		// NaN rompería la conversión de un binding sobre Width).
		private static void OnStretchModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not BoundedNumericSelector selector) return;

			if ((StretchMode)e.NewValue == StretchMode.AutoGrow)
			{
				selector._widthBeforeGrow = selector.Width;
			}
			else if (!double.IsNaN(selector._widthBeforeGrow))
			{
				selector.SetCurrentValue(WidthProperty, selector._widthBeforeGrow);
			}

			selector.InvalidateMeasure();
		}

		// Al cambiar Minimum hay que re-evaluar Maximum (que se apoya en él) y luego Value.
		private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is BoundedNumericSelector selector)
			{
				selector.CoerceValue(MaximumProperty);
				selector.RefreshAfterRangeChange();
			}
		}

		private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is BoundedNumericSelector selector)
			{
				// Simétrico a OnMinimumChanged: al separarse Maximum, Minimum puede recuperar
				// el valor base que se le había pedido y que había quedado topeado.
				selector.CoerceValue(MinimumProperty);
				selector.RefreshAfterRangeChange();
			}
		}

		// Re-evalúa el valor contra el rango nuevo y refresca la parte visual.
		private void RefreshAfterRangeChange()
		{
			// La coerción dispara OnValueChangedCallback solo si el valor efectivamente cambia.
			CoerceValue(ValueProperty);

			// ResetValue también vive dentro del rango, así que hay que re-evaluarlo.
			CoerceValue(ResetValueProperty);

			// Y los pasos, que se acotan al ancho del rango.
			CoerceValue(SmallChangeProperty);
			CoerceValue(LargeChangeProperty);

			// Y esto cubre el caso en que el rango cambió sin alterar el valor: la proporción
			// de la barra cambia igual, así que hay que redibujarla.
			OnValueChangedHandler(Value);
		}

		// Los colores NO necesitan callback de redibujo: los tres (ControlBorderColor,
		// BarFillColor y BarBorderColor) llegan a la plantilla por TemplateBinding, y cada
		// elemento se repinta solo cuando su brocha cambia. El InvalidateVisual() que había
		// acá forzaba el redibujo del control, que al ser lookless no dibuja nada propio
		// (no sobrescribe OnRender): era trabajo sin destinatario.

		// Callback para propiedades que afectan el layout.
		private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is BoundedNumericSelector selector)
			{
				// Forzar una reevaluación del layout.
				selector.InvalidateMeasure(); // Indica que la medida del control puede haber cambiado.
				selector.InvalidateArrange(); // Indica que el arrangement puede haber cambiado.
			}
		}

		// Coacciona ValuePlacement: WithTitle solo es válido con ShowTitleText; si no,
		// se degrada a BesideBar. La coerción recuerda el WithTitle "base" y lo restaura
		// cuando ShowTitleText vuelve a activarse (comportamiento A: recuerda).
		private static object CoerceValuePlacement(DependencyObject d, object baseValue)
		{
			if (d is BoundedNumericSelector selector &&
				(ValuePlacement)baseValue == ValuePlacement.WithTitle &&
				!selector.ShowTitleText)
			{
				return ValuePlacement.BesideBar;
			}
			return baseValue;
		}

		// Callback de ShowTitleText: re-coacciona ValuePlacement (para forzar/restaurar
		// WithTitle según corresponda) y luego actualiza el layout.
		private static void OnShowTitleTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is BoundedNumericSelector selector)
			{
				selector.CoerceValue(ValuePlacementProperty);
				selector.InvalidateMeasure();
				selector.InvalidateArrange();
			}
		}
	}
}
