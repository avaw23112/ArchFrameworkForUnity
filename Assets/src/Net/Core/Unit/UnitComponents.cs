using Arch.Core;

namespace Arch.Net
{
	/// <summary>
	/// Unit marker components.
	/// </summary>
	public struct Unit : IComponent
	{
		public ulong UnitId;    // Align with NetworkEntityId; can be generated locally for non-network units
	}

	/// <summary>
	/// Unit marker components.
	///</summary>
	public struct UnitName : IComponent
	{
		public string Value;
	}
}


