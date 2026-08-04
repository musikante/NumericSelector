namespace NumericSelector
{
	/// <summary>
	/// Ubicación del número (Value) dentro del control. Las tres opciones son
	/// mutuamente excluyentes: el valor se muestra en un solo lugar.
	/// Los nombres dicen respecto de QUÉ se ubica el número, no hacia qué lado, para que
	/// sigan siendo válidos cuando el control tenga orientación vertical.
	/// </summary>
	public enum ValuePlacement
	{
		/// <summary>Junto a la barra, en su propio casillero (clásico, por defecto).</summary>
		BesideBar,

		/// <summary>Superpuesto sobre la barra, que pasa a ocupar todo el largo (minimalista).</summary>
		OnBar,

		/// <summary>
		/// Compartiendo la línea del título. Requiere ShowTitleText; si el título no se
		/// muestra se degrada a BesideBar por coerción (y se restaura al reactivarlo).
		/// </summary>
		WithTitle
	}
}
