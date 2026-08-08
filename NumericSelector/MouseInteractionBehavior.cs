namespace NumericSelector
{
	/// <summary>
	/// What it takes for mouse gestures to change the value.
	/// </summary>
	public enum MouseInteractionBehavior
	{
		/// <summary>
		/// Mouse gestures always act, whether the control has the focus or not. This is the
		/// direct behavior, the one with the least friction.
		/// </summary>
		ChangeOnClick,

		/// <summary>
		/// The control has to have the focus for mouse gestures to change the value. Where
		/// the focus came from does not matter (tabbing, code or a click): if the control
		/// already had it, the gesture acts normally. When the focus arrives **through a
		/// click on the control**, that first click only focuses and leaves the value alone.
		/// This applies to every mouse gesture alike: click and drag on the bar, right click
		/// by zones, the reset double click and the vertical drag on the number.
		/// (The wheel already required focus in both modes.)
		/// </summary>
		MustFocusFirst,

		// Future ones: ChangeOnHover, ChangeOnDoubleClick, etc.
	}
}
