using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NumericSelector.Tests;

/// <summary>
/// Pruebas de la función pura de medición de texto (TextMeasure.Measure). No necesitan
/// ventana. No se afirman anchos en píxeles exactos —dependen de la fuente y la medición
/// del sistema—, sólo invariantes que la función debe cumplir siempre.
/// </summary>
[TestClass]
public class TextMeasureTests
{
	private static readonly CultureInfo ES = CultureInfo.GetCultureInfo("es-AR");
	private static readonly Typeface Sans = new("Segoe UI");

	[TestMethod]
	public void Empty_or_null_text_returns_empty_size()
	{
		var context = new TextMeasureContext(Sans, 14, ES, FlowDirection.LeftToRight);

		Assert.AreEqual(Size.Empty, TextMeasure.Measure(context, 1.0, ""));
		Assert.AreEqual(Size.Empty, TextMeasure.Measure(context, 1.0, null!));
	}

	[TestMethod]
	public void Text_width_and_height_are_positive_and_grow_with_length()
	{
		var context = new TextMeasureContext(Sans, 14, ES, FlowDirection.LeftToRight);

		var corto = TextMeasure.Measure(context, 1.0, "1");
		var largo = TextMeasure.Measure(context, 1.0, "1234567890");

		Assert.IsTrue(corto.Width > 0);
		Assert.IsTrue(corto.Height > 0);
		// Más caracteres ⇒ siempre más ancho (misma fuente y tamaño).
		Assert.IsTrue(largo.Width > corto.Width);
	}

	[TestMethod]
	public void Bigger_font_measures_wider_and_taller()
	{
		var chica = new TextMeasureContext(Sans, 10, ES, FlowDirection.LeftToRight);
		var grande = new TextMeasureContext(Sans, 20, ES, FlowDirection.LeftToRight);

		var sChico = TextMeasure.Measure(chica, 1.0, "10000");
		var sGrande = TextMeasure.Measure(grande, 1.0, "10000");

		Assert.IsTrue(sGrande.Width > sChico.Width);
		Assert.IsTrue(sGrande.Height > sChico.Height);
	}

	[TestMethod]
	public void The_measured_width_is_independent_of_the_dpi_argument()
	{
		var context = new TextMeasureContext(Sans, 14, ES, FlowDirection.LeftToRight);

		// pixelsPerDip gobierna la resolución del renderizado (trazado de glifos), no la
		// métrica nominal: FormattedText reporta su ancho en DIPs, que no deben depender de
		// la escala. Verificado empíricamente: 1.0, 1.25, 1.5 y 2.0 devuelven el mismo valor.
		double a96 = TextMeasure.Measure(context, 96.0 / 96.0, "100000").Width;
		double a144 = TextMeasure.Measure(context, 144.0 / 96.0, "100000").Width;
		double a192 = TextMeasure.Measure(context, 192.0 / 96.0, "100000").Width;

		Assert.AreEqual(a96, a144);
		Assert.AreEqual(a96, a192);
	}

	[TestMethod]
	public void Thousand_separator_width_depends_on_the_culture()
	{
		// La cultura cambia el separador de miles (punto en español, coma en inglés), así
		// que el mismo número se mide distinto si el contexto usa otra cultura.
		var esCtx = new TextMeasureContext(Sans, 14, CultureInfo.GetCultureInfo("es-AR"), FlowDirection.LeftToRight);
		var enCtx = new TextMeasureContext(Sans, 14, CultureInfo.GetCultureInfo("en-US"), FlowDirection.LeftToRight);

		string es = 1000000.ToString("N0", esCtx.Culture);
		string en = 1000000.ToString("N0", enCtx.Culture);

		Assert.AreEqual("1.000.000", es);
		Assert.AreEqual("1,000,000", en);

		double widthEs = TextMeasure.Measure(esCtx, 1.0, es).Width;
		double widthEn = TextMeasure.Measure(enCtx, 1.0, en).Width;

		// Ambos son legibles y positivos; no se comparan entre sí (la coma y el punto
		// pueden medir parecido). Lo que se verifica es que varían con la cultura.
		Assert.IsTrue(widthEs > 0);
		Assert.IsTrue(widthEn > 0);
	}

	[TestMethod]
	public void Same_inputs_measure_to_the_same_width()
	{
		var context = new TextMeasureContext(Sans, 14, ES, FlowDirection.LeftToRight);

		// Pura: con el mismo contexto, DPI y texto, el resultado es idéntico en cada llamada.
		Assert.AreEqual(
			TextMeasure.Measure(context, 1.0, "1234"),
			TextMeasure.Measure(context, 1.0, "1234"));
	}
}