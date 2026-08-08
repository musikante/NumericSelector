using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NumericSelector
{
	// The class is 'partial' and belongs to the same namespace. This half holds the public
	// API: the dependency properties, their coercions and the ValueChanged event.
	public partial class BoundedNumericSelector : Control
	{
		// --- Static constructor ---
		// It runs only once, when the class is loaded; it registers the default style.
		static BoundedNumericSelector()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(typeof(BoundedNumericSelector)));

			// In InteractionMode=ReadOnly the control must not be able to take the focus. It is
			// done by COERCION and not by assigning Focusable: that way the value underneath
			// (the one from the style, or the one the consumer set) stays untouched and comes
			// back on its own when the mode is left. Assigning it would force us to remember
			// which value to return to, and would override anyone who had their own reasons to
			// leave it false.
			FocusableProperty.OverrideMetadata(typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(true, null, CoerceFocusable));

			// The frame is part of this control's appearance, so its two properties inherited
			// from Control get visible defaults: in Control they are null and 0 (a control with
			// no frame), which here would leave the drawing open. Only the default value is
			// changed; the flags of the base metadata (AffectsMeasure on BorderThickness) are
			// preserved because OverrideMetadata merges them.
			BorderBrushProperty.OverrideMetadata(typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(Brushes.Black));
			BorderThicknessProperty.OverrideMetadata(typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(new Thickness(1)));
		}

		private static object CoerceFocusable(DependencyObject d, object baseValue) =>
			((BoundedNumericSelector)d).InteractionMode == UserInteractionMode.ReadOnly ? false : baseValue;

		/// <summary>
		/// Validation shared by the three enum properties: a value that is not one of the
		/// declared members is rejected instead of being stored.
		/// </summary>
		/// <remarks>
		/// A cast silences the compiler —<c>(ValueBoxDock)99</c> compiles— and without this the
		/// undefined value went in, no comparison in the template or the code-behind matched it,
		/// and the control drew as if the default had been asked for. Failing at the assignment
		/// points at the line that is wrong; drawing something plausible does not.
		/// It goes in a ValidateValueCallback and not in a coercion because these are not values
		/// to be adjusted to a range, they are values that do not exist: WPF throws an
		/// ArgumentException and the property keeps whatever it had. Being static and unaware of
		/// the instance is fine here — whether a member exists does not depend on the control's
		/// state.
		/// </remarks>
		private static bool IsDefinedEnumValue<TEnum>(object value) where TEnum : struct, Enum
			=> value is TEnum candidate && Enum.IsDefined(candidate);

		// --- ValueChanged routed event ---
		public static readonly RoutedEvent ValueChangedEvent =
			EventManager.RegisterRoutedEvent(
				nameof(ValueChanged),
				RoutingStrategy.Bubble,
				typeof(RoutedPropertyChangedEventHandler<int>),
				typeof(BoundedNumericSelector));

		/// <summary>
		/// Occurs when the value of the selector changes.
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

		// --- Dependency property definitions ---

		// Note: there is no property for the width of the value column. That width is always
		// computed, out of the template's hidden sizers (the longest number in the range). A
		// property overriding it could only make things worse: if it fell short, it cut the
		// number off. Hiding the box when the value drops to the detail row is resolved by the
		// triggers in Generic.xaml.

		// Property for the BASE WIDTH: the width the control grows from to fit its content. It
		// is NOT a hard WPF-style constraint (Width/MinWidth): it neither forces the size of
		// the element nor can make it overflow. It is read as a floor in MeasureOverride: the
		// control asks for max(BaseWidth, content) but never wider than the slot the panel
		// gives it, so the frame (the borders) is never clipped and the CharacterEllipsis of
		// the main text does the rest in the narrow case. NaN means "automatic": the floor is
		// the natural width of the content.
		public static readonly DependencyProperty BaseWidthProperty =
			DependencyProperty.Register(
				nameof(BaseWidth),
				typeof(double),
				typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(double.NaN,
					FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		/// Gets or sets the base width the control grows from to fit its content. It is not a
		/// fixed width: the control grows upwards as much as it needs to and, if the container
		/// does not give it that room, <see cref="MainText"/> is truncated with an ellipsis
		/// instead of overflowing the borders. NaN (the default) means "automatic".
		/// </summary>
		public double BaseWidth
		{
			get => (double)GetValue(BaseWidthProperty);
			set => SetValue(BaseWidthProperty, value);
		}

		// Property for the MAIN TEXT: the permanent label of the control, drawn over the bar
		// and always in sight. It is called MainText —and not CaptionText or HeaderText—
		// because it pairs with DetailText: main line and detail line. "Caption" evoked the
		// subordinate caption of a picture, and "Header" would promise a text ABOVE the bar
		// when it actually goes ON TOP of it.
		// The placeholder text is set by this metadata and BY NOBODY ELSE: the Style in
		// Generic.xaml does not repeat it. A Setter in the Style would win over the metadata,
		// and then the control would lose its default as soon as the template was replaced
		// —which is exactly what a lookless control expects its consumer to do—.
		// And the placeholder is the NAME OF THE PROPERTY, not some "Default Main Text":
		// whoever drops the control in the designer without assigning anything reads right
		// there what the thing they have to write in their XAML is called. It is the same idea
		// the demo follows.
		public static readonly DependencyProperty MainTextProperty =
			DependencyProperty.Register(
				nameof(MainText),
				typeof(string),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(nameof(MainText)));

		/// <summary>
		/// Gets or sets the permanent text that identifies the control. It is drawn over the
		/// bar, using the fill as its background (mind the color contrast).
		/// </summary>
		public string MainText
		{
			get => (string)GetValue(MainTextProperty);
			set => SetValue(MainTextProperty, value);
		}

		// Property for the DETAIL row (which unfolds below the bar).
		public static readonly DependencyProperty DetailTextProperty =
			DependencyProperty.Register(
				nameof(DetailText),
				typeof(string),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(nameof(DetailText))); // Same as MainText: the name.

		/// <summary>
		/// Gets or sets the text of the detail row, which unfolds below the bar when
		/// <see cref="ShowDetail"/> is true. Its usual purpose is to give a textual output for
		/// the index in <see cref="Value"/> (e.g. the chosen item), though it can also be used
		/// as a fixed header.
		/// </summary>
		public string DetailText
		{
			get => (string)GetValue(DetailTextProperty);
			set => SetValue(DetailTextProperty, value);
		}

		// Property for the numeric value of the selector.
		// It is 'int' because the control is for discrete numeric input.
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(
				nameof(Value),
				typeof(int),
				typeof(BoundedNumericSelector),
				new FrameworkPropertyMetadata(
					0, // Default value
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, // For TwoWay binding
					OnValueChangedCallback, // Static callback for when the value changes
					CoerceIntoRange, // Bounds the value to the [Minimum, Maximum] range
					false // isAnimationProhibited
				));

		/// <summary>
		/// Gets or sets the current value of the selector.
		/// </summary>
		public int Value
		{
			get => (int)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value); // CoerceValueCallback takes care of bounding
		}

		// Property for the minimum value. It acts as the anchor of the range: Maximum is
		// coerced so as not to end up below it (that way Minimum can never exceed Maximum).
		public static readonly DependencyProperty MinimumProperty =
			DependencyProperty.Register(
				nameof(Minimum),
				typeof(int),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(0, OnMinimumChanged, CoerceMinimum)); // Default value

		/// <summary>
		/// Gets or sets the lowest allowed value.
		/// </summary>
		public int Minimum
		{
			get => (int)GetValue(MinimumProperty);
			set => SetValue(MinimumProperty, value);
		}

		// Property for the maximum value. It is coerced to >= Minimum.
		public static readonly DependencyProperty MaximumProperty =
			DependencyProperty.Register(
				nameof(Maximum),
				typeof(int),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(100, OnMaximumChanged, CoerceMaximum)); // Default value

		/// <summary>
		/// Gets or sets the highest allowed value.
		/// </summary>
		public int Maximum
		{
			get => (int)GetValue(MaximumProperty);
			set => SetValue(MaximumProperty, value);
		}

		// Property for the small change (e.g. by arrow keys or the mouse wheel).
		public static readonly DependencyProperty SmallChangeProperty =
			DependencyProperty.Register(
				nameof(SmallChange),
				typeof(int),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(1, null, CoerceStep)); // Default value; never < 1

		/// <summary>
		/// Gets or sets the amount the value is increased or decreased by on a small change.
		/// It is coerced to a minimum of 1 (a step of 0 would leave the control inert).
		/// </summary>
		public int SmallChange
		{
			get => (int)GetValue(SmallChangeProperty);
			set => SetValue(SmallChangeProperty, value);
		}

		// Property for the large change (e.g. by PageUp/PageDown).
		public static readonly DependencyProperty LargeChangeProperty =
			DependencyProperty.Register(
				nameof(LargeChange),
				typeof(int),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(10, null, CoerceStep)); // Default value; never < 1

		/// <summary>
		/// Gets or sets the amount the value is increased or decreased by on a large change.
		/// It is coerced to a minimum of 1.
		/// </summary>
		public int LargeChange
		{
			get => (int)GetValue(LargeChangeProperty);
			set => SetValue(LargeChangeProperty, value);
		}

		// Property for the reset value.
		public static readonly DependencyProperty ResetValueProperty =
			DependencyProperty.Register(
				nameof(ResetValue),
				typeof(int),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(50, null, CoerceIntoRange)); // Default value; bounded to the range

		/// <summary>
		/// Gets or sets the value the selector is reset to (double click on the number, Delete,
		/// or a right click on the centre).
		/// </summary>
		public int ResetValue
		{
			get => (int)GetValue(ResetValueProperty);
			set => SetValue(ResetValueProperty, value);
		}

		// The frame of the control uses `Control.BorderBrush` and `Control.BorderThickness`,
		// the ones inherited from WPF: they are NOT redeclared here. Declaring them would have
		// hidden the inherited ones (CS0108) and, worse, a `TemplateBinding BorderBrush` in the
		// template would have bound to `Control`'s one anyway, leaving ours with no effect. All
		// that is needed is changing their default value —in `Control` they are `null` and `0`,
		// that is, a control with no frame— and that goes through OverrideMetadata (the static
		// constructor).

		// Property for the brush of the frames when the control has the focus.
		public static readonly DependencyProperty FocusBorderBrushProperty =
			DependencyProperty.Register(
				nameof(FocusBorderBrush),
				typeof(Brush),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(Brushes.DodgerBlue));

		/// <summary>
		/// Gets or sets the brush taken by the frames of every cell (bar, value box and detail)
		/// when the control has the focus.
		/// </summary>
		public Brush FocusBorderBrush
		{
			get => (Brush)GetValue(FocusBorderBrushProperty);
			set => SetValue(FocusBorderBrushProperty, value);
		}

		// Property for the fill of the bar. It is called `BarFill` and not `BarFillBrush` by
		// the same convention as `Shape.Fill`: when the property IS the fill, the type does not
		// need repeating in the name.
		public static readonly DependencyProperty BarFillProperty =
			DependencyProperty.Register(
				nameof(BarFill),
				typeof(Brush),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(Brushes.Orange));

		/// <summary>
		/// Gets or sets the fill brush of the bar (the portion representing the value). Being a
		/// <see cref="Brush"/> it accepts gradients, images or any other brush, not just a flat
		/// color.
		/// </summary>
		public Brush BarFill
		{
			get => (Brush)GetValue(BarFillProperty);
			set => SetValue(BarFillProperty, value);
		}

		// Property for the bar divider: the 1px vertical stroke on the right edge of the fill,
		// which visually separates the filled portion from the empty one.
		public static readonly DependencyProperty BarDividerBrushProperty =
			DependencyProperty.Register(
				nameof(BarDividerBrush),
				typeof(Brush),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(Brushes.Black));

		/// <summary>
		/// Gets or sets the brush of the bar divider (the stroke separating the filled portion
		/// from the empty one).
		/// </summary>
		public Brush BarDividerBrush
		{
			get => (Brush)GetValue(BarDividerBrushProperty);
			set => SetValue(BarDividerBrushProperty, value);
		}

		// Property deciding whether the detail row below the bar exists at all.
		// It is a bool: the detail row, if shown, always comes framed; there is no "loose"
		// shape to represent with a third value.
		public static readonly DependencyProperty ShowDetailProperty =
			DependencyProperty.Register(
				nameof(ShowDetail),
				typeof(bool),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(false, OnLayoutPropertyChanged)); // default: no detail

		/// <summary>
		/// Gets or sets whether the framed detail row is shown below the bar.
		/// </summary>
		public bool ShowDetail
		{
			get => (bool)GetValue(ShowDetailProperty);
			set => SetValue(ShowDetailProperty, value);
		}

		// Property deciding whether the value box drops to the detail row when that row exists.
		// With ShowDetail=true the value box moves down, next to the detail text; with false it
		// stays in the bar row (next to MainText).
		// It is a bool (and not an enum) because there is no invalid state: with no detail row
		// the value stays up by construction, with no coercion and no "degradation".
		public static readonly DependencyProperty ValueFollowsDetailProperty =
			DependencyProperty.Register(
				nameof(ValueFollowsDetail),
				typeof(bool),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(true, OnLayoutPropertyChanged)); // default: it follows the detail

		/// <summary>
		/// Gets or sets whether the value box drops to the detail row when that row exists
		/// (<see cref="ShowDetail"/>). With false the value stays next to the bar, alongside
		/// <see cref="MainText"/>.
		/// </summary>
		public bool ValueFollowsDetail
		{
			get => (bool)GetValue(ValueFollowsDetailProperty);
			set => SetValue(ValueFollowsDetailProperty, value);
		}

		// Property for the side (dock) of the value box relative to the box it shares its row
		// with (the bar above, or the detail below).
		public static readonly DependencyProperty ValueBoxDockProperty =
			DependencyProperty.Register(
				nameof(ValueBoxDock),
				typeof(ValueBoxDock),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(ValueBoxDock.Right, OnLayoutPropertyChanged), // classic: value on the right
				IsDefinedEnumValue<ValueBoxDock>);

		/// <summary>
		/// Gets or sets the side (dock) of the value box relative to its row partner: on the
		/// right (default) or on the left.
		/// </summary>
		public ValueBoxDock ValueBoxDock
		{
			get => (ValueBoxDock)GetValue(ValueBoxDockProperty);
			set => SetValue(ValueBoxDockProperty, value);
		}

		// Property demanding (or not) the focus before the mouse changes the value.
		public static readonly DependencyProperty MouseBehaviorProperty =
			DependencyProperty.Register(
				nameof(MouseBehavior),
				typeof(MouseInteractionBehavior),
				typeof(BoundedNumericSelector),
				// Default ChangeOnClick: it is the behavior the control already had.
				new PropertyMetadata(MouseInteractionBehavior.ChangeOnClick),
				IsDefinedEnumValue<MouseInteractionBehavior>);

		/// <summary>
		/// Gets or sets whether mouse gestures always act (ChangeOnClick) or demand that the
		/// control has the focus (MustFocusFirst), in which case the click that gives it the
		/// focus only focuses.
		/// </summary>
		public MouseInteractionBehavior MouseBehavior
		{
			get => (MouseInteractionBehavior)GetValue(MouseBehaviorProperty);
			set => SetValue(MouseBehaviorProperty, value);
		}

		// Property for the interaction mode (interactive or display only).
		public static readonly DependencyProperty InteractionModeProperty =
			DependencyProperty.Register(
				nameof(InteractionMode),
				typeof(UserInteractionMode),
				typeof(BoundedNumericSelector),
				new PropertyMetadata(UserInteractionMode.Interactive, OnInteractionModeChanged),
				IsDefinedEnumValue<UserInteractionMode>);

		/// <summary>
		/// Gets or sets the interaction mode of the control. With
		/// <see cref="UserInteractionMode.Interactive"/> (the default) it responds to mouse and
		/// keyboard as usual; with <see cref="UserInteractionMode.ReadOnly"/> it keeps its whole
		/// appearance and still reflects the changes it receives through its properties, but it
		/// does not respond to mouse or keyboard and cannot take the focus.
		/// It differs from IsEnabled=false in that it does not alter the appearance. And from
		/// TextBox.IsReadOnly (which does allow focusing) in that here the control is left out
		/// of the tab order.
		/// It blocks the user, not the program: assigning Value from code keeps working and
		/// keeps raising ValueChanged.
		/// </summary>
		public UserInteractionMode InteractionMode
		{
			get => (UserInteractionMode)GetValue(InteractionModeProperty);
			set => SetValue(InteractionModeProperty, value);
		}

		private static void OnInteractionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var selector = (BoundedNumericSelector)d;

			// Re-evaluate Focusable against the new mode (see CoerceFocusable).
			selector.CoerceValue(FocusableProperty);

			// Focusable=false does NOT release a focus that was already taken: verified, the
			// control was left with IsKeyboardFocused=true (focus border lit and, worse, the
			// wheel enabled, because the wheel looks at exactly that property).
			if (selector.InteractionMode == UserInteractionMode.ReadOnly)
				selector.ReleaseKeyboardFocusIfHeld();
		}

		// --- Font properties ---
		// FontFamily, FontStyle, FontWeight and FontSize are inherited from Control; they are
		// not redeclared so as not to hide the framework members (warning CS0108).
		// The XAML consumes them with TemplateBinding and Control already propagates them to
		// the template.

		// --- Static dependency property callbacks ---

		// Callback for value changes. It is invoked when the 'Value' property changes.
		private static void OnValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			// The value arrives already bounded by CoerceValueCallback; here we refresh the
			// visual part and announce the change with the ValueChanged event.
			if (d is BoundedNumericSelector selector)
			{
				int newValue = (int)e.NewValue;
				selector.OnValueChangedHandler(newValue);
				selector.RaiseValueChanged((int)e.OldValue, newValue);
			}
		}

		// Bounds the proposed value to the [Minimum, Maximum] range. Both Value and ResetValue
		// use it. Being a coercion, WPF always re-evaluates from the *base* value: if the range
		// narrows, the value shows up bounded, and if it widens again the one the user had
		// assigned applies once more (their intent is not lost).
		private static object CoerceIntoRange(DependencyObject d, object baseValue)
		{
			if (d is BoundedNumericSelector selector)
			{
				// The Math.Max is defensive: Math.Clamp throws ArgumentException if min > max,
				// and even though the coercion of Maximum already prevents that state, we do not
				// want an unexpected initialization order to be able to bring the application
				// down.
				int min = selector.Minimum;
				int max = Math.Max(min, selector.Maximum);
				return Math.Clamp((int)baseValue, min, max);
			}
			return baseValue;
		}

		// The range has to be at least 1 wide: with Minimum == Maximum the control is useless
		// (the bar never fills up, the value cannot move and the steps are left with no ceiling
		// to be bounded against).
		// The restriction is MUTUAL and symmetric: each end stops one step short of the other
		// and does NOT drag it along. Since the coercion re-evaluates from the base value, if
		// the other end is separated afterwards, this one recovers the value it had been asked
		// for (that is why the XAML case `Minimum="200" Maximum="300"` works with Maximum still
		// at 100).
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

		// The steps (SmallChange/LargeChange) go from 1 up to the width of the range: a step
		// larger than the whole range adds nothing (it jumps from end to end just like the
		// range width does) and would leave the property displaying an impossible number.
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

		// When Minimum changes, Maximum has to be re-evaluated (it leans on it) and then Value.
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
				// Symmetric to OnMinimumChanged: as Maximum moves away, Minimum may recover the
				// base value it had been asked for and that had been capped.
				selector.CoerceValue(MinimumProperty);
				selector.RefreshAfterRangeChange();
			}
		}

		// Re-evaluates the value against the new range and refreshes the visual part.
		private void RefreshAfterRangeChange()
		{
			// The coercion fires OnValueChangedCallback only if the value actually changes.
			CoerceValue(ValueProperty);

			// ResetValue also lives inside the range, so it has to be re-evaluated.
			CoerceValue(ResetValueProperty);

			// And so do the steps, which are bounded to the width of the range.
			CoerceValue(SmallChangeProperty);
			CoerceValue(LargeChangeProperty);

			// And this covers the case where the range changed without altering the value: the
			// proportion of the bar changes all the same, so it has to be redrawn.
			OnValueChangedHandler(Value);
		}

		// The brushes need NO redraw callback: all three (BorderBrush, BarFill and
		// BarDividerBrush) reach the template through TemplateBinding, and each element
		// repaints itself when its brush changes. The InvalidateVisual() that used to be here
		// forced a redraw of the control which, being lookless, draws nothing of its own (it
		// does not override OnRender): it was work with no recipient.

		// Callback for the properties that affect layout.
		private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is BoundedNumericSelector selector)
			{
				// Force a re-evaluation of the layout.
				selector.InvalidateMeasure(); // Says the measurement of the control may have changed.
				selector.InvalidateArrange(); // Says the arrangement may have changed.
			}
		}

	}
}
