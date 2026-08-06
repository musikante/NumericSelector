using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelector.Tests;

/// <summary>
/// Pruebas de la función pura de la matriz de costuras (ValueBorderResolver.Resolve).
/// No necesitan ventana: la matriz es una función sin estado y ése es justamente el punto
/// de aislarla — cada configuración de (ShowTitle, ValueFollowsTitle, ValueBoxSide) debe
/// repartir los lados de las cuatro celdas sin que ningún filo se dibuje dos veces.
/// </summary>
[TestClass]
public class ValueBorderResolverTests
{
	private static Thickness Resuelve(
		Thickness pixels,
		bool showTitle,
		bool followTitle,
		ValueBoxSide side,
		string cell)
		=> ValueBorderResolver.Resolve(pixels, showTitle, followTitle, side, cell);

	// --- Valor al lado de la barra (VBUp=false) ---

	[TestMethod]
	public void Beside_bar_right_the_value_owns_all_sides_and_the_bar_yields_the_right()
	{
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2), Resuelve(pixels, false, true, ValueBoxSide.Right, "Value"));
		Assert.AreEqual(new Thickness(2, 2, 0, 2), Resuelve(pixels, false, true, ValueBoxSide.Right, "Bar"));
	}

	[TestMethod]
	public void Beside_bar_left_the_bar_yields_the_left_edge()
	{
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2), Resuelve(pixels, false, true, ValueBoxSide.Left, "Value"));
		Assert.AreEqual(new Thickness(0, 2, 2, 2), Resuelve(pixels, false, true, ValueBoxSide.Left, "Bar"));
	}

	[TestMethod]
	public void Title_without_following_keeps_its_right_edge_but_yields_the_bottom()
	{
		var pixels = new Thickness(2);

		// ShowTitle=true pero ValueFollowsTitle=false: el valor queda abajo, el título
		// abarca todo el ancho y sólo cede la base a la costura que dibuja la barra.
		Assert.AreEqual(new Thickness(2, 2, 2, 0),
			Resuelve(pixels, true, false, ValueBoxSide.Right, "Title"));
	}

	// --- Valor arriba, junto al título (VBUp=true) ---

	[TestMethod]
	public void Value_up_yields_its_bottom_and_the_bar_recovers_all_four_sides()
	{
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2, 2, 2, 0),
			Resuelve(pixels, true, true, ValueBoxSide.Right, "Value"));
		Assert.AreEqual(new Thickness(2),
			Resuelve(pixels, true, true, ValueBoxSide.Right, "Bar"));
	}

	[TestMethod]
	public void Value_up_right_the_title_yields_the_shared_edge_to_the_box()
	{
		var pixels = new Thickness(2);

		// El casillero queda a la derecha del título: dibuja el filo compartido (su lado
		// izquierdo) y el título le cede el derecho.
		Assert.AreEqual(new Thickness(2, 2, 0, 0),
			Resuelve(pixels, true, true, ValueBoxSide.Right, "Title"));
	}

	[TestMethod]
	public void Value_up_left_the_title_yields_the_left_edge_to_the_box()
	{
		var pixels = new Thickness(2);

		// El casillero queda a la izquierda del título: dibuja el filo compartido (su lado
		// derecho) y el título cede el izquierdo.
		Assert.AreEqual(new Thickness(0, 2, 2, 0),
			Resuelve(pixels, true, true, ValueBoxSide.Left, "Title"));
	}

	// --- Los lados ceden de a uno, sin fundir el grosor por lado ---

	[TestMethod]
	public void Asymmetric_thickness_passes_through_side_by_side()
	{
		// ControlBorderPixels asimétrico (Left, Top, Right, Bottom): el ceder no debe
		// tocar los otros lados, que conservan su grosor propio.
		var pixels = new Thickness(1, 2, 3, 4);

		Assert.AreEqual(new Thickness(1, 2, 0, 4),
			Resuelve(pixels, false, true, ValueBoxSide.Right, "Bar"));
		Assert.AreEqual(new Thickness(0, 2, 3, 4),
			Resuelve(pixels, false, true, ValueBoxSide.Left, "Bar"));
		Assert.AreEqual(new Thickness(1, 2, 3, 0),
			Resuelve(pixels, true, true, ValueBoxSide.Right, "Value"));
		Assert.AreEqual(new Thickness(1, 2, 0, 0),
			Resuelve(pixels, true, true, ValueBoxSide.Right, "Title"));
	}
}