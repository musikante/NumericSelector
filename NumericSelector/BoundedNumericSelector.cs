using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace NumericSelector
{
	/// <summary>
	/// Lookless WPF control for picking an <see cref="int"/> value bounded to a range: there
	/// is no way for it to hand out a value outside [<see cref="Minimum"/>,
	/// <see cref="Maximum"/>], so the consumer never has to validate the input.
	/// </summary>
	/// <remarks>
	/// It is drawn as four sibling cells, each with its own frame: the bar with
	/// <see cref="MainText"/> over its fill, the value box, and —when <see cref="ShowDetail"/>
	/// is on— the detail row with a value box of its own. Which sides each cell draws is
	/// resolved by <see cref="ValueBorderResolver"/>, so that no edge is ever drawn twice.
	/// The width of the value box is reserved by the hidden sizers in the template, which is
	/// what guarantees the number is never clipped; see <see cref="BaseWidth"/> for how the
	/// control grows and how it fits into a narrow container.
	/// The default template lives in Themes/Generic.xaml.
	/// </remarks>
	// The template contract, declared for whoever writes a replacement template: these are the
	// four parts OnApplyTemplate looks up by name, and the type each one is used as.
	// Only these four are declared, and that is the point of the attribute: the rest of the
	// PART_* names in Themes/Generic.xaml exist so that the template triggers can aim at them
	// with TargetName, which is internal business of that template and binds nobody. A
	// replacement template is free to drop them.
	// The parts are optional in practice —the code-behind checks for null and a control with
	// none of them still builds and coerces— but without them there is no bar to click on and
	// no number to read, so a template that omits one is almost certainly a mistake.
	[TemplatePart(Name = "PART_BarGrid", Type = typeof(Grid))]
	[TemplatePart(Name = "PART_BarRect", Type = typeof(Border))]
	[TemplatePart(Name = "PART_ValueText", Type = typeof(TextBlock))]
	[TemplatePart(Name = "PART_ValueDetailText", Type = typeof(UIElement))]
	// 'partial' marks that this class is spread over more than one file: interaction,
	// measurement and cursors live here; the dependency properties and their coercions live
	// in BoundedNumericSelector.Dependencies.cs.
	public partial class BoundedNumericSelector : Control
	{
		// --- Instance fields ---
		// References to the template parts that the interaction draws on or listens to: the
		// bar and the two value texts. The pieces that only make up the appearance (the root
		// grid, the main text over the bar and the detail text) resolve themselves in the
		// XAML and need no field of their own here.
		private Grid? _barGrid;
		private Border? _barRect;
		private TextBlock? _valueText;
		private UIElement? _valueDetail;
		private Point _valueDragStart;
		// Fraction of the bar width taken by each side zone of the right click
		// (left -> Minimum, right -> Maximum). The remaining centre -> ResetValue.
		private const double RightClickEdgeZone = 0.3;

		// Whether the control already had the focus when the current press started. It is
		// taken in OnPreviewMouseDown (tunnel phase) because by the time the handlers of the
		// parts run (bubble phase) the Focus() of that very press HAS already been applied,
		// and asking IsKeyboardFocused there would always answer true: verified, the guard
		// would filter nothing.
		private bool _hadFocusOnPress;

		// --- Instance constructor ---
		public BoundedNumericSelector()
		{
			// WPF formats bindings according to FrameworkElement.Language, whose default value
			// is "en-US" no matter what the Windows regional settings say: that is why a
			// StringFormat N0 would show "1,000" where "1.000" belongs.
			// It has to be an ASSIGNED value and not a metadata default, because what does the
			// formatting is each TextBlock of the template, and inheritance does not propagate
			// defaults. With SetCurrentValue the control adopts the culture of the system it
			// runs on, and whoever uses it can override that by assigning Language on the
			// instance.
			SetCurrentValue(LanguageProperty,
				XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag));
		}

		// --- Life cycle and template handling ---
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// OnApplyTemplate can run more than once (for instance if the template is
			// replaced): we drop the subscriptions of the previous parts before taking the
			// new ones, so as not to end up with duplicated handlers.
			DetachTemplateParts();

			// Get the references to the visual elements of the template.
			_barGrid = GetTemplateChild("PART_BarGrid") as Grid;
			_barRect = GetTemplateChild("PART_BarRect") as Border;
			_valueText = GetTemplateChild("PART_ValueText") as TextBlock;
			_valueDetail = GetTemplateChild("PART_ValueDetailText") as UIElement;

			AttachTemplateParts();

			// Initial state: the fill and the cursors.
			UpdateBarFill(Value);
			UpdateCursors();
		}

		// The template parts are children of the control, so these subscriptions leak nothing
		// (the control <-> children cycle is collected as a whole). That is why they are NOT
		// unsubscribed on Unloaded: doing so would leave the control inert if it were loaded
		// again (on a tab change, for example) since OnApplyTemplate does not run twice.
		private void AttachTemplateParts()
		{
			if (_valueText != null)
			{
				_valueText.MouseLeftButtonDown += ValueText_MouseLeftButtonDown;
				_valueText.MouseMove += ValueText_MouseMove;
				_valueText.MouseLeftButtonUp += ValueText_MouseLeftButtonUp;
			}

			// The value in the detail row (it dropped there with ValueFollowsDetail) uses the same gestures.
			if (_valueDetail != null)
			{
				_valueDetail.MouseLeftButtonDown += ValueText_MouseLeftButtonDown;
				_valueDetail.MouseMove += ValueText_MouseMove;
				_valueDetail.MouseLeftButtonUp += ValueText_MouseLeftButtonUp;
			}

			// Recompute the bar fill whenever the available room changes, and enable the
			// mouse interaction on the bar.
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

		// Reacts to the state changes that call for redrawing cursors or releasing the focus.
		// The width of the control is NOT recomputed here: the floor and the growth are
		// resolved by the natural measurement of the content in MeasureOverride (the hidden
		// sizer of the value box already reserves the width of the longest number, and
		// BaseWidth defines the base width).
		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.Property == InteractionModeProperty || e.Property == MouseBehaviorProperty)
			{
				UpdateCursors();
			}

			// IsEnabled=false prevents GAINING the focus, but it does not release one that was
			// already taken (see ReleaseKeyboardFocusIfHeld). Without this, disabling a focused
			// control left it responding to the keyboard and with the focus frame lit up.
			if (e.Property == IsEnabledProperty && !IsEnabled)
			{
				ReleaseKeyboardFocusIfHeld();
			}
		}

		// --- Measurement ---

		// Safety margin on top of the measured width of the texts. WPF's rounding of the
		// glyphs (UseLayoutRounding plus the fractional measurements of FormattedText) makes
		// the "natural" width reported by the template fall a hair short of the width the
		// render asks for, by a few pixels: the result is MainText/DetailText showing
		// CharacterEllipsis at one font size and not showing it one step further up, even
		// though the control has room to spare. When the control grows from BaseWidth or from
		// the content, asking the layout for a few extra pixels (which are only granted when
		// the container has room) keeps the text away from the ellipsis threshold. When the
		// slot is tight, the clamp below still cuts it down all the same.
		private const double MeasureSlack = 3.0;

		// The control grows from BaseWidth (or from the natural width of the content if that
		// is NaN) to fit its text, but it NEVER asks for more width than the slot the panel
		// gives it: that way its frame (the borders) does not fall outside the container and
		// is not clipped. If the text does not fit in that slot, the CharacterEllipsis of the
		// main text takes over. BaseWidth is not a hard WPF constraint (it does not force the
		// size, it is only a floor).
		protected override Size MeasureOverride(Size constraint)
		{
			// Natural width of the content. It is measured with infinite width on purpose: the
			// bar lives in a '*' column, which against a finite width would stretch to
			// everything available and report the whole container's width as "needed". With
			// infinite width, '*' columns behave like Auto.
			Size natural = base.MeasureOverride(new Size(double.PositiveInfinity, constraint.Height));

			// The natural width rounded up, plus the slack: that way the length being reserved
			// is always enough for the text as the render measures it, and a one-pixel change
			// in the measurement does not decide on its own.
			double reserved = Math.Ceiling(natural.Width) + MeasureSlack;

			// Floor = BaseWidth if the consumer set it; growth never shrinks the content, so
			// the natural width also counts if it is the larger one.
			double width = Math.Max(
				double.IsNaN(BaseWidth) ? 0 : BaseWidth,
				reserved);

			// Do not grow beyond what the container offers: if there is room it grows (from
			// the BaseWidth floor); if there is not, the frame stays within what is available
			// and the main text truncates with an ellipsis. An empty or infinite width
			// constraint is let through as is.
			if (!double.IsPositiveInfinity(constraint.Width) && !double.IsNaN(constraint.Width)
				&& width > constraint.Width)
			{
				width = constraint.Width;
			}

			// It is measured again with the width settled by the floor so that the template
			// distributes the columns (the '*' bar fills up and the main text truncates with
			// an ellipsis if room is missing), but the DesiredSize returned is the LARGER of
			// the floor asked for and what the template measured: the '*' column reports no
			// width in the Grid's DesiredSize, so without this maximum the BaseWidth floor
			// would be invisible to a StackPanel and the control would shrink to its content
			// despite having room.
			Size measured = base.MeasureOverride(new Size(width, constraint.Height));
			return new Size(Math.Max(width, measured.Width), Math.Max(measured.Height, natural.Height));
		}

		// --- User interface event handling (instance level) ---

		// Instance-specific logic for key handling.
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			// Neither in display-only mode nor disabled should any key arrive, because in both
			// cases the focus is released and without focus there is no keyboard. The guard is
			// one line long and closes the case of a focus arriving by some other path.
			// The MustFocusFirst mode does NOT come in here: it is a mouse rule. If the control
			// has the focus to receive the key, that requirement is already met.
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

		// --- Mouse interaction ---

		// Turns a mouse position (inside the bar) into the matching integer value, along the
		// horizontal axis (0 = left, 1 = right).
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
			// The range is computed in long so that an extreme Minimum/Maximum does not overflow.
			long range = (long)Maximum - Minimum;
			long value = Minimum + (long)Math.Round(ratio * range);
			return (int)Math.Clamp(value, Minimum, Maximum);
		}

		private void BarGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// No capture: if the gesture is not enabled it must not be possible to drag from
			// that first click either, without ever releasing the button.
			if (!MouseGesturesAllowed)
				return;

			_barGrid?.CaptureMouse();
			Value = ValueFromPosition(e.GetPosition(_barGrid));
			e.Handled = true;
		}

		private void BarGrid_MouseMove(object sender, MouseEventArgs e)
		{
			// The capture already implies the gesture started out enabled; it is re-checked in
			// case the mode was changed from code in the middle of the drag.
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

		// Right click by zones: left -> Minimum, centre -> ResetValue, right -> Maximum.
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

		// Double click on the number -> ResetValue.
		// A single click plus a vertical drag -> adjusts the value by SmallChange.
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
			double delta = _valueDragStart.Y - current.Y; // dragging upwards raises the value

			if (delta >= 1) { Value += SmallChange; _valueDragStart = current; }
			else if (delta <= -1) { Value -= SmallChange; _valueDragStart = current; }
		}

		private void ValueText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (sender is UIElement el && el.IsMouseCaptured)
				el.ReleaseMouseCapture();
		}

		// Mouse wheel -> +/- SmallChange, with two precautions so as not to swallow the scroll
		// of a containing ScrollViewer (MouseWheel is a bubbling event: if we mark it as
		// handled, the ScrollViewer never hears about it and the list does not scroll):
		//   1) We only act if the control has the focus. That way, passing the mouse over it
		//      while scrolling a list does not alter values by accident.
		//   2) We only mark Handled if the value actually changed. At the ends the wheel goes
		//      on being useful for scrolling instead of going dead.
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);

			// The wheel already demanded focus in both modes, so MustFocusFirst adds nothing to
			// it. InteractionMode=ReadOnly does: without it, an inherited focus would leave the
			// wheel alive.
			if (InteractionMode == UserInteractionMode.ReadOnly || !IsKeyboardFocused)
				return;

			int before = Value;
			Value += e.Delta > 0 ? SmallChange : -SmallChange;
			e.Handled = Value != before;
		}

		// Any click (left, right or middle) anywhere on the control gives it the focus. It
		// uses the tunnel event to get ahead of the child handlers.
		protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseDown(e);

			// Before focusing: the handlers of the bubble phase need to know whether the focus
			// was already taken BEFORE this press (see _hadFocusOnPress).
			_hadFocusOnPress = IsKeyboardFocused;

			if (InteractionMode == UserInteractionMode.ReadOnly)
				return;

			Focus();
		}

		// The focus takes part in the cursor decision (see UpdateCursors), so the cursor has
		// to be repainted both when it arrives and when it leaves.
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

		// Releases the keyboard focus if the control is holding it.
		// Neither removing Focusable (InteractionMode.ReadOnly) nor IsEnabled=false releases it
		// on their own: verified in both cases, IsKeyboardFocused stayed true. And as long as
		// the control holds it the keyboard KEEPS arriving, because keys are routed to the
		// focused element and not to the one under the pointer: measured, an arrow key moved
		// the value of a disabled control. On top of that the focus frame stays lit, which on a
		// disabled control is plainly a lie.
		// The mouse needs no such care: with IsEnabled=false the input hit-test already stops
		// returning parts of the control (verified with InputHitTest), and since the wheel is
		// also routed from the element under the pointer, the same mechanism covers it.
		private void ReleaseKeyboardFocusIfHeld()
		{
			if (!IsKeyboardFocused)
				return;

			// To the focusable ancestor: the focus has to go somewhere, and handing it back to
			// the window takes it out of the control without stealing it from another concrete
			// control.
			var scope = FocusManager.GetFocusScope(this);
			FocusManager.SetFocusedElement(scope, null);
			Keyboard.ClearFocus();
		}

		// --- Cursors ---

		// The cursor tells the truth about whether the gesture is going to do anything:
		//   InteractionMode.ReadOnly   -> Arrow (wins over everything else)
		//   MustFocusFirst and no focus -> Arrow (the next click is only going to focus)
		//   anything else               -> the cursor of the gesture
		// In ChangeOnClick it does NOT depend on the focus: there the gesture works just the
		// same without it, and showing an Arrow would be lying.
		private void UpdateCursors()
		{
			bool gestures = InteractionMode != UserInteractionMode.ReadOnly &&
				(MouseBehavior == MouseInteractionBehavior.ChangeOnClick || IsKeyboardFocused);

			// Horizontal drag on the bar.
			if (_barGrid != null)
				_barGrid.Cursor = gestures ? Cursors.SizeWE : Cursors.Arrow;

			// The value box is dragged vertically.
			var valueCursor = gestures ? Cursors.SizeNS : Cursors.Arrow;
			if (_valueText != null) _valueText.Cursor = valueCursor;
			if (_valueDetail is FrameworkElement fed) fed.Cursor = valueCursor;

			// WPF resolves the cursor during mouse movement. This change happens with the
			// pointer STILL (a click is made, the focus arrives, the hand did not move), so the
			// re-evaluation has to be requested by hand or the new cursor would not be seen
			// until the next movement, exactly when the hint matters most.
			if (IsMouseOver)
				Mouse.UpdateCursor();
		}

		// --- Gesture enablement ---

		// The single decision point for every mouse gesture: in display-only mode none of them
		// act, and in MustFocusFirst they only act if the control already had the focus when
		// the press started (so the click that focuses does not change the value as well).
		// Removing Focusable is NOT enough to stop the mouse: verified, the handlers of the
		// parts went on running and the value did change.
		private bool MouseGesturesAllowed =>
			InteractionMode != UserInteractionMode.ReadOnly &&
			(MouseBehavior == MouseInteractionBehavior.ChangeOnClick || _hadFocusOnPress);

		// Updates the size of the fill rectangle so that it represents the proportion of the
		// current value within the [Minimum, Maximum] range.
		private void OnValueChangedHandler(int newValue)
		{
			UpdateBarFill(newValue);
			// The value text updates itself through its binding to Value.
		}

		private void UpdateBarFill(int value)
		{
			if (_barRect == null || _barGrid == null)
				return;

			// The range is computed in long so that an extreme Minimum/Maximum does not overflow.
			long range = (long)Maximum - Minimum;
			double ratio = (range > 0) ? Math.Clamp((double)(value - (long)Minimum) / range, 0, 1) : 0;

			// The fill grows towards the right in proportion to the value.
			// The height is set from code because the rectangle lives inside a Canvas
			// (see Generic.xaml), which does not stretch its children.
			_barRect.Width = _barGrid.ActualWidth * ratio;
			_barRect.Height = _barGrid.ActualHeight;
		}
	}
}
