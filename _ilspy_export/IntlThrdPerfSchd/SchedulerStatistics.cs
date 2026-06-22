using System;
using System.Text;

namespace IntlThrdPerfSchd;

public class SchedulerStatistics
{
	public long TotalDecisions;

	public long TotalInferenceTimeUs;

	public int[] CoreSelectionCounts;

	public float AvgInferenceTimeUs;

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

	public float RewardTrend;

	public float TATTrend;

	public float PolicyEntropy;

	public float PositiveRewardRatio;

	public float LastReward;

	public float LastTATDelta;

	private const int REWARD_HISTORY_SIZE = 100;

	private const int TAT_HISTORY_SIZE = 100;

	private const int ENERGY_HISTORY_SIZE = 100;

	private readonly float[] _rewardHistory;

	private readonly float[] _tatHistory;

	private readonly float[] _energyHistory;

	private int _rewardIndex;

	private int _tatIndex;

	private int _energyIndex;

	private int _rewardCount;

	private int _tatCount;

	private int _energyCount;

	private float _baselineTAT;

	public SchedulerStatistics(int maxCores)
	{
		CoreSelectionCounts = new int[maxCores];
		_rewardHistory = new float[100];
		_tatHistory = new float[100];
		_energyHistory = new float[100];
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
	}

	private float ComputeTrend(float[] history, int count)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		int num5 = ((count >= 100) ? _rewardIndex : 0);
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
