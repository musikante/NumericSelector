using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelector.Tests;

/// <summary>
/// Pruebas de la función pura de la matriz de costuras (ValueBorderResolver.Resolve).
/// No necesitan ventana: la matriz es una función sin estado y ése es justamente el punto
/// de aislarla — cada configuración de (ShowDetail, ValueFollowsDetail, ValueBoxDock) debe
/// repartir los lados de las celdas del control sin que ningún filo se dibuje dos veces.
/// </summary>
[TestClass]
public class ValueBorderResolverTests
{
	private static Thickness Resolve(
		Thickness pixels,
		bool showDetail,
		bool followsDetail,
		ValueBoxDock side,
		string cell)
		=> ValueBorderResolver.Resolve(pixels, showDetail, followsDetail, side, cell);

	// --- Marco fijo: la barra (arriba) no depende de la posición ---

	[TestMethod]
	public void The_bar_always_draws_its_four_sides()
	{
		var pixels = new Thickness(2);

		Assert.AreEqual(pixels, Resolve(pixels, false, false, ValueBoxDock.Right, "Bar"));
		Assert.AreEqual(pixels, Resolve(pixels, false, true, ValueBoxDock.Left, "Bar"));
		Assert.AreEqual(pixels, Resolve(pixels, true, false, ValueBoxDock.Right, "Bar"));
		Assert.AreEqual(pixels, Resolve(pixels, true, true, ValueBoxDock.Left, "Bar"));
	}

	// --- La fila de detalle (abajo) cede el borde superior a la barra ---

	[TestMethod]
	public void The_detail_row_yields_its_top_edge_to_the_bar()
	{
		// La barra está arriba y dibuja la costura: el detalle no deja su parte superior.
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resolve(pixels, true, false, ValueBoxDock.Right, "Detail"));
		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resolve(pixels, true, true, ValueBoxDock.Left, "Detail"));
	}

	// --- Valor junto a la barra (no desciende) ---

	[TestMethod]
	public void Docked_right_the_value_yields_the_shared_edge_to_the_bar()
	{
		var pixels = new Thickness(2);

		// No desciende (down=false): conserva su parte superior. La barra dibuja el filo compartido.
		Assert.AreEqual(new Thickness(0, 2, 2, 2), Resolve(pixels, false, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(2), Resolve(pixels, false, true, ValueBoxDock.Right, "Bar"));
	}

	[TestMethod]
	public void Docked_left_the_value_yields_the_shared_edge_to_the_bar()
	{
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2, 2, 0, 2), Resolve(pixels, false, true, ValueBoxDock.Left, "Value"));
		Assert.AreEqual(new Thickness(2), Resolve(pixels, false, true, ValueBoxDock.Left, "Bar"));
	}

	// --- Valor en la fila de detalle (desciende: ShowDetail && ValueFollowsDetail) ---

	[TestMethod]
	public void Value_in_the_detail_row_yields_its_top_and_its_inner_side()
	{
		var pixels = new Thickness(2);

		// Desciende a la fila del detalle: cede la parte superior (la dibuja la barra de
		// arriba) y el lado que mira a la etiqueta de detalle.
		Assert.AreEqual(new Thickness(0, 0, 2, 2), Resolve(pixels, true, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(2), Resolve(pixels, true, true, ValueBoxDock.Right, "Bar"));
	}

	[TestMethod]
	public void Value_in_the_detail_row_docked_left_yields_its_top_and_its_inner_side()
	{
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2, 0, 0, 2), Resolve(pixels, true, true, ValueBoxDock.Left, "Value"));
		Assert.AreEqual(new Thickness(2), Resolve(pixels, true, true, ValueBoxDock.Left, "Bar"));
	}

	[TestMethod]
	public void The_detail_row_draws_the_shared_side()
	{
		var pixels = new Thickness(2);

		// La fila de detalle es marco fijo: lleva derecha/izquierda y base, y cede el superior.
		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resolve(pixels, true, true, ValueBoxDock.Right, "Detail"));
		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resolve(pixels, true, true, ValueBoxDock.Left, "Detail"));
	}

	// --- Los lados ceden de a uno, sin fundir el grosor por lado ---

	[TestMethod]
	public void An_asymmetric_thickness_is_carried_side_by_side()
	{
		var pixels = new Thickness(1, 2, 3, 4);

		Assert.AreEqual(pixels, Resolve(pixels, false, true, ValueBoxDock.Right, "Bar"));
		Assert.AreEqual(pixels, Resolve(pixels, false, true, ValueBoxDock.Left, "Bar"));
		Assert.AreEqual(new Thickness(0, 2, 3, 4), Resolve(pixels, false, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(1, 2, 0, 4), Resolve(pixels, false, true, ValueBoxDock.Left, "Value"));
		Assert.AreEqual(new Thickness(1, 0, 3, 4), Resolve(pixels, true, true, ValueBoxDock.Right, "Detail"));
		Assert.AreEqual(new Thickness(0, 0, 3, 4), Resolve(pixels, true, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(1, 0, 0, 4), Resolve(pixels, true, true, ValueBoxDock.Left, "Value"));
	}

	// --- Una celda que no existe es un error, no un valor plausible ---

	[TestMethod]
	public void An_unknown_cell_is_not_taken_for_a_valid_one()
	{
		// El nombre de la celda es una cadena escrita a mano en el ConverterParameter de la
		// plantilla. Si un typo devolviera el Thickness de otra celda, el marco saldría mal
		// dibujado sin ningún aviso; por eso protesta.
		Assert.ThrowsExactly<ArgumentException>(
			() => Resolve(new Thickness(2), true, true, ValueBoxDock.Right, "Detai"));

		// La cadena vacía tampoco: es lo que llega si falta el ConverterParameter.
		Assert.ThrowsExactly<ArgumentException>(
			() => Resolve(new Thickness(2), false, false, ValueBoxDock.Right, ""));
	}
}