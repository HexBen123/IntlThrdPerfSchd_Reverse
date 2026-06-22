using System;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000012 RID: 18
	public struct SOMNeuron
	{
		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000150 RID: 336 RVA: 0x000128A7 File Offset: 0x00010AA7
		// (set) Token: 0x06000151 RID: 337 RVA: 0x000128AF File Offset: 0x00010AAF
		public int X { readonly get; set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000152 RID: 338 RVA: 0x000128B8 File Offset: 0x00010AB8
		// (set) Token: 0x06000153 RID: 339 RVA: 0x000128C0 File Offset: 0x00010AC0
		public int Y { readonly get; set; }

		// Token: 0x06000154 RID: 340 RVA: 0x000128C9 File Offset: 0x00010AC9
		public SOMNeuron(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}
	}
}
