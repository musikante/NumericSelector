using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NumericSelector
{
	// La clase es 'partial' y pertenece al mismo namespace.
	public partial class BoundedNumericSelector : Control
	{
		// --- Constructor Estático ---
		// Se ejecuta una sola vez cuando la clase es cargada; registra el estilo por defecto.
		static BoundedNumericSelector()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(typeof(BoundedNumericSelector)));

			// En InteractionMode=ReadOnly el control no debe poder recibir el foco. Se hace
			// por COERCIÓN y no asignando Focusable: así el valor de abajo (el del estilo, o
			// el que haya puesto el consumidor) queda intacto y vuelve solo al salir del modo.
			// Asignarlo obligaría a recordar a qué valor volver, y pisaría a quien tuviera
			// sus propias razones para dejarlo en false.
			FocusableProperty.OverrideMetadata(typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(true, null, CoerceFocusable));

			// El marco es parte del aspecto de este control, así que sus dos propiedades
			// heredadas de Control estrenan defaults visibles: en Control valen null y 0
			// (un control sin marco), que acá dejaría el dibujo abierto. Se cambia sólo el
			// valor por defecto; los flags de la metadata base (AffectsMeasure en
			// BorderThickness) se conservan porque OverrideMetadata los fusiona.
			BorderBrushProperty.OverrideMetadata(typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(Brushes.Black));
			BorderThicknessProperty.OverrideMetadata(typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(new Thickness(1)));
		}

		private static object CoerceFocusable(DependencyObject d, object baseValue) =>
			((BoundedNumericSelector)d).InteractionMode == UserInteractionMode.ReadOnly ? false : baseValue;

		// --- Evento Ruteado ValueChanged ---
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
		// cortaba el número. El ocultamiento de la caja cuando el valor desciende a la fila
		// de detalle lo resuelven los triggers de Generic.xaml.

		// Propiedad para el ANCHO BASE: el ancho desde el que el control crece para acomodar
		// su contenido. NO es un constraint duro tipo WPF (Width/MinWidth): no fuerza el
		// tamaño del elemento ni puede hacerlo desbordar. Se lee como piso en MeasureOverride:
		// el control pide max(BaseWidth, contenido) pero nunca más ancho que el hueco que le
		// da el panel, por lo que el marco (los bordes) jamás se recorta y la CharacterEllipsis
		// de la leyenda hace el resto en el caso estrecho. NaN significa "automático": el piso
		// es el ancho natural del contenido.
		public static readonly DependencyProperty BaseWidthProperty =
			DependencyProperty.Register(
				nameof(BaseWidth),
				typeof(double),
				typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(double.NaN,
					FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		/// Obtiene o establece el ancho base desde el que el control crece para acomodar su
		/// contenido. No es un ancho fijo: el control crece hacia arriba cuanto le haga
		/// falta y, si el contenedor no le da ese hueco, la leyenda se recorta con elipsis en
		/// lugar de desbordar los bordes. NaN (por defecto) equivale a "automático".
		/// </summary>
		public double BaseWidth
		{
			get => (double)GetValue(BaseWidthProperty);
			set => SetValue(BaseWidthProperty, value);
		}

		// Propiedad para la CAPTION: el texto identificador del control, que se dibuja sobre
		// la barra y es el rótulo permanente (como la etiqueta de un ComboBox).
		// El texto de relleno lo fija esta metadata y NADIE MÁS: el Style de Generic.xaml no
		// lo repite. Un Setter en el Style le ganaría a la metadata, y entonces el control
		// perdería su default al reemplazarse la plantilla —que es justamente lo que un
		// control lookless espera que su consumidor haga—.
		public static readonly DependencyProperty CaptionTextProperty =
			DependencyProperty.Register(
				nameof(CaptionText),
				typeof(string),
				typeof(BoundedNumericSelector),
				new PropertyMetadata("Default Caption"));

		/// <summary>
		/// Obtiene o establece el texto permanente que identifica al control. Se dibuja sobre
		/// la barra, aprovechando el relleno como fondo (cuidar el contraste de colores).
		/// </summary>
		public string CaptionText
		{
			get => (string)GetValue(CaptionTextProperty);
			set => SetValue(CaptionTextProperty, value);
		}

		// Propiedad para la fila de DETALLE (desplazable debajo de la barra).
		public static readonly DependencyProperty DetailTextProperty =
			DependencyProperty.Register(
				nameof(DetailText),
				typeof(string),
				typeof(BoundedNumericSelector),
				new PropertyMetadata("Default Detail")); // Única fuente, igual que CaptionText.

		/// <summary>
		/// Obtiene o establece el texto de la fila de detalle, que se despliega debajo de la
		/// barra cuando <see cref="ShowDetail"/> es true. Su propósito usual es dar salida en
		/// texto al índice de <see cref="Value"/> (p. ej. el elemento elegido), aunque puede
		/// usarse como encabezado fijo.
		/// </summary>
		public string DetailText
		{
			get => (string)GetValue(DetailTextProperty);
			set => SetValue(DetailTextProperty, value);
		}

		// Propiedad para el valor numérico del selector.
		// Es 'int' porque el control es de entrada numérica discreta.
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

		// El marco del control usa `Control.BorderBrush` y `Control.BorderThickness`, las
		// heredadas de WPF: acá NO se declaran propias. Declararlas habría ocultado a las
		// heredadas (CS0108) y, peor, un `TemplateBinding BorderBrush` de la plantilla se
		// habría enlazado igual a la de `Control`, dejando la propia sin efecto. Lo único
		// que hace falta es cambiarles el valor por defecto —en `Control` son `null` y `0`,
		// o sea un control sin marco—, y eso va por OverrideMetadata (constructor estático).

		// Propiedad para la brocha de los marcos cuando el control tiene el foco.
		public static readonly DependencyProperty FocusBorderBrushProperty =
			DependencyProperty.Register(
				nameof(FocusBorderBrush),
				typeof(Brush),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(Brushes.DodgerBlue));

		/// <summary>
		/// Obtiene o establece la brocha que toman los marcos de todas las celdas (barra,
		/// casillero del valor y detalle) cuando el control tiene el foco.
		/// </summary>
		public Brush FocusBorderBrush
		{
			get => (Brush)GetValue(FocusBorderBrushProperty);
			set => SetValue(FocusBorderBrushProperty, value);
		}

		// Propiedad para el relleno de la barra. Se llama `BarFill` y no `BarFillBrush` por
		// la misma convención que `Shape.Fill`: cuando la propiedad ES el relleno, el tipo
		// no necesita repetirse en el nombre.
		public static readonly DependencyProperty BarFillProperty =
			DependencyProperty.Register(
				nameof(BarFill),
				typeof(Brush),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(Brushes.Orange));

		/// <summary>
		/// Obtiene o establece la brocha de relleno de la barra (la porción que representa
		/// el valor). Al ser un <see cref="Brush"/> admite degradados, imágenes o cualquier
		/// otra brocha, no sólo un color liso.
		/// </summary>
		public Brush BarFill
		{
			get => (Brush)GetValue(BarFillProperty);
			set => SetValue(BarFillProperty, value);
		}

		// Propiedad para el divisor de la barra: el trazo vertical de 1px en el borde
		// derecho del relleno, que separa visualmente la porción llena de la vacía.
		public static readonly DependencyProperty BarDividerBrushProperty =
			DependencyProperty.Register(
				nameof(BarDividerBrush),
				typeof(Brush),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(Brushes.Black));

		/// <summary>
		/// Obtiene o establece la brocha del divisor de la barra (el trazo que separa la
		/// porción rellena de la vacía).
		/// </summary>
		public Brush BarDividerBrush
		{
			get => (Brush)GetValue(BarDividerBrushProperty);
			set => SetValue(BarDividerBrushProperty, value);
		}

// Propiedad para decidir si existe la fila de detalle debajo de la barra.
		// Es un bool: la fila de detalle, si se muestra, siempre va enmarcada; no hay una
		// forma "suelta" que representar con un tercer valor.
		public static readonly DependencyProperty ShowDetailProperty =
			DependencyProperty.Register(
				nameof(ShowDetail),
				typeof(bool),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(false, OnLayoutPropertyChanged)); // default: sin detalle

		/// <summary>
		/// Obtiene o establece si se muestra la fila de detalle enmarcada debajo de la barra.
		/// </summary>
		public bool ShowDetail
		{
			get => (bool)GetValue(ShowDetailProperty);
			set => SetValue(ShowDetailProperty, value);
		}

		// Propiedad para decidir si la caja del valor desciende a la fila de detalle cuando
		// existe. Con ShowDetail=true el casillero del valor se mueve abajo, junto al texto de
		// detalle; con false queda en la fila de la barra (junto a la Caption).
		// Es un bool (y no un enum) porque no hay estado inválido: sin fila de detalle el
		// valor queda arriba por construcción, sin coerción ni "degradación".
		public static readonly DependencyProperty ValueFollowsDetailProperty =
			DependencyProperty.Register(
				nameof(ValueFollowsDetail),
				typeof(bool),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(true, OnLayoutPropertyChanged)); // default: sigue al detalle

		/// <summary>
		/// Obtiene o establece si el casillero del valor desciende a la fila del detalle
		/// cuando existe (<see cref="ShowDetail"/>). Con false el valor se queda junto a la
		/// barra (junto a la Caption).
		/// </summary>
		public bool ValueFollowsDetail
		{
			get => (bool)GetValue(ValueFollowsDetailProperty);
			set => SetValue(ValueFollowsDetailProperty, value);
		}

		// Propiedad para el lado (anclaje) de la caja del valor respecto de la caja con la
		// que comparte fila (la barra arriba, o el detalle abajo).
		public static readonly DependencyProperty ValueBoxDockProperty =
			DependencyProperty.Register(
				nameof(ValueBoxDock),
				typeof(ValueBoxDock),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(ValueBoxDock.Right, OnLayoutPropertyChanged)); // clásico: valor a la derecha

		/// <summary>
		/// Obtiene o establece el lado (anclaje) de la caja del valor respecto de su
		/// compañero de fila: a la derecha (predeterminado) o a la izquierda.
		/// </summary>
		public ValueBoxDock ValueBoxDock
		{
			get => (ValueBoxDock)GetValue(ValueBoxDockProperty);
			set => SetValue(ValueBoxDockProperty, value);
		}

		// Propiedad para exigir (o no) el foco antes de que el mouse cambie el valor.
		public static readonly DependencyProperty MouseBehaviorProperty =
			DependencyProperty.Register(
				nameof(MouseBehavior),
				typeof(MouseInteractionBehavior),
				typeof(BoundedNumericSelector),
				// Default ChangeOnClick: es el comportamiento que el control ya tenía.
				new PropertyMetadata(MouseInteractionBehavior.ChangeOnClick));

		/// <summary>
		/// Obtiene o establece si los gestos de mouse actúan siempre (ChangeOnClick) o si
		/// exigen que el control tenga el foco (MustFocusFirst), en cuyo caso el click que
		/// le da el foco sólo enfoca.
		/// </summary>
		public MouseInteractionBehavior MouseBehavior
		{
			get => (MouseInteractionBehavior)GetValue(MouseBehaviorProperty);
			set => SetValue(MouseBehaviorProperty, value);
		}

		// Propiedad para el modo de interacción (interactivo o sólo visualización).
		public static readonly DependencyProperty InteractionModeProperty =
			DependencyProperty.Register(
				nameof(InteractionMode),
				typeof(UserInteractionMode),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(UserInteractionMode.Interactive, OnInteractionModeChanged));

		/// <summary>
		/// Obtiene o establece el modo de interacción del control. Con
		/// <see cref="UserInteractionMode.Interactive"/> (predeterminado) responde al mouse
		/// y al teclado como siempre; con <see cref="UserInteractionMode.ReadOnly"/> conserva
		/// todo su aspecto y sigue reflejando los cambios que reciba por sus propiedades,
		/// pero no responde al mouse ni al teclado y no puede recibir el foco.
		/// Se distingue de IsEnabled=false en que no altera la apariencia. Y de
		/// TextBox.IsReadOnly (que sí deja enfocar) en que acá el control queda fuera del
		/// recorrido de tabulación.
		/// Bloquea al usuario, no al programa: asignar Value por código sigue funcionando
		/// y sigue disparando ValueChanged.
		/// </summary>
		public UserInteractionMode InteractionMode
		{
			get => (UserInteractionMode)GetValue(InteractionModeProperty);
			set => SetValue(InteractionModeProperty, value);
		}

		private static void OnInteractionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var selector = (BoundedNumericSelector)d;

			// Re-evaluar Focusable con el modo nuevo (ver CoerceFocusable).
			selector.CoerceValue(FocusableProperty);

			// Focusable=false NO suelta el foco que ya estuviera puesto: verificado, el
			// control quedaba con IsKeyboardFocused=true (borde de foco encendido y, peor,
			// la rueda habilitada, porque la rueda mira justamente esa propiedad).
			if (selector.InteractionMode == UserInteractionMode.ReadOnly)
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

		// Las brochas NO necesitan callback de redibujo: las tres (BorderBrush,
		// BarFill y BarDividerBrush) llegan a la plantilla por TemplateBinding, y cada
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

	}
}
