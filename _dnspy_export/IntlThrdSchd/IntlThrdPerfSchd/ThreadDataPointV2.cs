using System;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000013 RID: 19
	public class ThreadDataPointV2
	{
		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000155 RID: 341 RVA: 0x000128D9 File Offset: 0x00010AD9
		// (set) Token: 0x06000156 RID: 342 RVA: 0x000128E1 File Offset: 0x00010AE1
		public double UserModeInstructionRatio { get; set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000157 RID: 343 RVA: 0x000128EA File Offset: 0x00010AEA
		// (set) Token: 0x06000158 RID: 344 RVA: 0x000128F2 File Offset: 0x00010AF2
		public double CoreIPC { get; set; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000159 RID: 345 RVA: 0x000128FB File Offset: 0x00010AFB
		// (set) Token: 0x0600015A RID: 346 RVA: 0x00012903 File Offset: 0x00010B03
		public double MemPressure { get; set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x0600015B RID: 347 RVA: 0x0001290C File Offset: 0x00010B0C
		// (set) Token: 0x0600015C RID: 348 RVA: 0x00012914 File Offset: 0x00010B14
		public double CodeFootprint { get; set; }
	}
}
