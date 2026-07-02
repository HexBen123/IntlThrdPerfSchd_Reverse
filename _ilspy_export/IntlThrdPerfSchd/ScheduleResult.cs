using System;

namespace IntlThrdPerfSchd;

public struct ScheduleResult
{
	public int BestCoreIndex;

	public IntPtr AffinityMask;
}
