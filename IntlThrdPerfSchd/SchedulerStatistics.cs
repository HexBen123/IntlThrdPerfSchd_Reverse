using System;
using System.Text;

namespace IntlThrdPerfSchd
{
	public class SchedulerStatistics
	{
		public const int NUM_CORE_ENCODER_LAYERS = 3;

		public const int NUM_CROSS_ATTN_LAYERS = 2;

		public long TotalDecisions;

		public long TotalInferenceTimeUs;

		public long TotalThreadEncodeTimeUs;

		public long TotalCoreEncodeTimeUs;

		public long TotalCrossAttnTimeUs;

		public int[] CoreSelectionCounts;

		public float AvgInferenceTimeUs;

		public float AvgThreadEncodeTimeUs;

		public float AvgCoreEncodeTimeUs;

		public float AvgCrossAttnTimeUs;

		public float RecentAvgReward;

		public int CacheHits;

		public int CacheMisses;

		public int LearningUpdates;

		public int ExperienceUsed;

		public int ExperienceSkipped;

		public float AvgLoss;

		public int MigrationCount;

		public float AvgTAT;

		public float MinTAT;

		public float MaxTAT;

		public long TotalTATSamples;

		public float AvgEnergy;

		public float MinEnergy;

		public float MaxEnergy;

		public long TotalEnergySamples;

		public float BaselineEnergy;

		public float EnergyTrend;

		public float LastEnergy;

		public float AvgNewMetric;

		public float MinNewMetric;

		public float MaxNewMetric;

		public long TotalNewMetricSamples;

		public float BaselineNewMetric;

		public float NewMetricTrend;

		public float LastNewMetric;

		public float AvgExtraMetric;

		public float MinExtraMetric;

		public float MaxExtraMetric;

		public long TotalExtraMetricSamples;

		public float BaselineExtraMetric;

		public float ExtraMetricTrend;

		public float LastExtraMetric;

		public float AvgLoadBalance;

		public float MinLoadBalance;

		public float MaxLoadBalance;

		public long TotalLoadBalanceSamples;

		public float BaselineLoadBalance;

		public float LoadBalanceTrend;

		public float LastLoadBalance;

		public float AvgLoadBalance2;

		public float MinLoadBalance2;

		public float MaxLoadBalance2;

		public long TotalLoadBalance2Samples;

		public float BaselineLoadBalance2;

		public float LoadBalance2Trend;

		public float LastLoadBalance2;

		public float RewardTrend;

		public float TATTrend;

		public float PolicyEntropy;

		public float PositiveRewardRatio;

		public float LastReward;

		public float LastTATDelta;

		private const int REWARD_HISTORY_SIZE = 100;

		private const int TAT_HISTORY_SIZE = 100;

		private const int ENERGY_HISTORY_SIZE = 100;

		private const int NEW_METRIC_HISTORY_SIZE = 100;

		private const int EXTRA_METRIC_HISTORY_SIZE = 100;

		private const int LOAD_BALANCE_HISTORY_SIZE = 100;

		private const int LOAD_BALANCE2_HISTORY_SIZE = 100;

		private readonly float[] _rewardHistory;

		private readonly float[] _tatHistory;

		private readonly float[] _energyHistory;

		private readonly float[] _newMetricHistory;

		private readonly float[] _extraMetricHistory;

		private readonly float[] _loadBalanceHistory;

		private readonly float[] _loadBalance2History;

		private int _rewardIndex;

		private int _tatIndex;

		private int _energyIndex;

		private int _newMetricIndex;

		private int _extraMetricIndex;

		private int _loadBalanceIndex;

		private int _loadBalance2Index;

		private int _rewardCount;

		private int _tatCount;

		private int _energyCount;

		private int _newMetricCount;

		private int _extraMetricCount;

		private int _loadBalanceCount;

		private int _loadBalance2Count;

		private float _baselineTAT;

		public SchedulerStatistics(int maxCores)
		{
			CoreSelectionCounts = new int[maxCores];
			_rewardHistory = new float[100];
			_tatHistory = new float[100];
			_energyHistory = new float[100];
			_newMetricHistory = new float[100];
			_extraMetricHistory = new float[100];
			_loadBalanceHistory = new float[100];
			_loadBalance2History = new float[100];
		}

		public void RecordReward(float reward)
		{
			LastReward = reward;
			_rewardHistory[_rewardIndex] = reward;
			_rewardIndex = (_rewardIndex + 1) % 100;
			if (_rewardCount < 100)
			{
				_rewardCount++;
			}
			PositiveRewardRatio = UpdatePositiveRatio();
		}

		public void RecordTATDelta(float delta)
		{
			LastTATDelta = delta;
			_tatHistory[_tatIndex] = delta;
			_tatIndex = (_tatIndex + 1) % 100;
			if (_tatCount < 100)
			{
				_tatCount++;
			}
		}

		public void RecordEnergy(float energy)
		{
			LastEnergy = energy;
			_energyHistory[_energyIndex] = energy;
			_energyIndex = (_energyIndex + 1) % 100;
			if (_energyCount < 100)
			{
				_energyCount++;
			}
		}

		public void RecordNewMetric(float newMetric)
		{
			LastNewMetric = newMetric;
			_newMetricHistory[_newMetricIndex] = newMetric;
			_newMetricIndex = (_newMetricIndex + 1) % 100;
			if (_newMetricCount < 100)
			{
				_newMetricCount++;
			}
		}

