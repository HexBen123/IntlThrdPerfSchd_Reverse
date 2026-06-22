using System;
using System.Text;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200000D RID: 13
	public class SchedulerStatistics
	{
		// Token: 0x060000B5 RID: 181 RVA: 0x00008059 File Offset: 0x00006259
		public SchedulerStatistics(int maxCores)
		{
			this.CoreSelectionCounts = new int[maxCores];
			this._rewardHistory = new float[100];
			this._tatHistory = new float[100];
			this._energyHistory = new float[100];
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00008094 File Offset: 0x00006294
		public void RecordReward(float reward)
		{
			this.LastReward = reward;
			this._rewardHistory[this._rewardIndex] = reward;
			this._rewardIndex = (this._rewardIndex + 1) % 100;
			if (this._rewardCount < 100)
			{
				this._rewardCount++;
			}
			this.PositiveRewardRatio = this.UpdatePositiveRatio();
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x000080EB File Offset: 0x000062EB
		public void RecordTATDelta(float delta)
		{
			this.LastTATDelta = delta;
			this._tatHistory[this._tatIndex] = delta;
			this._tatIndex = (this._tatIndex + 1) % 100;
			if (this._tatCount < 100)
			{
				this._tatCount++;
			}
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x0000812B File Offset: 0x0000632B
		public void RecordEnergy(float energy)
		{
			this.LastEnergy = energy;
			this._energyHistory[this._energyIndex] = energy;
			this._energyIndex = (this._energyIndex + 1) % 100;
			if (this._energyCount < 100)
			{
				this._energyCount++;
			}
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x0000816C File Offset: 0x0000636C
		private float UpdatePositiveRatio()
		{
			if (this._rewardCount == 0)
			{
				return 0f;
			}
			int num = 0;
			for (int i = 0; i < this._rewardCount; i++)
			{
				if (this._rewardHistory[i] > 0f)
				{
					num++;
				}
			}
			return (float)num / (float)this._rewardCount;
		}

		// Token: 0x060000BA RID: 186 RVA: 0x000081B8 File Offset: 0x000063B8
		public void ComputeTrends()
		{
			if (this._rewardCount >= 10)
			{
				this.RewardTrend = this.ComputeTrend(this._rewardHistory, this._rewardCount);
			}
			if (this._tatCount >= 10)
			{
				this.TATTrend = this.ComputeTrend(this._tatHistory, this._tatCount);
			}
			if (this._energyCount >= 10)
			{
				this.EnergyTrend = this.ComputeTrend(this._energyHistory, this._energyCount);
			}
		}

		// Token: 0x060000BB RID: 187 RVA: 0x0000822C File Offset: 0x0000642C
		private float ComputeTrend(float[] history, int count)
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			int num5 = ((count < 100) ? 0 : this._rewardIndex);
			for (int i = 0; i < count; i++)
			{
				float num6 = (float)i;
				float num7 = history[(num5 + i) % 100];
				num += num6;
				num2 += num7;
				num3 += num6 * num7;
				num4 += num6 * num6;
			}
			float num8 = (float)count;
			float num9 = num8 * num4 - num * num;
			if (Math.Abs(num9) < 1E-08f)
			{
				return 0f;
			}
			return (num8 * num3 - num * num2) / num9;
		}

		// Token: 0x060000BC RID: 188 RVA: 0x000082CC File Offset: 0x000064CC
		public string GetReport(int numCores, float explorationRate = 0.1f)
		{
			this.ComputeTrends();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("=== Transformer Scheduler Statistics ===");
			stringBuilder.AppendLine("--- Core Selection ---");
			stringBuilder.AppendLine(string.Format("Cache Hit Rate: {0:F1}%", (this.CacheHits + this.CacheMisses > 0) ? ((float)this.CacheHits * 100f / (float)(this.CacheHits + this.CacheMisses)) : 0f));
			stringBuilder.AppendLine(string.Format("Migrations: {0} ({1:F1}%)", this.MigrationCount, (this.TotalDecisions > 0L) ? ((float)this.MigrationCount * 100f / (float)this.TotalDecisions) : 0f));
			stringBuilder.AppendLine(string.Format("Exploration Rate: {0:P1}", explorationRate));
			stringBuilder.AppendLine("Core Selection Distribution:");
			for (int i = 0; i < numCores; i++)
			{
				float num = ((this.TotalDecisions > 0L) ? ((float)this.CoreSelectionCounts[i] * 100f / (float)this.TotalDecisions) : 0f);
				stringBuilder.AppendLine(string.Format("  Core {0}: {1} ({2:F1}%)", i, this.CoreSelectionCounts[i], num));
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060000BD RID: 189 RVA: 0x00008415 File Offset: 0x00006615
		public float GetBaselineTAT()
		{
			return this._baselineTAT;
		}

		// Token: 0x060000BE RID: 190 RVA: 0x0000841D File Offset: 0x0000661D
		public void UpdateBaselineTAT(float value)
		{
			this._baselineTAT = value;
		}

		// Token: 0x040000E2 RID: 226
		public long TotalDecisions;

		// Token: 0x040000E3 RID: 227
		public long TotalInferenceTimeUs;

		// Token: 0x040000E4 RID: 228
		public int[] CoreSelectionCounts;

		// Token: 0x040000E5 RID: 229
		public float AvgInferenceTimeUs;

		// Token: 0x040000E6 RID: 230
		public float RecentAvgReward;

		// Token: 0x040000E7 RID: 231
		public int CacheHits;

		// Token: 0x040000E8 RID: 232
		public int CacheMisses;

		// Token: 0x040000E9 RID: 233
		public int LearningUpdates;

		// Token: 0x040000EA RID: 234
		public int ExperienceUsed;

		// Token: 0x040000EB RID: 235
		public int ExperienceSkipped;

		// Token: 0x040000EC RID: 236
		public float AvgLoss;

		// Token: 0x040000ED RID: 237
		public int MigrationCount;

		// Token: 0x040000EE RID: 238
		public float AvgTAT;

		// Token: 0x040000EF RID: 239
		public float MinTAT;

		// Token: 0x040000F0 RID: 240
		public float MaxTAT;

		// Token: 0x040000F1 RID: 241
		public long TotalTATSamples;

		// Token: 0x040000F2 RID: 242
		public float AvgEnergy;

		// Token: 0x040000F3 RID: 243
		public float MinEnergy;

		// Token: 0x040000F4 RID: 244
		public float MaxEnergy;

		// Token: 0x040000F5 RID: 245
		public long TotalEnergySamples;

		// Token: 0x040000F6 RID: 246
		public float BaselineEnergy;

		// Token: 0x040000F7 RID: 247
		public float EnergyTrend;

		// Token: 0x040000F8 RID: 248
		public float LastEnergy;

		// Token: 0x040000F9 RID: 249
		public float RewardTrend;

		// Token: 0x040000FA RID: 250
		public float TATTrend;

		// Token: 0x040000FB RID: 251
		public float PolicyEntropy;

		// Token: 0x040000FC RID: 252
		public float PositiveRewardRatio;

		// Token: 0x040000FD RID: 253
		public float LastReward;

		// Token: 0x040000FE RID: 254
		public float LastTATDelta;

		// Token: 0x040000FF RID: 255
		private const int REWARD_HISTORY_SIZE = 100;

		// Token: 0x04000100 RID: 256
		private const int TAT_HISTORY_SIZE = 100;

		// Token: 0x04000101 RID: 257
		private const int ENERGY_HISTORY_SIZE = 100;

		// Token: 0x04000102 RID: 258
		private readonly float[] _rewardHistory;

		// Token: 0x04000103 RID: 259
		private readonly float[] _tatHistory;

		// Token: 0x04000104 RID: 260
		private readonly float[] _energyHistory;

		// Token: 0x04000105 RID: 261
		private int _rewardIndex;

		// Token: 0x04000106 RID: 262
		private int _tatIndex;

		// Token: 0x04000107 RID: 263
		private int _energyIndex;

		// Token: 0x04000108 RID: 264
		private int _rewardCount;

		// Token: 0x04000109 RID: 265
		private int _tatCount;

		// Token: 0x0400010A RID: 266
		private int _energyCount;

		// Token: 0x0400010B RID: 267
		private float _baselineTAT;
	}
}
