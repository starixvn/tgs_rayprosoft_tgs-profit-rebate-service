using System;

namespace Tgs.ProfitRebate.Enum;

[Flags]
public enum GameTag : byte
{
	None = 0,
	New = 1,
	Hot = 2,
	Recommend = 4
}
