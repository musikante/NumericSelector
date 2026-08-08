namespace NumericSelector
{
	/// <summary>
	/// Lado (anclaje) de la caja del valor respecto de la caja con la que comparte fila:
	/// la barra cuando se queda arriba, o el detalle cuando desciende a la fila inferior.
	/// </summary>
	public enum ValueBoxDock
	{
		/// <summary>
		/// La caja del valor queda a la derecha de su compañero de fila (predeterminado).
		/// </summary>
		Right,

		/// <summary>
		/// La caja del valor queda a la izquierda de su compañero de fila.
		/// </summary>
		Left
	}
}