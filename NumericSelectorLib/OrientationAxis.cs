using System;
using System.Windows;
using System.Windows.Input;

namespace NumericSelectorLib
{
	/// <summary>
	/// Abstrae el "eje principal" del control para que la lógica de interacción y de
	/// pintado no dependa de la orientación. Toda la dependencia de orientación queda
	/// concentrada acá (Fase 1: solo existe el eje horizontal; el vertical se agrega
	/// en la fase 2, sin tocar el resto del control).
	/// </summary>
	internal abstract class OrientationAxis
	{
		/// <summary>Instancia compartida del eje horizontal (no tiene estado).</summary>
		public static readonly OrientationAxis Horizontal = new HorizontalAxis();

		/// <summary>Largo útil del control a lo largo del eje principal.</summary>
		public abstract double GetExtent(Size barSize);

		/// <summary>
		/// Convierte una posición del mouse (dentro de la barra) en una proporción 0..1
		/// a lo largo del eje principal (0 = Minimum, 1 = Maximum).
		/// </summary>
		public abstract double PositionToRatio(Point p, Size barSize);

		/// <summary>Aplica el relleno dimensionando su eje principal.</summary>
		public abstract void ApplyFill(FrameworkElement fill, Size barSize, double ratio);

		/// <summary>
		/// Cursor de la barra cuando el arrastre está habilitado: la flecha doble del eje
		/// sobre el que se arrastra. Vive acá porque depende de la orientación, igual que
		/// todo lo demás de esta clase.
		/// </summary>
		public abstract Cursor DragCursor { get; }

		/// <summary>
		/// Rectángulo de recorte para la capa clara del valor (inversión OnBar): la
		/// porción "llena" a lo largo del eje principal.
		/// </summary>
		public abstract Rect GetFillClip(Size barSize, double ratio);
	}

	/// <summary>Eje horizontal: el valor crece hacia la derecha (X).</summary>
	internal sealed class HorizontalAxis : OrientationAxis
	{
		public override double GetExtent(Size barSize) => barSize.Width;

		public override double PositionToRatio(Point p, Size barSize) =>
			barSize.Width <= 0 ? 0 : Math.Clamp(p.X / barSize.Width, 0, 1);

		public override void ApplyFill(FrameworkElement fill, Size barSize, double ratio)
		{
			// Eje principal: la porción que representa el valor. Eje cruzado: completo.
			// El alto se fija por código porque el rectángulo vive dentro de un Canvas
			// (ver Generic.xaml), que no estira a sus hijos.
			fill.Width = barSize.Width * ratio;
			fill.Height = barSize.Height;
		}

		public override Rect GetFillClip(Size barSize, double ratio) =>
			new Rect(0, 0, barSize.Width * ratio, barSize.Height);

		// SizeWE (oeste-este) y no Hand: la mano es la convención de "hipervínculo, esto se
		// activa con un click" y calla la mitad del gesto. La flecha doble dice sobre qué
		// eje se arrastra, que es la misma regla que ya usa el casillero del valor con
		// SizeNS. (El nombre de WPF es SizeWE; "SizeEW" no existe.)
		public override Cursor DragCursor => Cursors.SizeWE;
	}
}
