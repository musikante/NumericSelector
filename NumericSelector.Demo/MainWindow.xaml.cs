using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NumericSelector;

namespace NumericSelector.Demo
{
	/// <summary>
	/// The demo's test bench. The Master at the top is the control under test, and every knob
	/// driving it is another BoundedNumericSelector, so the demo exercises the control while
	/// showing it off.
	/// </summary>
	/// <remarks>
	/// The pickers that cannot be expressed as a plain binding (fonts, brushes) work by index:
	/// the selector moves over a list —see Converters.cs— and the handler applies the chosen
	/// item to the Master AND to the picker itself, so that the change is visible on the very
	/// control that produced it.
	/// </remarks>
	public partial class MainWindow : Window
	{
		private int _valueChangedCount;

		public MainWindow()
		{
			InitializeComponent();
			Loaded += MainWindow_Loaded;
		}
		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// The font picker walks the list of system fonts; `Maximum` is not a constant for
			// this machine, so it is set at runtime once the list is known.
			FontPicker.Maximum = FontIndexConverter.Filtered.Length - 1;

			// FontFamily does not implement equality by value, so we cannot compare the
			// Master's instance against the ones in the list; we preselect it by name.
			string current = MasterNumericSelector.FontFamily.Source;
			int i = Array.FindIndex(FontIndexConverter.Filtered, f => f.Source == current);
			if (i >= 0) FontPicker.Value = i;

			// FontStyle and FontWeight are structs with equality by value: direct comparison.
			Preselect(FontStylePicker, FontStyleIndexConverter.Styles, MasterNumericSelector.FontStyle);
			Preselect(FontWeightPicker, FontWeightIndexConverter.Weights, MasterNumericSelector.FontWeight);
		}

		// F1 (or the button in the footer) opens the demo help.
		private void Help_Executed(object sender, ExecutedRoutedEventArgs e) => HelpWindow.Open(this);

		private static void Preselect<T>(BoundedNumericSelector selector, T[] options, T current)
			where T : struct
		{
			int i = Array.IndexOf(options, current);
			if (i >= 0) selector.Value = i;
		}

		// Applies the chosen font to the Master (and gives it to the picker itself as well, so
		// that the change is visible on the control that produced it).
		private void FontPicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyFont();

		private void ApplyFont()
		{
			int i = FontPicker.Value;
			// With an empty list —a filter with no matches— there is nothing to apply and the
			// last font that was set is kept.
			if (i < 0 || i >= FontIndexConverter.Filtered.Length) return;

			var font = FontIndexConverter.Filtered[i];
			MasterNumericSelector.FontFamily = font;
			FontPicker.FontFamily = font;
		}

		// Filters the list of fonts FontPicker walks over. It is applied on every keystroke.
		private void FontFilter_TextChanged(object sender, TextChangedEventArgs e)
		{
			// Which font was chosen, so as to find it again if it survives the filter. The name
			// is what gets saved and not the instance, because FontFamily has no equality by
			// value.
			int previousIndex = FontPicker.Value;
			string? chosen = (previousIndex >= 0 && previousIndex < FontIndexConverter.Filtered.Length)
				? FontIndexConverter.Filtered[previousIndex].Source
				: null;

			int matchCount = FontIndexConverter.Filter(FontFilterBox.Text);

			// The range of the control is always at least 1 wide: with 0 or 1 matches this asks
			// for 0 and the coercion raises it to 1. It shows in the DetailText, which for that
			// spare index says there is no font. It is the real limit of the API.
			FontPicker.Maximum = Math.Max(matchCount - 1, 0);

			int i = chosen is null
				? -1
				: Array.FindIndex(FontIndexConverter.Filtered, f => f.Source == chosen);
			FontPicker.Value = i >= 0 ? i : 0;

			// The DetailText is bound to Value, so it is only recomputed when Value changes.
			// Filtering can change the LIST without changing the index, and then the name being
			// shown would be that of the previous font: the refresh has to be asked for by hand.
			BindingOperations
				.GetBindingExpression(FontPicker, BoundedNumericSelector.DetailTextProperty)
				?.UpdateTarget();

			// And for the same reason it has to be reapplied: if the index did not change but
			// the list did, ValueChanged does not fire and the font would drift out of step with
			// the name being read.
			ApplyFont();
		}

		private void FontStylePicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
		{
			int i = FontStylePicker.Value;
			if (i < 0 || i >= FontStyleIndexConverter.Styles.Length) return;

			var style = FontStyleIndexConverter.Styles[i];
			MasterNumericSelector.FontStyle = style;
			FontStylePicker.FontStyle = style;
		}

		private void FontWeightPicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
		{
			int i = FontWeightPicker.Value;
			if (i < 0 || i >= FontWeightIndexConverter.Weights.Length) return;

			var weight = FontWeightIndexConverter.Weights[i];
			MasterNumericSelector.FontWeight = weight;
			FontWeightPicker.FontWeight = weight;
		}

		private void Master_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
		{
			// The event already fires while the XAML assigns Value="50", that is, before
			// InitializeComponent() has created the elements that come further down.
			// Without this guard, EventLog is still null and the window blows up on opening.
			if (EventLog is null) return;

			_valueChangedCount++;
			EventLog.Text = $"#{_valueChangedCount}   {e.OldValue} -> {e.NewValue}";
		}

		// Applies the color chosen by a picker to its property on the Master (the subject under
		// test) and on the picker itself, so that the change is visible on the control that
		// produced it (there is no need to focus the Master to appreciate e.g.
		// FocusBorderBrush). Each picker is wired to a single property, the same one it
		// announces in its MainText.
		private void ApplyColor(BoundedNumericSelector picker, DependencyProperty property)
		{
			int i = picker.Value;
			if (i < 0 || i >= ColorIndexConverter.Palette.Length) return;

			var brush = new SolidColorBrush(ColorIndexConverter.Palette[i]);
			MasterNumericSelector.SetValue(property, brush);
			picker.SetValue(property, brush);
		}

		private void BarFill_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, BoundedNumericSelector.BarFillProperty);

		private void BarDividerBrush_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, BoundedNumericSelector.BarDividerBrushProperty);

		private void BorderBrush_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, Control.BorderBrushProperty);

		private void FocusBorderBrush_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, BoundedNumericSelector.FocusBorderBrushProperty);

		private void Foreground_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, Control.ForegroundProperty);

		private void Background_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
			=> ApplyColor((BoundedNumericSelector)sender, Control.BackgroundProperty);
	}
}
