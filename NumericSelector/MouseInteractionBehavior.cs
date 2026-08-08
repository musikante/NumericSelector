namespace NumericSelector
{
	/// <summary>
	/// Qué hace falta para que los gestos de mouse cambien el valor.
	/// </summary>
	public enum MouseInteractionBehavior
	{
		/// <summary>
		/// Los gestos de mouse actúan siempre, tenga el control el foco o no. Es el
		/// comportamiento directo, el de menos fricción.
		/// </summary>
		ChangeOnClick,

		/// <summary>
		/// El control tiene que tener el foco para que los gestos de mouse cambien el
		/// valor. No importa de dónde vino el foco (tabulación, código o un click): si ya
		/// lo tenía, el gesto actúa normalmente. Cuando el foco llega **por un click sobre
		/// el control**, ese primer click sólo enfoca y no toca el valor.
		/// Vale para todos los gestos de mouse por igual: click y arrastre sobre la barra,
		/// click derecho por zonas, doble-click de reset y arrastre vertical sobre el número.
		/// (La rueda ya exigía foco en los dos modos.)
		/// </summary>
		MustFocusFirst,

		// Futuros: ChangeOnHover, ChangeOnDoubleClick, etc.
	}
}