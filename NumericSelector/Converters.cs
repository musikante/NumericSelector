using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NumericSelector
{
	/// <summary>
	/// Resolves, in a pure function, the border <see cref="Thickness"/> that belongs to each
	/// cell of the control, out of the seam matrix.
	/// </summary>
	/// <remarks>
	/// Cell model: the bar with its <c>MainText</c> (upper row) and the detail row (the one the
	/// value may drop into) are the fixed frame; the value box is the only element that moves
	/// depending on the layout. The rule is a single one: the seam separating two cells is
	/// always drawn by the fixed neighbour (the bar, above), and the box gives up the side
	/// facing it; that way no edge is drawn twice and there are no double-thickness lines.
	///
	/// Matrix (sides in WPF order: Left, Top, Right, Bottom). The first column is
	/// "ShowDetail and ValueFollowsDetail", the value drops below next to the detail; the
	/// second one is the value beside the bar:
	///
	///   cell    always
	///   Bar     Left,Top,Right,Bottom
	///   Detail  Left,0,Right,Bottom
	///   Value   (Right): 0,y,Right,Bottom   (Left): Left,y,0,Bottom
	///           y = Top if it stays next to the bar; 0 if it drops to the detail.
	///
	/// Caveats of the matrix:
	///   • The horizontal seam between rows is ALWAYS drawn by the bottom border of the
	///     bar, which also acts as the bottom border of the control when there is no detail.
	///   • The value box always gives up the side facing its row partner (the one opposite
	///     to ValueBoxDock); that edge is drawn by the fixed neighbour.
	/// </remarks>
	public sealed class ValueBorderResolver : IMultiValueConverter
	{
		/// <summary>Parameter of the bar cell.</summary>
		public const string BarCell = "Bar";
		/// <summary>Parameter of the value box.</summary>
		public const string ValueCell = "Value";
		/// <summary>Parameter of the detail row.</summary>
		public const string DetailCell = "Detail";

		/// <summary>
		/// The pure function of the matrix: given the base thickness per side and the
		/// (ShowDetail, ValueFollowsDetail, ValueBoxDock) triple, it returns the
		/// <see cref="Thickness"/> that belongs to the cell named by <paramref name="cell"/>.
		/// </summary>
		/// <remarks>
		/// It is static and stateless so that the tests can call it without a window or an
		/// instance of the control. The converter does nothing but dispatch its arguments here.
		/// </remarks>
		public static Thickness Resolve(
			Thickness pixels, bool showDetail, bool followsDetail, ValueBoxDock side, string cell)
		{
			// "down": the value drops to the detail row (which has to be visible).
			bool down = showDetail && followsDetail;

			switch (cell)
			{
				case BarCell:
				// Base frame: the bar always carries its four sides; its bottom border also
				// draws the horizontal seam when there is a detail row below.
					return pixels;

				case ValueCell:
				// The only element that moves: it gives up the side facing its row partner
				// (the one given by ValueBoxDock) to its fixed neighbour, and the top side
				// when it drops to the detail, because the horizontal seam is drawn by the
				// bar above.
					bool left = side == ValueBoxDock.Left;
					return new Thickness(
						left ? pixels.Left : 0,
						down ? 0 : pixels.Top,
						left ? 0 : pixels.Right,
						pixels.Bottom);

				case DetailCell:
				// Fixed frame: never on the top side (that seam is drawn by the bar).
					return new Thickness(pixels.Left, 0, pixels.Right, pixels.Bottom);

				default:
				// The parameter is a string typed by hand in the template, so a typo is the
				// likely mistake. This used to fall into the detail case and return a
				// plausible Thickness: the cell was drawn wrong with nothing to warn about
				// it. Breaking here is worth more; for our own template nothing changes,
				// because its three parameters are these constants.
					throw new ArgumentException(
						$"Unknown cell: \"{cell}\". The valid values are " +
						$"\"{BarCell}\", \"{ValueCell}\" and \"{DetailCell}\".",
						nameof(cell));
			}
		}

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var pixels = values.Length > 0 && values[0] is Thickness t ? t : new Thickness(0);
			bool showDetail = values.Length > 1 && values[1] is bool sd && sd;
			bool followsDetail = values.Length > 2 && values[2] is bool fd && fd;
			var side = values.Length > 3 && values[3] is ValueBoxDock s ? s : ValueBoxDock.Right;

			return Resolve(pixels, showDetail, followsDetail, side, parameter as string ?? "");
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}
