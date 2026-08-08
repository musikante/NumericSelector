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
	private static Thickness Resuelve(
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

		Assert.AreEqual(pixels, Resuelve(pixels, false, false, ValueBoxDock.Right, "Bar"));
		Assert.AreEqual(pixels, Resuelve(pixels, false, true, ValueBoxDock.Left, "Bar"));
		Assert.AreEqual(pixels, Resuelve(pixels, true, false, ValueBoxDock.Right, "Bar"));
		Assert.AreEqual(pixels, Resuelve(pixels, true, true, ValueBoxDock.Left, "Bar"));
	}

	// --- La fila de detalle (abajo) cede el borde superior a la barra ---

	[TestMethod]
	public void Detalle_cede_el_borde_superior_a_la_barra()
	{
		// La barra está arriba y dibuja la costura: el detalle no deja su parte superior.
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resuelve(pixels, true, false, ValueBoxDock.Right, "Detail"));
		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resuelve(pixels, true, true, ValueBoxDock.Left, "Detail"));
	}

	// --- Valor junto a la barra (no desciende) ---

	[TestMethod]
	public void Junto_a_la_barra_derecha_el_valor_cede_el_compartido_a_la_barra()
	{
		var pixels = new Thickness(2);

		// No desciende (down=false): conserva su parte superior. La barra dibuja el filo compartido.
		Assert.AreEqual(new Thickness(0, 2, 2, 2), Resuelve(pixels, false, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(2), Resuelve(pixels, false, true, ValueBoxDock.Right, "Bar"));
	}

	[TestMethod]
	public void Junto_a_la_barra_izquierda_el_valor_cede_el_compartido_a_la_barra()
	{
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2, 2, 0, 2), Resuelve(pixels, false, true, ValueBoxDock.Left, "Value"));
		Assert.AreEqual(new Thickness(2), Resuelve(pixels, false, true, ValueBoxDock.Left, "Bar"));
	}

	// --- Valor en la fila de detalle (desciende: ShowDetail && ValueFollowsDetail) ---

	[TestMethod]
	public void Valor_en_detalle_cede_su_superior_y_el_lado_interno()
	{
		var pixels = new Thickness(2);

		// Desciende a la fila del detalle: cede la parte superior (la dibuja la barra de
		// arriba) y el lado que mira a la etiqueta de detalle.
		Assert.AreEqual(new Thickness(0, 0, 2, 2), Resuelve(pixels, true, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(2), Resuelve(pixels, true, true, ValueBoxDock.Right, "Bar"));
	}

	[TestMethod]
	public void Valor_en_detalle_izquierda_cede_su_superior_y_el_lado_interno()
	{
		var pixels = new Thickness(2);

		Assert.AreEqual(new Thickness(2, 0, 0, 2), Resuelve(pixels, true, true, ValueBoxDock.Left, "Value"));
		Assert.AreEqual(new Thickness(2), Resuelve(pixels, true, true, ValueBoxDock.Left, "Bar"));
	}

	[TestMethod]
	public void Detalle_dibuja_el_lado_compartido()
	{
		var pixels = new Thickness(2);

		// La fila de detalle es marco fijo: lleva derecha/izquierda y base, y cede el superior.
		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resuelve(pixels, true, true, ValueBoxDock.Right, "Detail"));
		Assert.AreEqual(new Thickness(2, 0, 2, 2), Resuelve(pixels, true, true, ValueBoxDock.Left, "Detail"));
	}

	// --- Los lados ceden de a uno, sin fundir el grosor por lado ---

	[TestMethod]
	public void Grosor_asimetrico_pasa_lado_a_lado()
	{
		var pixels = new Thickness(1, 2, 3, 4);

		Assert.AreEqual(pixels, Resuelve(pixels, false, true, ValueBoxDock.Right, "Bar"));
		Assert.AreEqual(pixels, Resuelve(pixels, false, true, ValueBoxDock.Left, "Bar"));
		Assert.AreEqual(new Thickness(0, 2, 3, 4), Resuelve(pixels, false, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(1, 2, 0, 4), Resuelve(pixels, false, true, ValueBoxDock.Left, "Value"));
		Assert.AreEqual(new Thickness(1, 0, 3, 4), Resuelve(pixels, true, true, ValueBoxDock.Right, "Detail"));
		Assert.AreEqual(new Thickness(0, 0, 3, 4), Resuelve(pixels, true, true, ValueBoxDock.Right, "Value"));
		Assert.AreEqual(new Thickness(1, 0, 0, 4), Resuelve(pixels, true, true, ValueBoxDock.Left, "Value"));
	}
}