		public void RecordExtraMetric(float extraMetric)
		{
			LastExtraMetric = extraMetric;
			_extraMetricHistory[_extraMetricIndex] = extraMetric;
			_extraMetricIndex = (_extraMetricIndex + 1) % 100;
			if (_extraMetricCount < 100)
			{
				_extraMetricCount++;
			}
		}

		public void RecordLoadBalance(float loadBalance)
		{
			LastLoadBalance = loadBalance;
			_loadBalanceHistory[_loadBalanceIndex] = loadBalance;
			_loadBalanceIndex = (_loadBalanceIndex + 1) % 100;
			if (_loadBalanceCount < 100)
			{
				_loadBalanceCount++;
			}
		}

		public void RecordLoadBalance2(float loadBalance2)
		{
			LastLoadBalance2 = loadBalance2;
			_loadBalance2History[_loadBalance2Index] = loadBalance2;
			_loadBalance2Index = (_loadBalance2Index + 1) % 100;
			if (_loadBalance2Count < 100)
			{
				_loadBalance2Count++;
			}
		}

		private float UpdatePositiveRatio()
		{
			if (_rewardCount == 0)
			{
				return 0f;
			}
			int num = 0;
			for (int i = 0; i < _rewardCount; i++)
			{
				if (_rewardHistory[i] > 0f)
				{
					num++;
				}
			}
			return (float)num / (float)_rewardCount;
		}

		public void ComputeTrends()
		{
			if (_rewardCount >= 10)
			{
				RewardTrend = ComputeTrend(_rewardHistory, _rewardCount);
			}
			if (_tatCount >= 10)
			{
				TATTrend = ComputeTrend(_tatHistory, _tatCount);
			}
			if (_energyCount >= 10)
			{
				EnergyTrend = ComputeTrend(_energyHistory, _energyCount);
			}
			if (_newMetricCount >= 10)
			{
				NewMetricTrend = ComputeTrend(_newMetricHistory, _newMetricCount);
			}
			if (_extraMetricCount >= 10)
			{
				ExtraMetricTrend = ComputeTrend(_extraMetricHistory, _extraMetricCount);
			}
			if (_loadBalanceCount >= 10)
			{
				LoadBalanceTrend = ComputeTrend(_loadBalanceHistory, _loadBalanceCount);
			}
			if (_loadBalance2Count >= 10)
			{
				LoadBalance2Trend = ComputeTrend(_loadBalance2History, _loadBalance2Count);
			}
		}

		private float ComputeTrend(float[] history, int count)
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			int num5 = ((count < 100) ? 0 : _rewardIndex);
			for (int i = 0; i < count; i++)
			{
				float num6 = i;
				float num7 = history[(num5 + i) % 100];
				num += num6;
				num2 += num7;
				num3 += num6 * num7;
				num4 += num6 * num6;
			}
			float num8 = count;
			float num9 = num8 * num4 - num * num;
			if (Math.Abs(num9) < 1E-08f)
			{
				return 0f;
			}
			return (num8 * num3 - num * num2) / num9;
		}

		public string GetReport(int numCores, float explorationRate = 0.1f)
		{
			ComputeTrends();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("=== Transformer Scheduler Statistics ===");
			stringBuilder.AppendLine("--- Inference Timing ---");
			stringBuilder.AppendLine($"Avg Inference Time: {((TotalDecisions > 0) ? ((float)TotalInferenceTimeUs / (float)TotalDecisions) : 0f):F2} μs");
			stringBuilder.AppendLine($"  Thread Encode: {((TotalDecisions > 0) ? ((float)TotalThreadEncodeTimeUs / (float)TotalDecisions) : 0f):F2} μs");
			stringBuilder.AppendLine($"  Core Encode:   {((TotalDecisions > 0) ? ((float)TotalCoreEncodeTimeUs / (float)TotalDecisions) : 0f):F2} μs ({3} layers)");
			stringBuilder.AppendLine($"  Cross Attn:    {((TotalDecisions > 0) ? ((float)TotalCrossAttnTimeUs / (float)TotalDecisions) : 0f):F2} μs ({2} layers)");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("--- Core Selection ---");
			stringBuilder.AppendLine($"Cache Hit Rate: {((CacheHits + CacheMisses > 0) ? ((float)CacheHits * 100f / (float)(CacheHits + CacheMisses)) : 0f):F1}%");
			stringBuilder.AppendLine($"Migrations: {MigrationCount} ({((TotalDecisions > 0) ? ((float)MigrationCount * 100f / (float)TotalDecisions) : 0f):F1}%)");
			stringBuilder.AppendLine($"Exploration Rate: {explorationRate:P1}");
			stringBuilder.AppendLine("Core Selection Distribution:");
			for (int i = 0; i < numCores; i++)
			{
				float num = ((TotalDecisions > 0) ? ((float)CoreSelectionCounts[i] * 100f / (float)TotalDecisions) : 0f);
				stringBuilder.AppendLine($"  Core {i}: {CoreSelectionCounts[i]} ({num:F1}%)");
			}
			return stringBuilder.ToString();
		}

		public float GetBaselineTAT()
		{
			return _baselineTAT;
		}

		public void UpdateBaselineTAT(float value)
		{
			_baselineTAT = value;
		}
	}
}
