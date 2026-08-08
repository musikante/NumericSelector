namespace NumericSelector
{
	/// <summary>
	/// Side (dock) the value box takes relative to the box it shares its row with:
	/// the bar when it stays on top, or the detail when it drops to the bottom row.
	/// </summary>
	public enum ValueBoxDock
	{
		/// <summary>
		/// The value box sits to the right of its row partner (default).
		/// </summary>
		Right,

		/// <summary>
		/// The value box sits to the left of its row partner.
		/// </summary>
		Left
	}
}
