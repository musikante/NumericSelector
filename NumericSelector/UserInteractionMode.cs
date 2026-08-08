namespace NumericSelector
{
	/// <summary>
	/// Interaction mode of the control: interactive or display only.
	/// </summary>
	public enum UserInteractionMode
	{
		/// <summary>
		/// The control responds to mouse and keyboard as usual.
		/// </summary>
		Interactive,

		/// <summary>
		/// Display only: the control keeps its whole appearance and still reflects the
		/// changes it receives through its properties, but it does not respond to mouse
		/// or keyboard and cannot take the focus.
		/// </summary>
		ReadOnly,
	}
}
