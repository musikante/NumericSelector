using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NumericSelector
{
/// <summary>
	/// Resuelve en una función pura el <see cref="Thickness"/> de borde que corresponde a
	/// cada celda del control, a partir de la matriz de costuras.
	/// </summary>
	/// <remarks>
	/// Modelo de celdas: la barra con su Caption (fila superior) y la fila de detalle (la
	/// que pueda descender el valor) son el marco fijo; la caja del valor es el único
	/// elemento que cambia según la posición. La regla es única: la costura que separa dos
	/// celdas la dibuja siempre el vecino fijo (la barra arriba), y la caja cede el lado que
	/// mira hacia él; así ningún filo se dibuja dos veces y no hay líneas de doble espesor.
	///
	/// Matriz (lados en el orden de WPF: Left, Top, Right, Bottom). La primera columna es
	/// "ShowDetail y ValueFollowsDetail", el valor desciende debajo junto al detalle; la
	/// segunda es el valor al lado de la barra:
	///
	///   celda   siempre
	///   Bar     Left,Top,Right,Bottom
	///   Detail  Left,0,Right,Bottom
	///   Value   (Right): 0,y,Right,Bottom   (Left): Left,y,0,Bottom
	///           y = Top si se queda junto a la barra; 0 si desciende al detalle.
	///
	/// Salvedades de la matriz:
	///   • La costura horizontal entre filas la dibuja SIEMPRE el borde inferior de la
	///     barra, que además hace de borde inferior del control cuando no hay detalle.
	///   • La caja del valor cede siempre el lado que mira a su compañero de fila (el
	///     opuesto a ValueBoxSide); ese filo lo dibuja el vecino fijo.
	/// </remarks>
	public sealed class ValueBorderResolver : IMultiValueConverter
	{
		/// <summary>Parámetro de la celda de la barra.</summary>
		public const string BarCell = "Bar";
		/// <summary>Parámetro del casillero del valor.</summary>
		public const string ValueCell = "Value";
		/// <summary>Parámetro de la fila de detalle.</summary>
		public const string DetailCell = "Detail";

		/// <summary>
		/// Función pura de la matriz: dado el grosor base por lado y la (ShowDetail,
		/// ValueFollowsDetail, ValueBoxSide) devuelve el <see cref="Thickness"/> que toca a la
		/// celda indicada por <paramref name="cell"/>.
		/// </summary>
		/// <remarks>
		/// Es estática y sin estado para que las pruebas la llamen sin ventana ni instancia
		/// del control. El conversor no hace más que despachar sus argumentos acá.
		/// </remarks>
		public static Thickness Resolve(
			Thickness pixels, bool showDetail, bool followsDetail, ValueBoxSide side, string cell)
		{
			// "down": el valor desciende a la fila de detalle (que debe estar visible).
			bool down = showDetail && followsDetail;

			switch (cell)
			{
				case BarCell:
				// Marco base: la barra lleva siempre los cuatro lados; su borde inferior
				// dibuja además la costura horizontal cuando hay fila de detalle debajo.
					return pixels;

				case ValueCell:
				// Único elemento que cambia: cede el lado que mira a su compañero de fila
				// (el de ValueBoxPosition) a su vecino fijo, y el superior cuando desciende
				// al detalle, porque la costura horizontal la dibuja la barra que está
				// arriba.
					bool left = side == ValueBoxSide.Left;
					return new Thickness(
						left ? pixels.Left : 0,
						down ? 0 : pixels.Top,
						left ? 0 : pixels.Right,
						pixels.Bottom);

				default: // DetailCell
				// Marco fijo: nunca en la parte superior (la costura la dibuja la barra).
					return new Thickness(pixels.Left, 0, pixels.Right, pixels.Bottom);
			}
		}

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var pixels = values.Length > 0 && values[0] is Thickness t ? t : new Thickness(0);
			bool showDetail = values.Length > 1 && values[1] is bool sd && sd;
			bool followsDetail = values.Length > 2 && values[2] is bool fd && fd;
			var side = values.Length > 3 && values[3] is ValueBoxSide s ? s : ValueBoxSide.Right;

			return Resolve(pixels, showDetail, followsDetail, side, parameter as string ?? "");
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Tipografía y formato que usa un control para medir sus textos: la "fuente global"
	/// del control, única para todos sus textos (título, leyenda y valor). Es un record
	/// inmutable y de igualdad por valor: se arma una vez por cambio de tipografía y se
	/// reutiliza en todas las mediciones. El DPI no vive acá —se pasa por tanda de
	/// medición (ver <see cref="TextMeasure"/>)— porque cambia de monitor y se obtiene
	/// en vivo, sin riesgo de quedarse con una escala vencida.
	/// </summary>
	internal sealed record TextMeasureContext(
		Typeface Typeface,
		double FontSize,
		CultureInfo Culture,
		FlowDirection Flow);

	/// <summary>
	/// Medición pura de texto: dado el <see cref="TextMeasureContext"/> y el DPI de la
	/// escala activa, devuelve el <see cref="Size"/> (ancho y alto) netos que ocuparía el
	/// texto, sin padding ni bordes —esos los suma el llamador. El alto es de una sola
	/// línea: se mide con ancho ilimitado. Función pura: no depende de ventana ni de
	/// instancia del control, y se prueba sin ella.
	/// </summary>
	internal static class TextMeasure
	{
		/// <summary>
		/// Mide el ancho y alto netos del texto dado, con la tipografía del contexto a la
		/// escala <paramref name="dpi"/> (PixelsPerDip). Texto vacío o nulo devuelve
		/// <see cref="Size.Empty"/>.
		/// </summary>
		public static Size Measure(TextMeasureContext context, double dpi, string text)
		{
			if (string.IsNullOrEmpty(text))
				return Size.Empty;

			var ft = new FormattedText(
				text,
				context.Culture,
				context.Flow,
				context.Typeface,
				context.FontSize,
				Brushes.Black,
				dpi);

			return new Size(ft.Width, ft.Height);
		}
	}
}