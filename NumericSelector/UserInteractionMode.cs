namespace NumericSelector
{
	/// <summary>
	/// Modo de interacción del control: interactivo o sólo visualización.
	/// </summary>
	public enum UserInteractionMode
	{
		/// <summary>
		/// El control responde al mouse y al teclado como siempre.
		/// </summary>
		Interactive,

		/// <summary>
		/// Sólo visualización: el control conserva todo su aspecto y sigue reflejando los
		/// cambios que reciba por sus propiedades, pero no responde al mouse ni al teclado
		/// y no puede recibir el foco.
		/// </summary>
		ReadOnly,
	}
}