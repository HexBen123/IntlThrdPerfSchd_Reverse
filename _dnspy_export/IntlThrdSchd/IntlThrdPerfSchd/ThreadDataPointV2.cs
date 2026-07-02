using System;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000015 RID: 21
	public class ThreadDataPointV2
	{
		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000164 RID: 356 RVA: 0x00014C1D File Offset: 0x00012E1D
		// (set) Token: 0x06000165 RID: 357 RVA: 0x00014C25 File Offset: 0x00012E25
		public double UserModeInstructionRatio { get; set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000166 RID: 358 RVA: 0x00014C2E File Offset: 0x00012E2E
		// (set) Token: 0x06000167 RID: 359 RVA: 0x00014C36 File Offset: 0x00012E36
		public double CoreIPC { get; set; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000168 RID: 360 RVA: 0x00014C3F File Offset: 0x00012E3F
		// (set) Token: 0x06000169 RID: 361 RVA: 0x00014C47 File Offset: 0x00012E47
		public double MemPressure { get; set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x0600016A RID: 362 RVA: 0x00014C50 File Offset: 0x00012E50
		// (set) Token: 0x0600016B RID: 363 RVA: 0x00014C58 File Offset: 0x00012E58
		public double CodeFootprint { get; set; }
	}
}
