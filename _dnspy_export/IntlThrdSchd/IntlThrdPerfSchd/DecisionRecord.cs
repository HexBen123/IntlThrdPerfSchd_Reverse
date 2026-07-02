using System;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200000D RID: 13
	public struct DecisionRecord
	{
		// Token: 0x060000AF RID: 175 RVA: 0x00007EE0 File Offset: 0x000060E0
		public DecisionRecord(int maxCores)
		{
			this.ThreadFeatures = new float[8];
			this.CoreFeatures = new float[maxCores][];
			for (int i = 0; i < maxCores; i++)
			{
				this.CoreFeatures[i] = new float[9];
			}
			this.NumCores = 0;
			this.SelectedCore = -1;
			this.Reward = 0f;
			this.Timestamp = 0L;
			this.PredictedCore = -1;
			this.ActualCore = -1;
			this.IsValid = true;
		}

		// Token: 0x040000DA RID: 218
		public float[] ThreadFeatures;

		// Token: 0x040000DB RID: 219
		public float[][] CoreFeatures;

		// Token: 0x040000DC RID: 220
		public int NumCores;

		// Token: 0x040000DD RID: 221
		public int SelectedCore;

		// Token: 0x040000DE RID: 222
		public float Reward;

		// Token: 0x040000DF RID: 223
		public long Timestamp;

		// Token: 0x040000E0 RID: 224
		public int PredictedCore;

		// Token: 0x040000E1 RID: 225
		public int ActualCore;

		// Token: 0x040000E2 RID: 226
		public bool IsValid;
	}
}
