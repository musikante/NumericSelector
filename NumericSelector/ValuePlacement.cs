namespace NumericSelector
{
	/// <summary>
	/// Ubicación del número (Value) dentro del control. Las opciones son mutuamente
	/// excluyentes: el valor se muestra en un solo lugar.
	/// Los nombres dicen respecto de QUÉ se ubica el número, no hacia qué lado, para que
	/// sigan siendo válidos cuando el control tenga orientación vertical.
	///
	/// Las dos variantes de la fila del título llevan el marco en el nombre en vez de en una
	/// propiedad aparte, porque el marco de esa caja SÓLO se puede elegir ahí: junto a la
	/// barra la caja del valor forma parte del rectángulo del control y su marco no es
	/// opcional. Una propiedad suelta habría quedado inerte en dos de los cuatro valores;
	/// enumerándolas, la lista misma dice qué combinaciones existen.
	/// </summary>
	public enum ValuePlacement
	{
		/// <summary>Junto a la barra, en su propio casillero (clásico, por defecto).</summary>
		BesideBar,

		/// <summary>Superpuesto sobre la barra, que pasa a ocupar todo el largo (minimalista).</summary>
		OnBar,

		/// <summary>
		/// Compartiendo la línea del título, en una caja con su marco.
		/// Requiere que haya título; si no, se degrada a <see cref="BesideBar"/> por coerción
		/// y se restaura al volver a mostrarlo.
		/// </summary>
		WithTitleFramed,

		/// <summary>
		/// Compartiendo la línea del título, como una etiqueta suelta sin marco ni fondo.
		/// La etiqueta del título recupera entonces el lado que le había cedido a la caja,
		/// para que el contorno no quede abierto.
		/// Requiere que haya título, igual que <see cref="WithTitleFramed"/>.
		/// </summary>
		WithTitleFrameless
	}
}
