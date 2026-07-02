using System;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000014 RID: 20
	public struct SOMNeuron
	{
		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600015F RID: 351 RVA: 0x00014BEB File Offset: 0x00012DEB
		// (set) Token: 0x06000160 RID: 352 RVA: 0x00014BF3 File Offset: 0x00012DF3
		public int X { readonly get; set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000161 RID: 353 RVA: 0x00014BFC File Offset: 0x00012DFC
		// (set) Token: 0x06000162 RID: 354 RVA: 0x00014C04 File Offset: 0x00012E04
		public int Y { readonly get; set; }

		// Token: 0x06000163 RID: 355 RVA: 0x00014C0D File Offset: 0x00012E0D
		public SOMNeuron(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}
	}
}
