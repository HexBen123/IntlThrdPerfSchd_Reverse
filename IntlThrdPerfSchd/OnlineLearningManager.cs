using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IntlThrdPerfSchd
{

public class OnlineLearningManager
{
	public class EnergyWindow
	{
		public DateTime StartTime;

		public DateTime EndTime;

		public List<RewardWindow> SubWindows = new List<RewardWindow>();

		public float EnergyEfficiency;

		public bool IsComplete;
	}

	public class RewardWindow
	{
		public DateTime StartTime;

		public DateTime EndTime;

		public float[][] StartState;

		public float[][] EndState;

		public List<SchedulingRecord> Decisions = new List<SchedulingRecord>();

		public float ImmediateReward;

		public float EnergyReward;

		public bool HasImmediateReward;

		public bool HasEnergyReward;
	}

	public class SchedulingRecord
	{
		public float[] ThreadFeatures;

		public float[][] CoreFeatures;

		public int SelectedCore;

		public DateTime Timestamp;

		public float ActionProb;

		public int ThreadId;

		public int? PreviousCore;

		public float ImmediatePortion;

		public float EnergyPortion;

		public float TimeWeight;

		public float SelectedCoreLLCMissRate;

		public float SelectedCoreQueueThreads;

		public float SelectedCoreQueueExecTime;

		public float SelectedCoreL1MissRateBefore;

		public float SelectedCoreL1MissRateAfter;

		public float SelectedCoreIPCBefore;

		public float SelectedCoreIPCAfter;

		public float PreviousIPC;

		public bool HasPreviousIPC;

		public float ExecutionTime;

		public float ActualIPC;

		public float CacheMissRate;

		public int? ActualCore;

		public bool HasExecutionResult;

		public float TotalReward => ImmediatePortion + EnergyPortion;
	}

	public class LearningStats
	{
		public int TotalDecisions;

		public int TotalUpdates;

		public float AverageImmediateReward;

		public float AverageEnergyReward;

		public int PendingRecords;

		public int ModelVersion;

		public int PendingEnergyWindows;

		public float AverageReward => (AverageImmediateReward + AverageEnergyReward) / 2f;
	}

	public enum ImmediateRewardMode
	{
		ExecutionBased,
		CoreStateBased,
		Combined
	}

	private readonly RealtimeScheduler _scheduler;

	private readonly List<SchedulingRecord> _allRecords;

	private readonly List<RewardWindow> _rewardWindows;

	private readonly List<EnergyWindow> _energyWindows;

	private readonly Dictionary<int, int> _threadLastCore;

	private readonly Dictionary<int, float> _threadLastIPC;

	private int _numCores;

	private bool _initialized;

	private RewardWindow _currentRewardWindow;

	private EnergyWindow _currentEnergyWindow;

	private DateTime _initializationTime;

	private bool _disableExploration;

	private int _totalDecisions;

	private int _totalUpdates;

	private float _totalImmediateReward;

	private float _totalEnergyReward;

	private int _energyRewardCount;

	private int _modelVersion;

	private Random _random;

	private int[] _coreSelectionCounts;

	private const string DefaultModelFileName = "scheduler_model.bin";

	public float LearningRate { get; set; } = 0.003f;

	public int BatchSize { get; set; } = 30;

	public int MaxTrainingBatchSize { get; set; } = 20;

	public int MaxRecords { get; set; } = 10000;

	public float RewardWindowMs { get; set; } = 30f;

	public float EnergyWindowSeconds { get; set; } = 1f;

	public float EpsilonStart { get; set; } = 0.05f;

	public float EpsilonEnd { get; set; } = 0.01f;

	public int EpsilonDecaySteps { get; set; } = 10000;

	public float ImmediateRewardWeight { get; set; }

	public float EnergyRewardWeight { get; set; } = 1f;

	public float IPCImprovementRewardWeight { get; set; } = 2f;

	public float OptimalCoreRewardWeight { get; set; }

	public float BaseRewardValue { get; set; } = 0.5f;

	public ImmediateRewardMode CurrentImmediateRewardMode { get; set; } = ImmediateRewardMode.Combined;

	public float CoreStateWeight { get; set; } = 0.5f;

	public float ExecutionWeight { get; set; } = 0.5f;

	public float LLCMissRateRewardWeight { get; set; } = 0.5f;

	public float QueueThreadsRewardWeight { get; set; } = 0.5f;

	public float LoadBalanceRewardWeight { get; set; } = 0.2f;

	public float StabilityRewardWeight { get; set; } = 0.15f;

	public float CoreConsistencyPenalty { get; set; } = 0.3f;

	public bool EnableImmediateReward { get; set; }

	public bool EnableEnergyReward { get; set; } = true;

	public float AttentionTemperature
	{
		get
		{
			return _scheduler.AttentionTemperature;
		}
		set
		{
			_scheduler.AttentionTemperature = value;
		}
	}

	public double ExplorationDurationMinutes { get; set; } = 10.0;

	public bool IsExploring
	{
		get
		{
			if (_disableExploration)
			{
				return false;
			}
			return (DateTime.Now - _initializationTime).TotalMinutes < ExplorationDurationMinutes;
		}
	}

	public bool IsInitialized => _initialized;

	public OnlineLearningManager(RealtimeScheduler scheduler, int numCores, int seed = 42)
	{
		_scheduler = scheduler ?? throw new ArgumentNullException("scheduler");
		_numCores = numCores;
		_allRecords = new List<SchedulingRecord>();
		_rewardWindows = new List<RewardWindow>();
		_energyWindows = new List<EnergyWindow>();
		_random = new Random(seed);
		_coreSelectionCounts = new int[numCores];
		_initialized = true;
		_initializationTime = DateTime.Now;
		InitializeWindows();
	}

	public OnlineLearningManager(RealtimeScheduler scheduler, int seed = 42)
	{
		_scheduler = scheduler ?? throw new ArgumentNullException("scheduler");
		_allRecords = new List<SchedulingRecord>();
		_rewardWindows = new List<RewardWindow>();
		_energyWindows = new List<EnergyWindow>();
		_threadLastCore = new Dictionary<int, int>();
		_threadLastIPC = new Dictionary<int, float>();
		_random = new Random(seed);
		_initializationTime = DateTime.Now;
	}

	public void SetNumCores(int numCores)
	{
		if (_initialized)
		{
			throw new InvalidOperationException("OnlineLearningManager already initialized");
		}
		if (numCores <= 0)
		{
			throw new ArgumentException("numCores must be positive", "numCores");
		}
		_numCores = numCores;
		_coreSelectionCounts = new int[numCores];
		_initialized = true;
		InitializeWindows();
	}

	private void InitializeWindows()
	{
		DateTime now = DateTime.Now;
		_currentEnergyWindow = new EnergyWindow
		{
			StartTime = now,
			EndTime = now.AddSeconds(EnergyWindowSeconds)
		};
		_energyWindows.Add(_currentEnergyWindow);
		_currentRewardWindow = new RewardWindow
		{
			StartTime = now,
			EndTime = now.AddMilliseconds(RewardWindowMs)
		};
		_rewardWindows.Add(_currentRewardWindow);
		_currentEnergyWindow.SubWindows.Add(_currentRewardWindow);
	}

	public int Schedule(float[] threadFeatures, float[][] coreFeatures, int threadId = 0)
	{
		if (!_initialized)
		{
			throw new InvalidOperationException("OnlineLearningManager not initialized. Call SetNumCores first.");
		}
		CheckAndRotateWindows();
		float[] probs = GetActionProbs(threadFeatures, coreFeatures);
		float epsilon = GetCurrentEpsilon();
		int selectedCore;
		float actionProb;
		if (_random.NextDouble() < (double)epsilon)
		{
			selectedCore = _random.Next(_numCores);
			actionProb = 1f / (float)_numCores;
		}
		else
		{
			selectedCore = 0;
			float maxProb = probs[0];
			for (int i = 1; i < _numCores; i++)
			{
				if (probs[i] > maxProb)
				{
					maxProb = probs[i];
					selectedCore = i;
				}
			}
			actionProb = probs[selectedCore];
		}
		int? previousCore = null;
		if (threadId != 0 && _threadLastCore.TryGetValue(threadId, out var lastCore))
		{
			previousCore = lastCore;
		}
		float previousIPC = 0f;
		bool hasPreviousIPC = false;
		if (threadId != 0 && _threadLastIPC.TryGetValue(threadId, out var lastIPC))
		{
			previousIPC = lastIPC;
			hasPreviousIPC = true;
		}
		SchedulingRecord record = new SchedulingRecord
		{
			ThreadFeatures = (float[])threadFeatures.Clone(),
			CoreFeatures = CloneCoreFeatures(coreFeatures),
			SelectedCore = selectedCore,
			Timestamp = DateTime.Now,
			ActionProb = actionProb,
			ThreadId = threadId,
			PreviousCore = previousCore,
			PreviousIPC = previousIPC,
			HasPreviousIPC = hasPreviousIPC,
			SelectedCoreLLCMissRate = coreFeatures[selectedCore][3],
			SelectedCoreQueueThreads = coreFeatures[selectedCore][2],
			SelectedCoreQueueExecTime = coreFeatures[selectedCore][1],
			SelectedCoreL1MissRateBefore = coreFeatures[selectedCore][3],
			SelectedCoreIPCBefore = coreFeatures[selectedCore][4]
		};
		if (threadId != 0)
		{
			_threadLastCore[threadId] = selectedCore;
		}
		lock (_allRecords)
		{
			_allRecords.Add(record);
			_currentRewardWindow.Decisions.Add(record);
			while (_allRecords.Count > MaxRecords)
			{
				_allRecords.RemoveAt(0);
			}
		}
		_totalDecisions++;
		_coreSelectionCounts[selectedCore]++;
		return selectedCore;
	}

	public void OnThreadComplete(int decisionCore, float executionTime, float actualIPC, float cacheMissRate, int? actualCore = null, float coreL1MissRateAfter = 0f, float coreIPCAfter = 0f)
	{
		SchedulingRecord record = null;
		lock (_allRecords)
		{
			for (int i = _allRecords.Count - 1; i >= 0; i--)
			{
				if (_allRecords[i].SelectedCore == decisionCore && !_allRecords[i].HasExecutionResult)
				{
					record = _allRecords[i];
					break;
				}
			}
			if (record == null)
			{
				for (int i2 = _allRecords.Count - 1; i2 >= 0; i2--)
				{
					if (!_allRecords[i2].HasExecutionResult)
					{
						record = _allRecords[i2];
						_ = record.SelectedCore;
						break;
					}
				}
			}
		}
		if (record == null)
		{
			return;
		}
		bool coreMigrated = actualCore.HasValue && actualCore.Value != record.SelectedCore;
		record.ExecutionTime = executionTime;
		record.ActualIPC = actualIPC;
		record.CacheMissRate = cacheMissRate;
		record.ActualCore = actualCore;
		record.SelectedCoreL1MissRateAfter = (coreMigrated ? record.SelectedCoreL1MissRateBefore : coreL1MissRateAfter);
		record.SelectedCoreIPCAfter = (coreMigrated ? record.SelectedCoreIPCBefore : coreIPCAfter);
		record.HasExecutionResult = true;
		if (record.ThreadId != 0)
		{
			_threadLastIPC[record.ThreadId] = actualIPC;
		}
		if (EnableImmediateReward)
		{
			float ipcImprovementReward = CalculateIPCImprovementReward(actualIPC, record.PreviousIPC, record.HasPreviousIPC, BaseRewardValue);
			record.ImmediatePortion = ImmediateRewardWeight * ipcImprovementReward;
			_totalImmediateReward += record.ImmediatePortion;
			if (coreMigrated)
			{
				record.ImmediatePortion *= 1f - CoreConsistencyPenalty;
			}
		}
		else if (EnableEnergyReward)
		{
			record.ImmediatePortion = BaseRewardValue;
		}
	}

	private float CalculateIPCImprovementReward(float actualIPC, float previousIPC, bool hasPreviousIPC, float baseReward)
	{
		if (!hasPreviousIPC)
		{
			return baseReward;
		}
		float improvement = (actualIPC - previousIPC) / Math.Max(previousIPC, 0.001f);
		if (improvement > 0f)
		{
			float bonus = Math.Min(improvement * 1f, 1f);
			return Math.Min(baseReward + bonus, 1f);
		}
		float penalty = Math.Abs(improvement) * 1f;
		return Math.Max(baseReward - penalty, 0f);
	}

	public bool UpdateEnergyReward(float energyEfficiency, bool forceUpdate = false)
	{
		if (float.IsNaN(energyEfficiency) || float.IsInfinity(energyEfficiency))
		{
			return false;
		}
		if (EnableEnergyReward)
		{
			_currentEnergyWindow.EnergyEfficiency = energyEfficiency;
			_currentEnergyWindow.IsComplete = true;
			DistributeEnergyReward(_currentEnergyWindow, energyEfficiency);
			DateTime now = DateTime.Now;
			_currentEnergyWindow = new EnergyWindow
			{
				StartTime = now,
				EndTime = now.AddSeconds(EnergyWindowSeconds)
			};
			_energyWindows.Add(_currentEnergyWindow);
		}
		int readyCount = 0;
		lock (_allRecords)
		{
			foreach (SchedulingRecord allRecord in _allRecords)
			{
				if (allRecord.HasExecutionResult)
				{
					readyCount++;
				}
			}
			if (readyCount >= BatchSize || forceUpdate)
			{
				UpdateModel();
				return true;
			}
		}
		return false;
	}

	public bool UpdateReward(float energyEfficiency, bool forceUpdate = false)
	{
		return UpdateEnergyReward(energyEfficiency, forceUpdate);
	}

	public LearningStats GetStats()
	{
		int pendingCount = 0;
		int readyCount = 0;
		lock (_allRecords)
		{
			foreach (SchedulingRecord allRecord in _allRecords)
			{
				if (allRecord.HasExecutionResult)
				{
					readyCount++;
				}
				else
				{
					pendingCount++;
				}
			}
		}
		return new LearningStats
		{
			TotalDecisions = _totalDecisions,
			TotalUpdates = _totalUpdates,
			AverageImmediateReward = ((_totalDecisions > 0) ? (_totalImmediateReward / (float)_totalDecisions) : 0f),
			AverageEnergyReward = ((_energyRewardCount > 0) ? (_totalEnergyReward / (float)_energyRewardCount) : 0f),
			PendingRecords = pendingCount,
			ModelVersion = _modelVersion,
			PendingEnergyWindows = _energyWindows.Count((EnergyWindow w) => !w.IsComplete)
		};
	}

	public string GetNetworkStatsString()
	{
		if (!_initialized)
		{
			return "OnlineLearningManager not initialized";
		}
		StringBuilder sb = new StringBuilder();
		LearningStats stats = GetStats();
		sb.AppendLine("========== 网络学习统计 ==========");
		sb.AppendLine($"模型版本: {stats.ModelVersion}");
		sb.AppendLine($"总决策数: {stats.TotalDecisions}");
		sb.AppendLine($"总更新数: {stats.TotalUpdates}");
		sb.AppendLine($"待处理记录: {stats.PendingRecords}");
		TimeSpan elapsed = DateTime.Now - _initializationTime;
		double remainingMinutes = Math.Max(0.0, ExplorationDurationMinutes - elapsed.TotalMinutes);
		sb.AppendLine($"当前探索率(Epsilon): {GetCurrentEpsilon():F4}");
		sb.AppendLine("探索状态: " + (IsExploring ? $"进行中 (剩余 {remainingMinutes:F1} 分钟)" : "已结束"));
		sb.AppendLine();
		sb.AppendLine("---------- 奖励统计 ----------");
		sb.AppendLine($"平均即时奖励: {stats.AverageImmediateReward:F4}");
		sb.AppendLine($"平均能效奖励: {stats.AverageEnergyReward:F4}");
		sb.AppendLine($"平均总奖励: {stats.AverageReward:F4}");
		sb.AppendLine($"即时奖励权重: {ImmediateRewardWeight:F2}");
		sb.AppendLine($"能效奖励权重: {EnergyRewardWeight:F2}");
		sb.AppendLine();
		sb.AppendLine("---------- 网络配置 ----------");
		sb.AppendLine($"核心数: {_numCores}");
		sb.AppendLine($"dModel: {128}");
		sb.AppendLine($"注意力头数: {8}");
		sb.AppendLine($"头维度: {16}");
		sb.AppendLine($"学习率: {LearningRate:F6}");
		sb.AppendLine($"批次大小: {BatchSize}");
		sb.AppendLine($"注意力温度: {AttentionTemperature:F2}");
		sb.AppendLine();
		float[] weights = _scheduler.GetAllWeights();
		float weightSum = 0f;
		float weightMax = float.MinValue;
		float weightMin = float.MaxValue;
		int nanCount = 0;
		int infCount = 0;
		float[] array = weights;
		foreach (float w in array)
		{
			if (float.IsNaN(w))
			{
				nanCount++;
				continue;
			}
			if (float.IsInfinity(w))
			{
				infCount++;
				continue;
			}
			weightSum += w;
			if (w > weightMax)
			{
				weightMax = w;
			}
			if (w < weightMin)
			{
				weightMin = w;
			}
		}
		sb.AppendLine("---------- 权重统计 ----------");
		sb.AppendLine($"总权重数: {weights.Length}");
		if (nanCount > 0 || infCount > 0)
		{
			sb.AppendLine($"[警告] NaN数量: {nanCount}, Infinity数量: {infCount}");
			sb.AppendLine("[建议] 模型已损坏，请重新初始化或加载备份模型");
		}
		else
		{
			sb.AppendLine($"权重均值: {weightSum / (float)weights.Length:F6}");
			sb.AppendLine($"权重最大: {weightMax:F6}");
			sb.AppendLine($"权重最小: {weightMin:F6}");
		}
		sb.AppendLine();
		sb.AppendLine("---------- 注意力头说明 ----------");
		sb.AppendLine("Head 0 (性能匹配): 关注IPC、指令数、优先级");
		sb.AppendLine("Head 1 (缓存匹配): 关注LLC miss、分支预测");
		sb.AppendLine("Head 2 (负载均衡): 关注队列深度、利用率");
		sb.AppendLine("Head 3 (能效优化): 关注功耗相关特征");
		sb.AppendLine("Head 4 (内存带宽): 关注内存访问模式");
		sb.AppendLine("Head 5 (计算密集): 关注计算密集型任务");
		sb.AppendLine("Head 6 (IO密集): 关注IO等待特征");
		sb.AppendLine("Head 7 (综合评估): 综合多维度特征");
		sb.AppendLine("================================");
		return sb.ToString();
	}

	public string GetCriticStatsString()
	{
		if (!_initialized)
		{
			return "OnlineLearningManager not initialized";
		}
		StringBuilder stringBuilder = new StringBuilder();
		GetStats();
		stringBuilder.AppendLine("================================");
		return stringBuilder.ToString();
	}

	public string GetLastAttentionWeightsString()
	{
		if (!_initialized)
		{
			return "OnlineLearningManager not initialized";
		}
		StringBuilder sb = new StringBuilder();
		float[][] attentionWeights = _scheduler.GetAttentionWeights(_numCores);
		sb.AppendLine("========== 注意力权重分布 ==========");
		string[] headNames = new string[8] { "性能匹配", "缓存匹配", "负载均衡", "能效优化", "内存带宽", "计算密集", "IO密集", "综合评估" };
		for (int h = 0; h < 8; h++)
		{
			sb.AppendLine($"--- Head {h} ({headNames[h]}) ---");
			float sum = 0f;
			float max = float.MinValue;
			int maxCore = 0;
			for (int c = 0; c < _numCores; c++)
			{
				sum += attentionWeights[h][c];
				if (attentionWeights[h][c] > max)
				{
					max = attentionWeights[h][c];
					maxCore = c;
				}
			}
			sb.Append("大核[0-7]: ");
			for (int i = 0; i < Math.Min(8, _numCores); i++)
			{
				sb.Append($"C{i}={attentionWeights[h][i]:F3} ");
			}
			sb.AppendLine();
			if (_numCores > 8)
			{
				sb.Append("小核[8+]: ");
				for (int j = 8; j < _numCores; j++)
				{
					sb.Append($"C{j}={attentionWeights[h][j]:F3} ");
					if ((j - 7) % 8 == 0 && j < _numCores - 1)
					{
						sb.AppendLine();
					}
					if (j > 8 && (j - 7) % 8 == 0)
					{
						sb.Append("         ");
					}
				}
				sb.AppendLine();
			}
			sb.AppendLine($"最大权重核心: C{maxCore} ({max:F4}), 权重和: {sum:F4}");
			sb.AppendLine();
		}
		sb.AppendLine("--- 聚合概率分布 ---");
		float[] probs = new float[_numCores];
		for (int k = 0; k < _numCores; k++)
		{
			for (int l = 0; l < 8; l++)
			{
				probs[k] += attentionWeights[l][k];
			}
		}
		float totalSum = 0f;
		float[] array = probs;
		foreach (float p in array)
		{
			totalSum += p;
		}
		if (totalSum > 0f)
		{
			for (int n = 0; n < _numCores; n++)
			{
				probs[n] /= totalSum;
			}
		}
		sb.Append("大核[0-7]: ");
		for (int num = 0; num < Math.Min(8, _numCores); num++)
		{
			sb.Append($"C{num}={probs[num] * 100f:F1}% ");
		}
		sb.AppendLine();
		if (_numCores > 8)
		{
			sb.Append("小核[8+]: ");
			for (int num2 = 8; num2 < _numCores; num2++)
			{
				sb.Append($"C{num2}={probs[num2] * 100f:F1}% ");
				if ((num2 - 7) % 8 == 0 && num2 < _numCores - 1)
				{
					sb.AppendLine();
				}
				if (num2 > 8 && (num2 - 7) % 8 == 0)
				{
					sb.Append("         ");
				}
			}
			sb.AppendLine();
		}
		int bestCore = 0;
		float bestProb = probs[0];
		for (int num3 = 1; num3 < _numCores; num3++)
		{
			if (probs[num3] > bestProb)
			{
				bestProb = probs[num3];
				bestCore = num3;
			}
		}
		sb.AppendLine($"推荐核心: C{bestCore} (概率: {bestProb * 100f:F1}%)");
		sb.AppendLine("====================================");
		return sb.ToString();
	}

	public string GetAttentionScoreStats()
	{
		if (!_initialized)
		{
			return "OnlineLearningManager not initialized";
		}
		return _scheduler.GetAttentionScoreStats(_numCores);
	}

	private void CheckAndRotateWindows()
	{
		DateTime now = DateTime.Now;
		if (now >= _currentRewardWindow.EndTime)
		{
			_currentRewardWindow.EndState = CaptureCoreState();
			_currentRewardWindow = new RewardWindow
			{
				StartTime = now,
				EndTime = now.AddMilliseconds(RewardWindowMs)
			};
			_rewardWindows.Add(_currentRewardWindow);
			_currentEnergyWindow.SubWindows.Add(_currentRewardWindow);
		}
	}

	private float[][] CaptureCoreState()
	{
		return new float[_numCores][];
	}

	private void DistributeEnergyReward(EnergyWindow energyWindow, float totalEnergyReward)
	{
		List<SchedulingRecord> allDecisions = new List<SchedulingRecord>();
		lock (_allRecords)
		{
			foreach (SchedulingRecord decision in _allRecords)
			{
				if (decision.HasExecutionResult && decision.EnergyPortion == 0f)
				{
					allDecisions.Add(decision);
				}
			}
		}
		if (allDecisions.Count != 0 && !float.IsNaN(totalEnergyReward) && !float.IsInfinity(totalEnergyReward))
		{
			float normalizedEnergyReward = (totalEnergyReward - -100f) / 200f;
			normalizedEnergyReward = Math.Max(0f, Math.Min(1f, normalizedEnergyReward));
			float totalWeight = 0f;
			float[] weights = new float[allDecisions.Count];
			for (int i = 0; i < allDecisions.Count; i++)
			{
				weights[i] = 1f;
				allDecisions[i].TimeWeight = weights[i];
				totalWeight += weights[i];
			}
			float totalRewardToDistribute = EnergyRewardWeight * normalizedEnergyReward * (float)allDecisions.Count;
			for (int j = 0; j < allDecisions.Count; j++)
			{
				allDecisions[j].EnergyPortion = weights[j] / totalWeight * totalRewardToDistribute;
				_totalEnergyReward += allDecisions[j].EnergyPortion;
				_energyRewardCount++;
			}
		}
	}

	private float[] GetActionProbs(float[] threadFeatures, float[][] coreFeatures)
	{
		_scheduler.Predict(threadFeatures, coreFeatures, _numCores);
		float[][] attentionWeights = _scheduler.GetAttentionWeights(_numCores);
		float[] probs = new float[_numCores];
		for (int c = 0; c < _numCores; c++)
		{
			for (int h = 0; h < 8; h++)
			{
				probs[c] += attentionWeights[h][c];
			}
		}
		float sum = 0f;
		for (int i = 0; i < _numCores; i++)
		{
			sum += probs[i];
		}
		if (sum > 0f)
		{
			for (int j = 0; j < _numCores; j++)
			{
				probs[j] /= sum;
			}
		}
		else
		{
			for (int k = 0; k < _numCores; k++)
			{
				probs[k] = 1f / (float)_numCores;
			}
		}
		return probs;
	}

	private float[] Softmax(float[] scores)
	{
		float[] probs = new float[scores.Length];
		float maxScore = scores[0];
		for (int i = 1; i < scores.Length; i++)
		{
			if (scores[i] > maxScore)
			{
				maxScore = scores[i];
			}
		}
		float sum = 0f;
		for (int j = 0; j < scores.Length; j++)
		{
			probs[j] = (float)Math.Exp(scores[j] - maxScore);
			sum += probs[j];
		}
		for (int k = 0; k < scores.Length; k++)
		{
			probs[k] /= sum;
		}
		return probs;
	}

	private float GetCurrentEpsilon()
	{
		if (_disableExploration)
		{
			return 0f;
		}
		if ((DateTime.Now - _initializationTime).TotalMinutes >= ExplorationDurationMinutes)
		{
			return 0f;
		}
		if (_totalDecisions >= EpsilonDecaySteps)
		{
			return EpsilonEnd;
		}
		float progress = (float)_totalDecisions / (float)EpsilonDecaySteps;
		return EpsilonStart + (EpsilonEnd - EpsilonStart) * progress;
	}

	private void UpdateModel()
	{
		if (_allRecords.Count == 0)
		{
			return;
		}
		List<SchedulingRecord> trainingBatch = new List<SchedulingRecord>();
		lock (_allRecords)
		{
			foreach (SchedulingRecord r in _allRecords)
			{
				if (r.HasExecutionResult)
				{
					trainingBatch.Add(r);
				}
			}
		}
		if (trainingBatch.Count == 0)
		{
			return;
		}
		if (trainingBatch.Count > MaxTrainingBatchSize)
		{
			trainingBatch = trainingBatch.GetRange(trainingBatch.Count - MaxTrainingBatchSize, MaxTrainingBatchSize);
		}
		float avgReward = 0f;
		foreach (SchedulingRecord r2 in trainingBatch)
		{
			avgReward += r2.TotalReward;
		}
		avgReward /= (float)trainingBatch.Count;
		_scheduler.ClearGradients();
		foreach (SchedulingRecord record in trainingBatch)
		{
			_scheduler.PredictWithCache(record.ThreadFeatures, record.CoreFeatures, _numCores);
			float advantage = record.TotalReward - avgReward;
			_scheduler.Backward(record.SelectedCore, advantage);
		}
		_scheduler.ApplyGradients(LearningRate, trainingBatch.Count);
		lock (_allRecords)
		{
			if (EnableEnergyReward)
			{
				_allRecords.RemoveAll((SchedulingRecord schedulingRecord) => schedulingRecord.EnergyPortion > 0f);
			}
			else
			{
				HashSet<SchedulingRecord> trainedSet = new HashSet<SchedulingRecord>(trainingBatch);
				_allRecords.RemoveAll((SchedulingRecord item) => trainedSet.Contains(item));
			}
		}
		CleanupOldWindows();
		_totalUpdates++;
		_modelVersion++;
	}

	private void CleanupOldWindows()
	{
		int keepWindows = 5;
		while (_energyWindows.Count > keepWindows && _energyWindows[0].IsComplete)
		{
			_energyWindows.RemoveAt(0);
		}
		while (_rewardWindows.Count > keepWindows * (int)(EnergyWindowSeconds * 1000f / RewardWindowMs))
		{
			_rewardWindows.RemoveAt(0);
		}
	}

	private float[][] CloneCoreFeatures(float[][] features)
	{
		float[][] clone = new float[features.Length][];
		for (int i = 0; i < features.Length; i++)
		{
			clone[i] = (float[])features[i].Clone();
		}
		return clone;
	}

	public bool IsModelCorrupted()
	{
		float[] allWeights = _scheduler.GetAllWeights();
		foreach (float w in allWeights)
		{
			if (float.IsNaN(w) || float.IsInfinity(w))
			{
				return true;
			}
		}
		return false;
	}

	public void ResetModel()
	{
		_scheduler.InitializeWeights();
		_modelVersion++;
		lock (_allRecords)
		{
			_allRecords.Clear();
		}
		_rewardWindows.Clear();
		_energyWindows.Clear();
		_totalDecisions = 0;
		_totalUpdates = 0;
		_totalImmediateReward = 0f;
		_totalEnergyReward = 0f;
		_energyRewardCount = 0;
		Array.Clear(_coreSelectionCounts, 0, _coreSelectionCounts.Length);
		InitializeWindows();
	}

	public void SaveModel()
	{
		SaveModel("scheduler_model.bin");
	}

	public void SaveModel(string filePath)
	{
		float[] weights = _scheduler.GetAllWeights();
		using BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
		writer.Write(2);
		writer.Write(weights.Length);
		float[] array = weights;
		foreach (float w in array)
		{
			writer.Write(w);
		}
		bool explorationEnded = !IsExploring;
		writer.Write(explorationEnded);
		writer.Write(_initializationTime.ToBinary());
		writer.Write(ExplorationDurationMinutes);
	}

	public void LoadModel()
	{
		LoadModel("scheduler_model.bin");
	}

	public void LoadModel(string filePath)
	{
		if (!File.Exists(filePath))
		{
			_scheduler.InitializeWeights();
			_modelVersion++;
			_initializationTime = DateTime.Now;
			_disableExploration = false;
			return;
		}
		using BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open));
		int fileVersion = reader.ReadInt32();
		int count = reader.ReadInt32();
		int expectedCount = _scheduler.GetWeightCount();
		if (count != expectedCount)
		{
			_scheduler.InitializeWeights();
			_modelVersion++;
			_initializationTime = DateTime.Now;
			_disableExploration = false;
			return;
		}
		float[] weights = new float[count];
		for (int i = 0; i < count; i++)
		{
			weights[i] = reader.ReadSingle();
		}
		_scheduler.SetAllWeights(weights);
		_modelVersion++;
		if (fileVersion >= 2)
		{
			_disableExploration = reader.ReadBoolean();
			long ticks = reader.ReadInt64();
			_initializationTime = DateTime.FromBinary(ticks);
			ExplorationDurationMinutes = reader.ReadDouble();
			if (!_disableExploration && (DateTime.Now - _initializationTime).TotalMinutes >= ExplorationDurationMinutes)
			{
				_disableExploration = true;
			}
		}
		else
		{
			_initializationTime = DateTime.Now;
			_disableExploration = false;
		}
	}

	public string GetCoreSelectionDistribution()
	{
		int total = _totalDecisions;
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("========== 核心选择分布 ==========");
		sb.AppendLine($"总决策数: {total}");
		sb.AppendLine();
		sb.AppendLine("--- 大核 [0-7] ---");
		int bigCoreTotal = 0;
		for (int i = 0; i < Math.Min(8, _numCores); i++)
		{
			double pct = ((total > 0) ? (100.0 * (double)_coreSelectionCounts[i] / (double)total) : 0.0);
			sb.AppendLine($"C{i}: {_coreSelectionCounts[i],5} 次 ({pct,5:F1}%)");
			bigCoreTotal += _coreSelectionCounts[i];
		}
		double bigPct = ((total > 0) ? (100.0 * (double)bigCoreTotal / (double)total) : 0.0);
		sb.AppendLine($"大核合计: {bigCoreTotal} 次 ({bigPct:F1}%)");
		if (_numCores > 8)
		{
			sb.AppendLine();
			sb.AppendLine("--- 小核 [8+] ---");
			int smallCoreTotal = 0;
			for (int j = 8; j < _numCores; j++)
			{
				double pct2 = ((total > 0) ? (100.0 * (double)_coreSelectionCounts[j] / (double)total) : 0.0);
				sb.AppendLine($"C{j}: {_coreSelectionCounts[j],5} 次 ({pct2,5:F1}%)");
				smallCoreTotal += _coreSelectionCounts[j];
			}
			double smallPct = ((total > 0) ? (100.0 * (double)smallCoreTotal / (double)total) : 0.0);
			sb.AppendLine($"小核合计: {smallCoreTotal} 次 ({smallPct:F1}%)");
		}
		sb.AppendLine();
		sb.AppendLine("--- 负载均衡指标 ---");
		double entropy = 0.0;
		for (int k = 0; k < _numCores; k++)
		{
			double p = ((total > 0) ? ((double)_coreSelectionCounts[k] / (double)total) : 0.0);
			if (p > 0.0)
			{
				entropy -= p * Math.Log(p, 2.0);
			}
		}
		double maxEntropy = Math.Log(_numCores, 2.0);
		double balanceScore = ((maxEntropy > 0.0) ? (entropy / maxEntropy) : 0.0);
		sb.AppendLine($"熵值: {entropy:F3} / {maxEntropy:F3} (最大熵)");
		sb.AppendLine($"均衡度: {balanceScore * 100.0:F1}% (100% = 完美均衡)");
		sb.AppendLine("================================");
		return sb.ToString();
	}
}
}
