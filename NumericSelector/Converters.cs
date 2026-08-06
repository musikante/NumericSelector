using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NumericSelector
{
	/// <summary>
	/// Resuelve en una función pura el <see cref="Thickness"/> de borde que corresponde a
	/// cada celda del control, a partir de la matriz de costuras.
	/// </summary>
	/// <remarks>
	/// Modelo de celdas: el control son celdas hermanas con marco propio, y la costura se
	/// resuelve mirando qué celdas comparten fila. La regla es única: donde una celda con
	/// prioridad —el casillero del valor— se encuentra con un vecino, el vecino cede el
	/// lado que toca; así ningún filo se dibuja dos veces y no hay líneas de doble espesor.
	///
	/// Matriz (lados en el orden de WPF: Left, Top, Right, Bottom). La primera columna es
	/// "ShowTitle y ValueFollowsTitle" (el valor vive arriba, junto al título, y la barra
	/// queda sola); la segunda es el valor al lado de la barra:
	///
	///   celda   valor arriba                    valor al lado de la barra
	///   Bar     Left,Top,Right,Bottom              cede el lado de ValueBoxSide
	///   Value   Left,Top,Right,0                   Left,Top,Right,Bottom
	///   Title   Lado opuesto al casillero,0,0      Left,Top,Right,0
	///
	/// Salvedades de la matriz:
	///   • La costura horizontal entre filas la dibuja SIEMPRE el borde superior de la
	///     barra, que además hace de borde superior del control cuando no hay título. Por
	///     eso las celdas de la fila de arriba ceden siempre la base (0).
	///   • Cuando el casillero del valor llega arriba dibuja el filo compartido con el
	///     título, y el título le cede el lado que toca según ValueBoxSide.
	/// </remarks>
	public sealed class ValueBorderResolver : IMultiValueConverter
	{
		/// <summary>Parámetro de la celda de la barra.</summary>
		public const string BarCell = "Bar";
		/// <summary>Parámetro del casillero del valor.</summary>
		public const string ValueCell = "Value";
		/// <summary>Parámetro de la etiqueta del título.</summary>
		public const string TitleCell = "Title";

		/// <summary>
		/// Función pura de la matriz: dado el grosor base por lado y la (ShowTitle,
		/// ValueFollowsTitle, ValueBoxSide) devuelve el <see cref="Thickness"/> que toca a la
		/// celda indicada por <paramref name="cell"/>.
		/// </summary>
		/// <remarks>
		/// Es estática y sin estado para que las pruebas la llamen sin ventana ni instancia
		/// del control. El conversor no hace más que despachar sus argumentos acá.
		/// </remarks>
		public static Thickness Resolve(
			Thickness pixels, bool showTitle, bool followTitle, ValueBoxSide side, string cell)
		{
			bool up = showTitle && followTitle;

			switch (cell)
			{
				case BarCell:
				// Con el valor arriba la barra está sola y recupera los cuatro lados. Junto a la
				// caja del valor, cede el lado que esa caja toca (el de ValueBoxSide).
					return up
						? pixels
						: side == ValueBoxSide.Right
							? new Thickness(pixels.Left, pixels.Top, 0, pixels.Bottom)
							: new Thickness(0, pixels.Top, pixels.Right, pixels.Bottom);

				case ValueCell:
					// Prioridad: define sus lados. Sólo cede la base cuando sube, porque la
					// costura horizontal la dibuja la barra que tiene debajo.
					return up
						? new Thickness(pixels.Left, pixels.Top, pixels.Right, 0)
						: pixels;

				default: // TitleCell
// Siempre sin base (la costura la dibuja la barra). Cuando el valor sube a la
				// derecha, la caja dibuja el filo compartido y el título cede su lado derecho;
				// cuando sube a la izquierda, la caja queda a la izquierda y el título cede el
				// lado izquierdo.
					return up
						? side == ValueBoxSide.Right
							? new Thickness(pixels.Left, pixels.Top, 0, 0)
							: new Thickness(0, pixels.Top, pixels.Right, 0)
						: new Thickness(pixels.Left, pixels.Top, pixels.Right, 0);
			}
		}

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var pixels = values.Length > 0 && values[0] is Thickness t ? t : new Thickness(0);
			bool showTitle = values.Length > 1 && values[1] is bool st && st;
			bool followTitle = values.Length > 2 && values[2] is bool ft && ft;
			var side = values.Length > 3 && values[3] is ValueBoxSide s ? s : ValueBoxSide.Right;

			return Resolve(pixels, showTitle, followTitle, side, parameter as string ?? "");
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			=> throw new NotSupportedException();
	}
}