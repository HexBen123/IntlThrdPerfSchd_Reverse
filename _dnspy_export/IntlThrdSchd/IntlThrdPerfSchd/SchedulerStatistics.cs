using System;
using System.Text;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200000F RID: 15
	public class SchedulerStatistics
	{
		// Token: 0x060000B5 RID: 181 RVA: 0x0000805C File Offset: 0x0000625C
		public SchedulerStatistics(int maxCores)
		{
			this.CoreSelectionCounts = new int[maxCores];
			this._rewardHistory = new float[100];
			this._tatHistory = new float[100];
			this._energyHistory = new float[100];
			this._newMetricHistory = new float[100];
			this._extraMetricHistory = new float[100];
			this._loadBalanceHistory = new float[100];
			this._loadBalance2History = new float[100];
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x000080D8 File Offset: 0x000062D8
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

		// Token: 0x060000B7 RID: 183 RVA: 0x0000812F File Offset: 0x0000632F
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

		// Token: 0x060000B8 RID: 184 RVA: 0x0000816F File Offset: 0x0000636F
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

		// Token: 0x060000B9 RID: 185 RVA: 0x000081AF File Offset: 0x000063AF
		public void RecordNewMetric(float newMetric)
		{
			this.LastNewMetric = newMetric;
			this._newMetricHistory[this._newMetricIndex] = newMetric;
			this._newMetricIndex = (this._newMetricIndex + 1) % 100;
			if (this._newMetricCount < 100)
			{
				this._newMetricCount++;
			}
		}

		// Token: 0x060000BA RID: 186 RVA: 0x000081EF File Offset: 0x000063EF
		public void RecordExtraMetric(float extraMetric)
		{
			this.LastExtraMetric = extraMetric;
			this._extraMetricHistory[this._extraMetricIndex] = extraMetric;
			this._extraMetricIndex = (this._extraMetricIndex + 1) % 100;
			if (this._extraMetricCount < 100)
			{
				this._extraMetricCount++;
			}
		}

		// Token: 0x060000BB RID: 187 RVA: 0x0000822F File Offset: 0x0000642F
		public void RecordLoadBalance(float loadBalance)
		{
			this.LastLoadBalance = loadBalance;
			this._loadBalanceHistory[this._loadBalanceIndex] = loadBalance;
			this._loadBalanceIndex = (this._loadBalanceIndex + 1) % 100;
			if (this._loadBalanceCount < 100)
			{
				this._loadBalanceCount++;
			}
		}

		// Token: 0x060000BC RID: 188 RVA: 0x0000826F File Offset: 0x0000646F
		public void RecordLoadBalance2(float loadBalance2)
		{
			this.LastLoadBalance2 = loadBalance2;
			this._loadBalance2History[this._loadBalance2Index] = loadBalance2;
			this._loadBalance2Index = (this._loadBalance2Index + 1) % 100;
			if (this._loadBalance2Count < 100)
			{
				this._loadBalance2Count++;
			}
		}

		// Token: 0x060000BD RID: 189 RVA: 0x000082B0 File Offset: 0x000064B0
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

		// Token: 0x060000BE RID: 190 RVA: 0x000082FC File Offset: 0x000064FC
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
			if (this._newMetricCount >= 10)
			{
				this.NewMetricTrend = this.ComputeTrend(this._newMetricHistory, this._newMetricCount);
			}
			if (this._extraMetricCount >= 10)
			{
				this.ExtraMetricTrend = this.ComputeTrend(this._extraMetricHistory, this._extraMetricCount);
			}
			if (this._loadBalanceCount >= 10)
			{
				this.LoadBalanceTrend = this.ComputeTrend(this._loadBalanceHistory, this._loadBalanceCount);
			}
			if (this._loadBalance2Count >= 10)
			{
				this.LoadBalance2Trend = this.ComputeTrend(this._loadBalance2History, this._loadBalance2Count);
			}
		}

		// Token: 0x060000BF RID: 191 RVA: 0x000083F8 File Offset: 0x000065F8
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

		// Token: 0x060000C0 RID: 192 RVA: 0x00008498 File Offset: 0x00006698
		public string GetReport(int numCores, float explorationRate = 0.1f)
		{
			this.ComputeTrends();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("=== Transformer Scheduler Statistics ===");
			stringBuilder.AppendLine("--- Inference Timing ---");
			stringBuilder.AppendLine(string.Format("Avg Inference Time: {0:F2} μs", (this.TotalDecisions > 0L) ? ((float)this.TotalInferenceTimeUs / (float)this.TotalDecisions) : 0f));
			stringBuilder.AppendLine(string.Format("  Thread Encode: {0:F2} μs", (this.TotalDecisions > 0L) ? ((float)this.TotalThreadEncodeTimeUs / (float)this.TotalDecisions) : 0f));
			stringBuilder.AppendLine(string.Format("  Core Encode:   {0:F2} μs ({1} layers)", (this.TotalDecisions > 0L) ? ((float)this.TotalCoreEncodeTimeUs / (float)this.TotalDecisions) : 0f, 3));
			stringBuilder.AppendLine(string.Format("  Cross Attn:    {0:F2} μs ({1} layers)", (this.TotalDecisions > 0L) ? ((float)this.TotalCrossAttnTimeUs / (float)this.TotalDecisions) : 0f, 2));
			stringBuilder.AppendLine();
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

		// Token: 0x060000C1 RID: 193 RVA: 0x000086D8 File Offset: 0x000068D8
		public float GetBaselineTAT()
		{
			return this._baselineTAT;
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x000086E0 File Offset: 0x000068E0
		public void UpdateBaselineTAT(float value)
		{
			this._baselineTAT = value;
		}

		// Token: 0x040000E7 RID: 231
		public const int NUM_CORE_ENCODER_LAYERS = 3;

		// Token: 0x040000E8 RID: 232
		public const int NUM_CROSS_ATTN_LAYERS = 2;

		// Token: 0x040000E9 RID: 233
		public long TotalDecisions;

		// Token: 0x040000EA RID: 234
		public long TotalInferenceTimeUs;

		// Token: 0x040000EB RID: 235
		public long TotalThreadEncodeTimeUs;

		// Token: 0x040000EC RID: 236
		public long TotalCoreEncodeTimeUs;

		// Token: 0x040000ED RID: 237
		public long TotalCrossAttnTimeUs;

		// Token: 0x040000EE RID: 238
		public int[] CoreSelectionCounts;

		// Token: 0x040000EF RID: 239
		public float AvgInferenceTimeUs;

		// Token: 0x040000F0 RID: 240
		public float AvgThreadEncodeTimeUs;

		// Token: 0x040000F1 RID: 241
		public float AvgCoreEncodeTimeUs;

		// Token: 0x040000F2 RID: 242
		public float AvgCrossAttnTimeUs;

		// Token: 0x040000F3 RID: 243
		public float RecentAvgReward;

		// Token: 0x040000F4 RID: 244
		public int CacheHits;

		// Token: 0x040000F5 RID: 245
		public int CacheMisses;

		// Token: 0x040000F6 RID: 246
		public int LearningUpdates;

		// Token: 0x040000F7 RID: 247
		public int ExperienceUsed;

		// Token: 0x040000F8 RID: 248
		public int ExperienceSkipped;

		// Token: 0x040000F9 RID: 249
		public float AvgLoss;

		// Token: 0x040000FA RID: 250
		public int MigrationCount;

		// Token: 0x040000FB RID: 251
		public float AvgTAT;

		// Token: 0x040000FC RID: 252
		public float MinTAT;

		// Token: 0x040000FD RID: 253
		public float MaxTAT;

		// Token: 0x040000FE RID: 254
		public long TotalTATSamples;

		// Token: 0x040000FF RID: 255
		public float AvgEnergy;

		// Token: 0x04000100 RID: 256
		public float MinEnergy;

		// Token: 0x04000101 RID: 257
		public float MaxEnergy;

		// Token: 0x04000102 RID: 258
		public long TotalEnergySamples;

		// Token: 0x04000103 RID: 259
		public float BaselineEnergy;

		// Token: 0x04000104 RID: 260
		public float EnergyTrend;

		// Token: 0x04000105 RID: 261
		public float LastEnergy;

		// Token: 0x04000106 RID: 262
		public float AvgNewMetric;

		// Token: 0x04000107 RID: 263
		public float MinNewMetric;

		// Token: 0x04000108 RID: 264
		public float MaxNewMetric;

		// Token: 0x04000109 RID: 265
		public long TotalNewMetricSamples;

		// Token: 0x0400010A RID: 266
		public float BaselineNewMetric;

		// Token: 0x0400010B RID: 267
		public float NewMetricTrend;

		// Token: 0x0400010C RID: 268
		public float LastNewMetric;

		// Token: 0x0400010D RID: 269
		public float AvgExtraMetric;

		// Token: 0x0400010E RID: 270
		public float MinExtraMetric;

		// Token: 0x0400010F RID: 271
		public float MaxExtraMetric;

		// Token: 0x04000110 RID: 272
		public long TotalExtraMetricSamples;

		// Token: 0x04000111 RID: 273
		public float BaselineExtraMetric;

		// Token: 0x04000112 RID: 274
		public float ExtraMetricTrend;

		// Token: 0x04000113 RID: 275
		public float LastExtraMetric;

		// Token: 0x04000114 RID: 276
		public float AvgLoadBalance;

		// Token: 0x04000115 RID: 277
		public float MinLoadBalance;

		// Token: 0x04000116 RID: 278
		public float MaxLoadBalance;

		// Token: 0x04000117 RID: 279
		public long TotalLoadBalanceSamples;

		// Token: 0x04000118 RID: 280
		public float BaselineLoadBalance;

		// Token: 0x04000119 RID: 281
		public float LoadBalanceTrend;

		// Token: 0x0400011A RID: 282
		public float LastLoadBalance;

		// Token: 0x0400011B RID: 283
		public float AvgLoadBalance2;

		// Token: 0x0400011C RID: 284
		public float MinLoadBalance2;

		// Token: 0x0400011D RID: 285
		public float MaxLoadBalance2;

		// Token: 0x0400011E RID: 286
		public long TotalLoadBalance2Samples;

		// Token: 0x0400011F RID: 287
		public float BaselineLoadBalance2;

		// Token: 0x04000120 RID: 288
		public float LoadBalance2Trend;

		// Token: 0x04000121 RID: 289
		public float LastLoadBalance2;

		// Token: 0x04000122 RID: 290
		public float RewardTrend;

		// Token: 0x04000123 RID: 291
		public float TATTrend;

		// Token: 0x04000124 RID: 292
		public float PolicyEntropy;

		// Token: 0x04000125 RID: 293
		public float PositiveRewardRatio;

		// Token: 0x04000126 RID: 294
		public float LastReward;

		// Token: 0x04000127 RID: 295
		public float LastTATDelta;

		// Token: 0x04000128 RID: 296
		private const int REWARD_HISTORY_SIZE = 100;

		// Token: 0x04000129 RID: 297
		private const int TAT_HISTORY_SIZE = 100;

		// Token: 0x0400012A RID: 298
		private const int ENERGY_HISTORY_SIZE = 100;

		// Token: 0x0400012B RID: 299
		private const int NEW_METRIC_HISTORY_SIZE = 100;

		// Token: 0x0400012C RID: 300
		private const int EXTRA_METRIC_HISTORY_SIZE = 100;

		// Token: 0x0400012D RID: 301
		private const int LOAD_BALANCE_HISTORY_SIZE = 100;

		// Token: 0x0400012E RID: 302
		private const int LOAD_BALANCE2_HISTORY_SIZE = 100;

		// Token: 0x0400012F RID: 303
		private readonly float[] _rewardHistory;

		// Token: 0x04000130 RID: 304
		private readonly float[] _tatHistory;

		// Token: 0x04000131 RID: 305
		private readonly float[] _energyHistory;

		// Token: 0x04000132 RID: 306
		private readonly float[] _newMetricHistory;

		// Token: 0x04000133 RID: 307
		private readonly float[] _extraMetricHistory;

		// Token: 0x04000134 RID: 308
		private readonly float[] _loadBalanceHistory;

		// Token: 0x04000135 RID: 309
		private readonly float[] _loadBalance2History;

		// Token: 0x04000136 RID: 310
		private int _rewardIndex;

		// Token: 0x04000137 RID: 311
		private int _tatIndex;

		// Token: 0x04000138 RID: 312
		private int _energyIndex;

		// Token: 0x04000139 RID: 313
		private int _newMetricIndex;

		// Token: 0x0400013A RID: 314
		private int _extraMetricIndex;

		// Token: 0x0400013B RID: 315
		private int _loadBalanceIndex;

		// Token: 0x0400013C RID: 316
		private int _loadBalance2Index;

		// Token: 0x0400013D RID: 317
		private int _rewardCount;

		// Token: 0x0400013E RID: 318
		private int _tatCount;

		// Token: 0x0400013F RID: 319
		private int _energyCount;

		// Token: 0x04000140 RID: 320
		private int _newMetricCount;

		// Token: 0x04000141 RID: 321
		private int _extraMetricCount;

		// Token: 0x04000142 RID: 322
		private int _loadBalanceCount;

		// Token: 0x04000143 RID: 323
		private int _loadBalance2Count;

		// Token: 0x04000144 RID: 324
		private float _baselineTAT;
	}
}
