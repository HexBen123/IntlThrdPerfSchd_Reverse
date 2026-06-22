using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000006 RID: 6
	public class OnlineLearningManager
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600001B RID: 27 RVA: 0x00002F31 File Offset: 0x00001131
		// (set) Token: 0x0600001C RID: 28 RVA: 0x00002F39 File Offset: 0x00001139
		public float LearningRate { get; set; } = 0.003f;

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600001D RID: 29 RVA: 0x00002F42 File Offset: 0x00001142
		// (set) Token: 0x0600001E RID: 30 RVA: 0x00002F4A File Offset: 0x0000114A
		public int BatchSize { get; set; } = 30;

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600001F RID: 31 RVA: 0x00002F53 File Offset: 0x00001153
		// (set) Token: 0x06000020 RID: 32 RVA: 0x00002F5B File Offset: 0x0000115B
		public int MaxTrainingBatchSize { get; set; } = 20;

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000021 RID: 33 RVA: 0x00002F64 File Offset: 0x00001164
		// (set) Token: 0x06000022 RID: 34 RVA: 0x00002F6C File Offset: 0x0000116C
		public int MaxRecords { get; set; } = 10000;

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000023 RID: 35 RVA: 0x00002F75 File Offset: 0x00001175
		// (set) Token: 0x06000024 RID: 36 RVA: 0x00002F7D File Offset: 0x0000117D
		public float RewardWindowMs { get; set; } = 30f;

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000025 RID: 37 RVA: 0x00002F86 File Offset: 0x00001186
		// (set) Token: 0x06000026 RID: 38 RVA: 0x00002F8E File Offset: 0x0000118E
		public float EnergyWindowSeconds { get; set; } = 1f;

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000027 RID: 39 RVA: 0x00002F97 File Offset: 0x00001197
		// (set) Token: 0x06000028 RID: 40 RVA: 0x00002F9F File Offset: 0x0000119F
		public float EpsilonStart { get; set; } = 0.05f;

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000029 RID: 41 RVA: 0x00002FA8 File Offset: 0x000011A8
		// (set) Token: 0x0600002A RID: 42 RVA: 0x00002FB0 File Offset: 0x000011B0
		public float EpsilonEnd { get; set; } = 0.01f;

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x0600002B RID: 43 RVA: 0x00002FB9 File Offset: 0x000011B9
		// (set) Token: 0x0600002C RID: 44 RVA: 0x00002FC1 File Offset: 0x000011C1
		public int EpsilonDecaySteps { get; set; } = 10000;

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600002D RID: 45 RVA: 0x00002FCA File Offset: 0x000011CA
		// (set) Token: 0x0600002E RID: 46 RVA: 0x00002FD2 File Offset: 0x000011D2
		public float ImmediateRewardWeight { get; set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600002F RID: 47 RVA: 0x00002FDB File Offset: 0x000011DB
		// (set) Token: 0x06000030 RID: 48 RVA: 0x00002FE3 File Offset: 0x000011E3
		public float EnergyRewardWeight { get; set; } = 1f;

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000031 RID: 49 RVA: 0x00002FEC File Offset: 0x000011EC
		// (set) Token: 0x06000032 RID: 50 RVA: 0x00002FF4 File Offset: 0x000011F4
		public float IPCImprovementRewardWeight { get; set; } = 2f;

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000033 RID: 51 RVA: 0x00002FFD File Offset: 0x000011FD
		// (set) Token: 0x06000034 RID: 52 RVA: 0x00003005 File Offset: 0x00001205
		public float OptimalCoreRewardWeight { get; set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000035 RID: 53 RVA: 0x0000300E File Offset: 0x0000120E
		// (set) Token: 0x06000036 RID: 54 RVA: 0x00003016 File Offset: 0x00001216
		public float BaseRewardValue { get; set; } = 0.5f;

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000037 RID: 55 RVA: 0x0000301F File Offset: 0x0000121F
		// (set) Token: 0x06000038 RID: 56 RVA: 0x00003027 File Offset: 0x00001227
		public OnlineLearningManager.ImmediateRewardMode CurrentImmediateRewardMode { get; set; } = OnlineLearningManager.ImmediateRewardMode.Combined;

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000039 RID: 57 RVA: 0x00003030 File Offset: 0x00001230
		// (set) Token: 0x0600003A RID: 58 RVA: 0x00003038 File Offset: 0x00001238
		public float CoreStateWeight { get; set; } = 0.5f;

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x0600003B RID: 59 RVA: 0x00003041 File Offset: 0x00001241
		// (set) Token: 0x0600003C RID: 60 RVA: 0x00003049 File Offset: 0x00001249
		public float ExecutionWeight { get; set; } = 0.5f;

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600003D RID: 61 RVA: 0x00003052 File Offset: 0x00001252
		// (set) Token: 0x0600003E RID: 62 RVA: 0x0000305A File Offset: 0x0000125A
		public float LLCMissRateRewardWeight { get; set; } = 0.5f;

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600003F RID: 63 RVA: 0x00003063 File Offset: 0x00001263
		// (set) Token: 0x06000040 RID: 64 RVA: 0x0000306B File Offset: 0x0000126B
		public float QueueThreadsRewardWeight { get; set; } = 0.5f;

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000041 RID: 65 RVA: 0x00003074 File Offset: 0x00001274
		// (set) Token: 0x06000042 RID: 66 RVA: 0x0000307C File Offset: 0x0000127C
		public float LoadBalanceRewardWeight { get; set; } = 0.2f;

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000043 RID: 67 RVA: 0x00003085 File Offset: 0x00001285
		// (set) Token: 0x06000044 RID: 68 RVA: 0x0000308D File Offset: 0x0000128D
		public float StabilityRewardWeight { get; set; } = 0.15f;

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000045 RID: 69 RVA: 0x00003096 File Offset: 0x00001296
		// (set) Token: 0x06000046 RID: 70 RVA: 0x0000309E File Offset: 0x0000129E
		public float CoreConsistencyPenalty { get; set; } = 0.3f;

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000047 RID: 71 RVA: 0x000030A7 File Offset: 0x000012A7
		// (set) Token: 0x06000048 RID: 72 RVA: 0x000030AF File Offset: 0x000012AF
		public bool EnableImmediateReward { get; set; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000049 RID: 73 RVA: 0x000030B8 File Offset: 0x000012B8
		// (set) Token: 0x0600004A RID: 74 RVA: 0x000030C0 File Offset: 0x000012C0
		public bool EnableEnergyReward { get; set; } = true;

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x0600004B RID: 75 RVA: 0x000030C9 File Offset: 0x000012C9
		// (set) Token: 0x0600004C RID: 76 RVA: 0x000030D6 File Offset: 0x000012D6
		public float AttentionTemperature
		{
			get
			{
				return this._scheduler.AttentionTemperature;
			}
			set
			{
				this._scheduler.AttentionTemperature = value;
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600004D RID: 77 RVA: 0x000030E4 File Offset: 0x000012E4
		// (set) Token: 0x0600004E RID: 78 RVA: 0x000030EC File Offset: 0x000012EC
		public double ExplorationDurationMinutes { get; set; } = 10.0;

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600004F RID: 79 RVA: 0x000030F8 File Offset: 0x000012F8
		public bool IsExploring
		{
			get
			{
				return !this._disableExploration && (DateTime.Now - this._initializationTime).TotalMinutes < this.ExplorationDurationMinutes;
			}
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00003130 File Offset: 0x00001330
		public OnlineLearningManager(RealtimeScheduler scheduler, int numCores, int seed = 42)
		{
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			this._scheduler = scheduler;
			this._numCores = numCores;
			this._allRecords = new List<OnlineLearningManager.SchedulingRecord>();
			this._rewardWindows = new List<OnlineLearningManager.RewardWindow>();
			this._energyWindows = new List<OnlineLearningManager.EnergyWindow>();
			this._random = new Random(seed);
			this._coreSelectionCounts = new int[numCores];
			this._initialized = true;
			this._initializationTime = DateTime.Now;
			this.InitializeWindows();
		}

		// Token: 0x06000051 RID: 81 RVA: 0x0000329C File Offset: 0x0000149C
		public OnlineLearningManager(RealtimeScheduler scheduler, int seed = 42)
		{
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			this._scheduler = scheduler;
			this._allRecords = new List<OnlineLearningManager.SchedulingRecord>();
			this._rewardWindows = new List<OnlineLearningManager.RewardWindow>();
			this._energyWindows = new List<OnlineLearningManager.EnergyWindow>();
			this._threadLastCore = new Dictionary<int, int>();
			this._threadLastIPC = new Dictionary<int, float>();
			this._random = new Random(seed);
			this._initializationTime = DateTime.Now;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x000033FC File Offset: 0x000015FC
		public void SetNumCores(int numCores)
		{
			if (this._initialized)
			{
				throw new InvalidOperationException("OnlineLearningManager already initialized");
			}
			if (numCores <= 0)
			{
				throw new ArgumentException("numCores must be positive", "numCores");
			}
			this._numCores = numCores;
			this._coreSelectionCounts = new int[numCores];
			this._initialized = true;
			this.InitializeWindows();
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000053 RID: 83 RVA: 0x00003450 File Offset: 0x00001650
		public bool IsInitialized
		{
			get
			{
				return this._initialized;
			}
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00003458 File Offset: 0x00001658
		private void InitializeWindows()
		{
			DateTime now = DateTime.Now;
			this._currentEnergyWindow = new OnlineLearningManager.EnergyWindow
			{
				StartTime = now,
				EndTime = now.AddSeconds((double)this.EnergyWindowSeconds)
			};
			this._energyWindows.Add(this._currentEnergyWindow);
			this._currentRewardWindow = new OnlineLearningManager.RewardWindow
			{
				StartTime = now,
				EndTime = now.AddMilliseconds((double)this.RewardWindowMs)
			};
			this._rewardWindows.Add(this._currentRewardWindow);
			this._currentEnergyWindow.SubWindows.Add(this._currentRewardWindow);
		}

		// Token: 0x06000055 RID: 85 RVA: 0x000034F0 File Offset: 0x000016F0
		public int Schedule(float[] threadFeatures, float[][] coreFeatures, int threadId = 0)
		{
			if (!this._initialized)
			{
				throw new InvalidOperationException("OnlineLearningManager not initialized. Call SetNumCores first.");
			}
			this.CheckAndRotateWindows();
			float[] actionProbs = this.GetActionProbs(threadFeatures, coreFeatures);
			float currentEpsilon = this.GetCurrentEpsilon();
			int num;
			float num2;
			if (this._random.NextDouble() < (double)currentEpsilon)
			{
				num = this._random.Next(this._numCores);
				num2 = 1f / (float)this._numCores;
			}
			else
			{
				num = 0;
				float num3 = actionProbs[0];
				for (int i = 1; i < this._numCores; i++)
				{
					if (actionProbs[i] > num3)
					{
						num3 = actionProbs[i];
						num = i;
					}
				}
				num2 = actionProbs[num];
			}
			int? num4 = null;
			int num5;
			if (threadId != 0 && this._threadLastCore.TryGetValue(threadId, out num5))
			{
				num4 = new int?(num5);
			}
			float num6 = 0f;
			bool flag = false;
			float num7;
			if (threadId != 0 && this._threadLastIPC.TryGetValue(threadId, out num7))
			{
				num6 = num7;
				flag = true;
			}
			OnlineLearningManager.SchedulingRecord schedulingRecord = new OnlineLearningManager.SchedulingRecord
			{
				ThreadFeatures = (float[])threadFeatures.Clone(),
				CoreFeatures = this.CloneCoreFeatures(coreFeatures),
				SelectedCore = num,
				Timestamp = DateTime.Now,
				ActionProb = num2,
				ThreadId = threadId,
				PreviousCore = num4,
				PreviousIPC = num6,
				HasPreviousIPC = flag,
				SelectedCoreLLCMissRate = coreFeatures[num][3],
				SelectedCoreQueueThreads = coreFeatures[num][2],
				SelectedCoreQueueExecTime = coreFeatures[num][1],
				SelectedCoreL1MissRateBefore = coreFeatures[num][3],
				SelectedCoreIPCBefore = coreFeatures[num][4]
			};
			if (threadId != 0)
			{
				this._threadLastCore[threadId] = num;
			}
			List<OnlineLearningManager.SchedulingRecord> allRecords = this._allRecords;
			lock (allRecords)
			{
				this._allRecords.Add(schedulingRecord);
				this._currentRewardWindow.Decisions.Add(schedulingRecord);
				while (this._allRecords.Count > this.MaxRecords)
				{
					this._allRecords.RemoveAt(0);
				}
			}
			this._totalDecisions++;
			this._coreSelectionCounts[num]++;
			return num;
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00003708 File Offset: 0x00001908
		public void OnThreadComplete(int decisionCore, float executionTime, float actualIPC, float cacheMissRate, int? actualCore = null, float coreL1MissRateAfter = 0f, float coreIPCAfter = 0f)
		{
			OnlineLearningManager.SchedulingRecord schedulingRecord = null;
			List<OnlineLearningManager.SchedulingRecord> allRecords = this._allRecords;
			lock (allRecords)
			{
				for (int i = this._allRecords.Count - 1; i >= 0; i--)
				{
					if (this._allRecords[i].SelectedCore == decisionCore && !this._allRecords[i].HasExecutionResult)
					{
						schedulingRecord = this._allRecords[i];
						break;
					}
				}
				if (schedulingRecord == null)
				{
					for (int j = this._allRecords.Count - 1; j >= 0; j--)
					{
						if (!this._allRecords[j].HasExecutionResult)
						{
							schedulingRecord = this._allRecords[j];
							int selectedCore = schedulingRecord.SelectedCore;
							break;
						}
					}
				}
			}
			if (schedulingRecord == null)
			{
				return;
			}
			bool flag2 = actualCore != null && actualCore.Value != schedulingRecord.SelectedCore;
			schedulingRecord.ExecutionTime = executionTime;
			schedulingRecord.ActualIPC = actualIPC;
			schedulingRecord.CacheMissRate = cacheMissRate;
			schedulingRecord.ActualCore = actualCore;
			schedulingRecord.SelectedCoreL1MissRateAfter = (flag2 ? schedulingRecord.SelectedCoreL1MissRateBefore : coreL1MissRateAfter);
			schedulingRecord.SelectedCoreIPCAfter = (flag2 ? schedulingRecord.SelectedCoreIPCBefore : coreIPCAfter);
			schedulingRecord.HasExecutionResult = true;
			if (schedulingRecord.ThreadId != 0)
			{
				this._threadLastIPC[schedulingRecord.ThreadId] = actualIPC;
			}
			if (this.EnableImmediateReward)
			{
				float num = this.CalculateIPCImprovementReward(actualIPC, schedulingRecord.PreviousIPC, schedulingRecord.HasPreviousIPC, this.BaseRewardValue);
				schedulingRecord.ImmediatePortion = this.ImmediateRewardWeight * num;
				this._totalImmediateReward += schedulingRecord.ImmediatePortion;
				if (flag2)
				{
					schedulingRecord.ImmediatePortion *= 1f - this.CoreConsistencyPenalty;
					return;
				}
			}
			else if (this.EnableEnergyReward)
			{
				schedulingRecord.ImmediatePortion = this.BaseRewardValue;
			}
		}

		// Token: 0x06000057 RID: 87 RVA: 0x000038E4 File Offset: 0x00001AE4
		private float CalculateIPCImprovementReward(float actualIPC, float previousIPC, bool hasPreviousIPC, float baseReward)
		{
			if (!hasPreviousIPC)
			{
				return baseReward;
			}
			float num = (actualIPC - previousIPC) / Math.Max(previousIPC, 0.001f);
			if (num > 0f)
			{
				float num2 = Math.Min(num * 1f, 1f);
				return Math.Min(baseReward + num2, 1f);
			}
			float num3 = Math.Abs(num) * 1f;
			return Math.Max(baseReward - num3, 0f);
		}

		// Token: 0x06000058 RID: 88 RVA: 0x0000394C File Offset: 0x00001B4C
		public bool UpdateEnergyReward(float energyEfficiency, bool forceUpdate = false)
		{
			if (float.IsNaN(energyEfficiency) || float.IsInfinity(energyEfficiency))
			{
				return false;
			}
			if (this.EnableEnergyReward)
			{
				this._currentEnergyWindow.EnergyEfficiency = energyEfficiency;
				this._currentEnergyWindow.IsComplete = true;
				this.DistributeEnergyReward(this._currentEnergyWindow, energyEfficiency);
				DateTime now = DateTime.Now;
				this._currentEnergyWindow = new OnlineLearningManager.EnergyWindow
				{
					StartTime = now,
					EndTime = now.AddSeconds((double)this.EnergyWindowSeconds)
				};
				this._energyWindows.Add(this._currentEnergyWindow);
			}
			int num = 0;
			List<OnlineLearningManager.SchedulingRecord> allRecords = this._allRecords;
			lock (allRecords)
			{
				using (List<OnlineLearningManager.SchedulingRecord>.Enumerator enumerator = this._allRecords.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.HasExecutionResult)
						{
							num++;
						}
					}
				}
				if (num >= this.BatchSize || forceUpdate)
				{
					this.UpdateModel();
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00003A6C File Offset: 0x00001C6C
		public bool UpdateReward(float energyEfficiency, bool forceUpdate = false)
		{
			return this.UpdateEnergyReward(energyEfficiency, forceUpdate);
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00003A78 File Offset: 0x00001C78
		public OnlineLearningManager.LearningStats GetStats()
		{
			int num = 0;
			int num2 = 0;
			List<OnlineLearningManager.SchedulingRecord> allRecords = this._allRecords;
			lock (allRecords)
			{
				using (List<OnlineLearningManager.SchedulingRecord>.Enumerator enumerator = this._allRecords.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.HasExecutionResult)
						{
							num2++;
						}
						else
						{
							num++;
						}
					}
				}
			}
			OnlineLearningManager.LearningStats learningStats = new OnlineLearningManager.LearningStats();
			learningStats.TotalDecisions = this._totalDecisions;
			learningStats.TotalUpdates = this._totalUpdates;
			learningStats.AverageImmediateReward = ((this._totalDecisions > 0) ? (this._totalImmediateReward / (float)this._totalDecisions) : 0f);
			learningStats.AverageEnergyReward = ((this._energyRewardCount > 0) ? (this._totalEnergyReward / (float)this._energyRewardCount) : 0f);
			learningStats.PendingRecords = num;
			learningStats.ModelVersion = this._modelVersion;
			learningStats.PendingEnergyWindows = this._energyWindows.Count((OnlineLearningManager.EnergyWindow w) => !w.IsComplete);
			return learningStats;
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00003BA8 File Offset: 0x00001DA8
		public string GetNetworkStatsString()
		{
			if (!this._initialized)
			{
				return "OnlineLearningManager not initialized";
			}
			StringBuilder stringBuilder = new StringBuilder();
			OnlineLearningManager.LearningStats stats = this.GetStats();
			stringBuilder.AppendLine("========== 网络学习统计 ==========");
			stringBuilder.AppendLine(string.Format("模型版本: {0}", stats.ModelVersion));
			stringBuilder.AppendLine(string.Format("总决策数: {0}", stats.TotalDecisions));
			stringBuilder.AppendLine(string.Format("总更新数: {0}", stats.TotalUpdates));
			stringBuilder.AppendLine(string.Format("待处理记录: {0}", stats.PendingRecords));
			TimeSpan timeSpan = DateTime.Now - this._initializationTime;
			double num = Math.Max(0.0, this.ExplorationDurationMinutes - timeSpan.TotalMinutes);
			stringBuilder.AppendLine(string.Format("当前探索率(Epsilon): {0:F4}", this.GetCurrentEpsilon()));
			stringBuilder.AppendLine("探索状态: " + (this.IsExploring ? string.Format("进行中 (剩余 {0:F1} 分钟)", num) : "已结束"));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("---------- 奖励统计 ----------");
			stringBuilder.AppendLine(string.Format("平均即时奖励: {0:F4}", stats.AverageImmediateReward));
			stringBuilder.AppendLine(string.Format("平均能效奖励: {0:F4}", stats.AverageEnergyReward));
			stringBuilder.AppendLine(string.Format("平均总奖励: {0:F4}", stats.AverageReward));
			stringBuilder.AppendLine(string.Format("即时奖励权重: {0:F2}", this.ImmediateRewardWeight));
			stringBuilder.AppendLine(string.Format("能效奖励权重: {0:F2}", this.EnergyRewardWeight));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("---------- 网络配置 ----------");
			stringBuilder.AppendLine(string.Format("核心数: {0}", this._numCores));
			stringBuilder.AppendLine(string.Format("dModel: {0}", 128));
			stringBuilder.AppendLine(string.Format("注意力头数: {0}", 8));
			stringBuilder.AppendLine(string.Format("头维度: {0}", 16));
			stringBuilder.AppendLine(string.Format("学习率: {0:F6}", this.LearningRate));
			stringBuilder.AppendLine(string.Format("批次大小: {0}", this.BatchSize));
			stringBuilder.AppendLine(string.Format("注意力温度: {0:F2}", this.AttentionTemperature));
			stringBuilder.AppendLine();
			float[] allWeights = this._scheduler.GetAllWeights();
			float num2 = 0f;
			float num3 = float.MinValue;
			float num4 = float.MaxValue;
			int num5 = 0;
			int num6 = 0;
			foreach (float num7 in allWeights)
			{
				if (float.IsNaN(num7))
				{
					num5++;
				}
				else if (float.IsInfinity(num7))
				{
					num6++;
				}
				else
				{
					num2 += num7;
					if (num7 > num3)
					{
						num3 = num7;
					}
					if (num7 < num4)
					{
						num4 = num7;
					}
				}
			}
			stringBuilder.AppendLine("---------- 权重统计 ----------");
			stringBuilder.AppendLine(string.Format("总权重数: {0}", allWeights.Length));
			if (num5 > 0 || num6 > 0)
			{
				stringBuilder.AppendLine(string.Format("[警告] NaN数量: {0}, Infinity数量: {1}", num5, num6));
				stringBuilder.AppendLine("[建议] 模型已损坏，请重新初始化或加载备份模型");
			}
			else
			{
				stringBuilder.AppendLine(string.Format("权重均值: {0:F6}", num2 / (float)allWeights.Length));
				stringBuilder.AppendLine(string.Format("权重最大: {0:F6}", num3));
				stringBuilder.AppendLine(string.Format("权重最小: {0:F6}", num4));
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("---------- 注意力头说明 ----------");
			stringBuilder.AppendLine("Head 0 (性能匹配): 关注IPC、指令数、优先级");
			stringBuilder.AppendLine("Head 1 (缓存匹配): 关注LLC miss、分支预测");
			stringBuilder.AppendLine("Head 2 (负载均衡): 关注队列深度、利用率");
			stringBuilder.AppendLine("Head 3 (能效优化): 关注功耗相关特征");
			stringBuilder.AppendLine("Head 4 (内存带宽): 关注内存访问模式");
			stringBuilder.AppendLine("Head 5 (计算密集): 关注计算密集型任务");
			stringBuilder.AppendLine("Head 6 (IO密集): 关注IO等待特征");
			stringBuilder.AppendLine("Head 7 (综合评估): 综合多维度特征");
			stringBuilder.AppendLine("================================");
			return stringBuilder.ToString();
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00003FEC File Offset: 0x000021EC
		public string GetCriticStatsString()
		{
			if (!this._initialized)
			{
				return "OnlineLearningManager not initialized";
			}
			StringBuilder stringBuilder = new StringBuilder();
			this.GetStats();
			stringBuilder.AppendLine("================================");
			return stringBuilder.ToString();
		}

		// Token: 0x0600005D RID: 93 RVA: 0x0000401C File Offset: 0x0000221C
		public string GetLastAttentionWeightsString()
		{
			if (!this._initialized)
			{
				return "OnlineLearningManager not initialized";
			}
			StringBuilder stringBuilder = new StringBuilder();
			float[][] attentionWeights = this._scheduler.GetAttentionWeights(this._numCores);
			stringBuilder.AppendLine("========== 注意力权重分布 ==========");
			string[] array = new string[] { "性能匹配", "缓存匹配", "负载均衡", "能效优化", "内存带宽", "计算密集", "IO密集", "综合评估" };
			for (int i = 0; i < 8; i++)
			{
				stringBuilder.AppendLine(string.Format("--- Head {0} ({1}) ---", i, array[i]));
				float num = 0f;
				float num2 = float.MinValue;
				int num3 = 0;
				for (int j = 0; j < this._numCores; j++)
				{
					num += attentionWeights[i][j];
					if (attentionWeights[i][j] > num2)
					{
						num2 = attentionWeights[i][j];
						num3 = j;
					}
				}
				stringBuilder.Append("大核[0-7]: ");
				for (int k = 0; k < Math.Min(8, this._numCores); k++)
				{
					stringBuilder.Append(string.Format("C{0}={1:F3} ", k, attentionWeights[i][k]));
				}
				stringBuilder.AppendLine();
				if (this._numCores > 8)
				{
					stringBuilder.Append("小核[8+]: ");
					for (int l = 8; l < this._numCores; l++)
					{
						stringBuilder.Append(string.Format("C{0}={1:F3} ", l, attentionWeights[i][l]));
						if ((l - 7) % 8 == 0 && l < this._numCores - 1)
						{
							stringBuilder.AppendLine();
						}
						if (l > 8 && (l - 7) % 8 == 0)
						{
							stringBuilder.Append("         ");
						}
					}
					stringBuilder.AppendLine();
				}
				stringBuilder.AppendLine(string.Format("最大权重核心: C{0} ({1:F4}), 权重和: {2:F4}", num3, num2, num));
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine("--- 聚合概率分布 ---");
			float[] array2 = new float[this._numCores];
			for (int m = 0; m < this._numCores; m++)
			{
				for (int n = 0; n < 8; n++)
				{
					array2[m] += attentionWeights[n][m];
				}
			}
			float num4 = 0f;
			foreach (float num6 in array2)
			{
				num4 += num6;
			}
			if (num4 > 0f)
			{
				for (int num7 = 0; num7 < this._numCores; num7++)
				{
					array2[num7] /= num4;
				}
			}
			stringBuilder.Append("大核[0-7]: ");
			for (int num8 = 0; num8 < Math.Min(8, this._numCores); num8++)
			{
				stringBuilder.Append(string.Format("C{0}={1:F1}% ", num8, array2[num8] * 100f));
			}
			stringBuilder.AppendLine();
			if (this._numCores > 8)
			{
				stringBuilder.Append("小核[8+]: ");
				for (int num9 = 8; num9 < this._numCores; num9++)
				{
					stringBuilder.Append(string.Format("C{0}={1:F1}% ", num9, array2[num9] * 100f));
					if ((num9 - 7) % 8 == 0 && num9 < this._numCores - 1)
					{
						stringBuilder.AppendLine();
					}
					if (num9 > 8 && (num9 - 7) % 8 == 0)
					{
						stringBuilder.Append("         ");
					}
				}
				stringBuilder.AppendLine();
			}
			int num10 = 0;
			float num11 = array2[0];
			for (int num12 = 1; num12 < this._numCores; num12++)
			{
				if (array2[num12] > num11)
				{
					num11 = array2[num12];
					num10 = num12;
				}
			}
			stringBuilder.AppendLine(string.Format("推荐核心: C{0} (概率: {1:F1}%)", num10, num11 * 100f));
			stringBuilder.AppendLine("====================================");
			return stringBuilder.ToString();
		}

		// Token: 0x0600005E RID: 94 RVA: 0x0000441A File Offset: 0x0000261A
		public string GetAttentionScoreStats()
		{
			if (!this._initialized)
			{
				return "OnlineLearningManager not initialized";
			}
			return this._scheduler.GetAttentionScoreStats(this._numCores);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x0000443C File Offset: 0x0000263C
		private void CheckAndRotateWindows()
		{
			DateTime now = DateTime.Now;
			if (now >= this._currentRewardWindow.EndTime)
			{
				this._currentRewardWindow.EndState = this.CaptureCoreState();
				this._currentRewardWindow = new OnlineLearningManager.RewardWindow
				{
					StartTime = now,
					EndTime = now.AddMilliseconds((double)this.RewardWindowMs)
				};
				this._rewardWindows.Add(this._currentRewardWindow);
				this._currentEnergyWindow.SubWindows.Add(this._currentRewardWindow);
			}
		}

		// Token: 0x06000060 RID: 96 RVA: 0x000044C0 File Offset: 0x000026C0
		private float[][] CaptureCoreState()
		{
			return new float[this._numCores][];
		}

		// Token: 0x06000061 RID: 97 RVA: 0x000044D0 File Offset: 0x000026D0
		private void DistributeEnergyReward(OnlineLearningManager.EnergyWindow energyWindow, float totalEnergyReward)
		{
			List<OnlineLearningManager.SchedulingRecord> list = new List<OnlineLearningManager.SchedulingRecord>();
			List<OnlineLearningManager.SchedulingRecord> allRecords = this._allRecords;
			lock (allRecords)
			{
				foreach (OnlineLearningManager.SchedulingRecord schedulingRecord in this._allRecords)
				{
					if (schedulingRecord.HasExecutionResult && schedulingRecord.EnergyPortion == 0f)
					{
						list.Add(schedulingRecord);
					}
				}
			}
			if (list.Count == 0)
			{
				return;
			}
			if (float.IsNaN(totalEnergyReward) || float.IsInfinity(totalEnergyReward))
			{
				return;
			}
			float num = (totalEnergyReward - -100f) / 200f;
			num = Math.Max(0f, Math.Min(1f, num));
			float num2 = 0f;
			float[] array = new float[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				array[i] = 1f;
				list[i].TimeWeight = array[i];
				num2 += array[i];
			}
			float num3 = this.EnergyRewardWeight * num * (float)list.Count;
			for (int j = 0; j < list.Count; j++)
			{
				list[j].EnergyPortion = array[j] / num2 * num3;
				this._totalEnergyReward += list[j].EnergyPortion;
				this._energyRewardCount++;
			}
		}

		// Token: 0x06000062 RID: 98 RVA: 0x0000465C File Offset: 0x0000285C
		private float[] GetActionProbs(float[] threadFeatures, float[][] coreFeatures)
		{
			this._scheduler.Predict(threadFeatures, coreFeatures, this._numCores);
			float[][] attentionWeights = this._scheduler.GetAttentionWeights(this._numCores);
			float[] array = new float[this._numCores];
			for (int i = 0; i < this._numCores; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					array[i] += attentionWeights[j][i];
				}
			}
			float num = 0f;
			for (int k = 0; k < this._numCores; k++)
			{
				num += array[k];
			}
			if (num > 0f)
			{
				for (int l = 0; l < this._numCores; l++)
				{
					array[l] /= num;
				}
			}
			else
			{
				for (int m = 0; m < this._numCores; m++)
				{
					array[m] = 1f / (float)this._numCores;
				}
			}
			return array;
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00004744 File Offset: 0x00002944
		private float[] Softmax(float[] scores)
		{
			float[] array = new float[scores.Length];
			float num = scores[0];
			for (int i = 1; i < scores.Length; i++)
			{
				if (scores[i] > num)
				{
					num = scores[i];
				}
			}
			float num2 = 0f;
			for (int j = 0; j < scores.Length; j++)
			{
				array[j] = (float)Math.Exp((double)(scores[j] - num));
				num2 += array[j];
			}
			for (int k = 0; k < scores.Length; k++)
			{
				array[k] /= num2;
			}
			return array;
		}

		// Token: 0x06000064 RID: 100 RVA: 0x000047C8 File Offset: 0x000029C8
		private float GetCurrentEpsilon()
		{
			if (this._disableExploration)
			{
				return 0f;
			}
			if ((DateTime.Now - this._initializationTime).TotalMinutes >= this.ExplorationDurationMinutes)
			{
				return 0f;
			}
			if (this._totalDecisions >= this.EpsilonDecaySteps)
			{
				return this.EpsilonEnd;
			}
			float num = (float)this._totalDecisions / (float)this.EpsilonDecaySteps;
			return this.EpsilonStart + (this.EpsilonEnd - this.EpsilonStart) * num;
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00004844 File Offset: 0x00002A44
		private void UpdateModel()
		{
			if (this._allRecords.Count == 0)
			{
				return;
			}
			List<OnlineLearningManager.SchedulingRecord> list = new List<OnlineLearningManager.SchedulingRecord>();
			List<OnlineLearningManager.SchedulingRecord> list2 = this._allRecords;
			lock (list2)
			{
				foreach (OnlineLearningManager.SchedulingRecord schedulingRecord in this._allRecords)
				{
					if (schedulingRecord.HasExecutionResult)
					{
						list.Add(schedulingRecord);
					}
				}
			}
			if (list.Count == 0)
			{
				return;
			}
			if (list.Count > this.MaxTrainingBatchSize)
			{
				list = list.GetRange(list.Count - this.MaxTrainingBatchSize, this.MaxTrainingBatchSize);
			}
			float num = 0f;
			foreach (OnlineLearningManager.SchedulingRecord schedulingRecord2 in list)
			{
				num += schedulingRecord2.TotalReward;
			}
			num /= (float)list.Count;
			this._scheduler.ClearGradients();
			foreach (OnlineLearningManager.SchedulingRecord schedulingRecord3 in list)
			{
				this._scheduler.PredictWithCache(schedulingRecord3.ThreadFeatures, schedulingRecord3.CoreFeatures, this._numCores);
				float num2 = schedulingRecord3.TotalReward - num;
				this._scheduler.Backward(schedulingRecord3.SelectedCore, num2);
			}
			this._scheduler.ApplyGradients(this.LearningRate, list.Count);
			list2 = this._allRecords;
			lock (list2)
			{
				if (this.EnableEnergyReward)
				{
					this._allRecords.RemoveAll((OnlineLearningManager.SchedulingRecord r) => r.EnergyPortion > 0f);
				}
				else
				{
					HashSet<OnlineLearningManager.SchedulingRecord> trainedSet = new HashSet<OnlineLearningManager.SchedulingRecord>(list);
					this._allRecords.RemoveAll((OnlineLearningManager.SchedulingRecord r) => trainedSet.Contains(r));
				}
			}
			this.CleanupOldWindows();
			this._totalUpdates++;
			this._modelVersion++;
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00004AA8 File Offset: 0x00002CA8
		private void CleanupOldWindows()
		{
			int num = 5;
			while (this._energyWindows.Count > num)
			{
				if (!this._energyWindows[0].IsComplete)
				{
					IL_003F:
					while (this._rewardWindows.Count > num * (int)(this.EnergyWindowSeconds * 1000f / this.RewardWindowMs))
					{
						this._rewardWindows.RemoveAt(0);
					}
					return;
				}
				this._energyWindows.RemoveAt(0);
			}
			goto IL_003F;
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00004B18 File Offset: 0x00002D18
		private float[][] CloneCoreFeatures(float[][] features)
		{
			float[][] array = new float[features.Length][];
			for (int i = 0; i < features.Length; i++)
			{
				array[i] = (float[])features[i].Clone();
			}
			return array;
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00004B50 File Offset: 0x00002D50
		public bool IsModelCorrupted()
		{
			foreach (float num in this._scheduler.GetAllWeights())
			{
				if (float.IsNaN(num) || float.IsInfinity(num))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00004B90 File Offset: 0x00002D90
		public void ResetModel()
		{
			this._scheduler.InitializeWeights();
			this._modelVersion++;
			List<OnlineLearningManager.SchedulingRecord> allRecords = this._allRecords;
			lock (allRecords)
			{
				this._allRecords.Clear();
			}
			this._rewardWindows.Clear();
			this._energyWindows.Clear();
			this._totalDecisions = 0;
			this._totalUpdates = 0;
			this._totalImmediateReward = 0f;
			this._totalEnergyReward = 0f;
			this._energyRewardCount = 0;
			Array.Clear(this._coreSelectionCounts, 0, this._coreSelectionCounts.Length);
			this.InitializeWindows();
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00004C4C File Offset: 0x00002E4C
		public void SaveModel()
		{
			this.SaveModel("scheduler_model.bin");
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00004C5C File Offset: 0x00002E5C
		public void SaveModel(string filePath)
		{
			float[] allWeights = this._scheduler.GetAllWeights();
			using (BinaryWriter binaryWriter = new BinaryWriter(File.Open(filePath, FileMode.Create)))
			{
				binaryWriter.Write(2);
				binaryWriter.Write(allWeights.Length);
				foreach (float num in allWeights)
				{
					binaryWriter.Write(num);
				}
				bool flag = !this.IsExploring;
				binaryWriter.Write(flag);
				binaryWriter.Write(this._initializationTime.ToBinary());
				binaryWriter.Write(this.ExplorationDurationMinutes);
			}
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00004D00 File Offset: 0x00002F00
		public void LoadModel()
		{
			this.LoadModel("scheduler_model.bin");
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00004D10 File Offset: 0x00002F10
		public void LoadModel(string filePath)
		{
			if (!File.Exists(filePath))
			{
				this._scheduler.InitializeWeights();
				this._modelVersion++;
				this._initializationTime = DateTime.Now;
				this._disableExploration = false;
				return;
			}
			using (BinaryReader binaryReader = new BinaryReader(File.Open(filePath, FileMode.Open)))
			{
				int num = binaryReader.ReadInt32();
				int num2 = binaryReader.ReadInt32();
				int weightCount = this._scheduler.GetWeightCount();
				if (num2 != weightCount)
				{
					this._scheduler.InitializeWeights();
					this._modelVersion++;
					this._initializationTime = DateTime.Now;
					this._disableExploration = false;
				}
				else
				{
					float[] array = new float[num2];
					for (int i = 0; i < num2; i++)
					{
						array[i] = binaryReader.ReadSingle();
					}
					this._scheduler.SetAllWeights(array);
					this._modelVersion++;
					if (num >= 2)
					{
						this._disableExploration = binaryReader.ReadBoolean();
						long num3 = binaryReader.ReadInt64();
						this._initializationTime = DateTime.FromBinary(num3);
						this.ExplorationDurationMinutes = binaryReader.ReadDouble();
						if (!this._disableExploration && (DateTime.Now - this._initializationTime).TotalMinutes >= this.ExplorationDurationMinutes)
						{
							this._disableExploration = true;
						}
					}
					else
					{
						this._initializationTime = DateTime.Now;
						this._disableExploration = false;
					}
				}
			}
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00004E88 File Offset: 0x00003088
		public string GetCoreSelectionDistribution()
		{
			int totalDecisions = this._totalDecisions;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("========== 核心选择分布 ==========");
			stringBuilder.AppendLine(string.Format("总决策数: {0}", totalDecisions));
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("--- 大核 [0-7] ---");
			int num = 0;
			for (int i = 0; i < Math.Min(8, this._numCores); i++)
			{
				double num2 = ((totalDecisions > 0) ? (100.0 * (double)this._coreSelectionCounts[i] / (double)totalDecisions) : 0.0);
				stringBuilder.AppendLine(string.Format("C{0}: {1,5} 次 ({2,5:F1}%)", i, this._coreSelectionCounts[i], num2));
				num += this._coreSelectionCounts[i];
			}
			double num3 = ((totalDecisions > 0) ? (100.0 * (double)num / (double)totalDecisions) : 0.0);
			stringBuilder.AppendLine(string.Format("大核合计: {0} 次 ({1:F1}%)", num, num3));
			if (this._numCores > 8)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("--- 小核 [8+] ---");
				int num4 = 0;
				for (int j = 8; j < this._numCores; j++)
				{
					double num5 = ((totalDecisions > 0) ? (100.0 * (double)this._coreSelectionCounts[j] / (double)totalDecisions) : 0.0);
					stringBuilder.AppendLine(string.Format("C{0}: {1,5} 次 ({2,5:F1}%)", j, this._coreSelectionCounts[j], num5));
					num4 += this._coreSelectionCounts[j];
				}
				double num6 = ((totalDecisions > 0) ? (100.0 * (double)num4 / (double)totalDecisions) : 0.0);
				stringBuilder.AppendLine(string.Format("小核合计: {0} 次 ({1:F1}%)", num4, num6));
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("--- 负载均衡指标 ---");
			double num7 = 0.0;
			for (int k = 0; k < this._numCores; k++)
			{
				double num8 = ((totalDecisions > 0) ? ((double)this._coreSelectionCounts[k] / (double)totalDecisions) : 0.0);
				if (num8 > 0.0)
				{
					num7 -= num8 * Math.Log(num8, 2.0);
				}
			}
			double num9 = Math.Log((double)this._numCores, 2.0);
			double num10 = ((num9 > 0.0) ? (num7 / num9) : 0.0);
			stringBuilder.AppendLine(string.Format("熵值: {0:F3} / {1:F3} (最大熵)", num7, num9));
			stringBuilder.AppendLine(string.Format("均衡度: {0:F1}% (100% = 完美均衡)", num10 * 100.0));
			stringBuilder.AppendLine("================================");
			return stringBuilder.ToString();
		}

		// Token: 0x04000041 RID: 65
		private readonly RealtimeScheduler _scheduler;

		// Token: 0x04000042 RID: 66
		private readonly List<OnlineLearningManager.SchedulingRecord> _allRecords;

		// Token: 0x04000043 RID: 67
		private readonly List<OnlineLearningManager.RewardWindow> _rewardWindows;

		// Token: 0x04000044 RID: 68
		private readonly List<OnlineLearningManager.EnergyWindow> _energyWindows;

		// Token: 0x04000045 RID: 69
		private readonly Dictionary<int, int> _threadLastCore;

		// Token: 0x04000046 RID: 70
		private readonly Dictionary<int, float> _threadLastIPC;

		// Token: 0x04000047 RID: 71
		private int _numCores;

		// Token: 0x04000048 RID: 72
		private bool _initialized;

		// Token: 0x04000049 RID: 73
		private OnlineLearningManager.RewardWindow _currentRewardWindow;

		// Token: 0x0400004A RID: 74
		private OnlineLearningManager.EnergyWindow _currentEnergyWindow;

		// Token: 0x04000064 RID: 100
		private DateTime _initializationTime;

		// Token: 0x04000065 RID: 101
		private bool _disableExploration;

		// Token: 0x04000066 RID: 102
		private int _totalDecisions;

		// Token: 0x04000067 RID: 103
		private int _totalUpdates;

		// Token: 0x04000068 RID: 104
		private float _totalImmediateReward;

		// Token: 0x04000069 RID: 105
		private float _totalEnergyReward;

		// Token: 0x0400006A RID: 106
		private int _energyRewardCount;

		// Token: 0x0400006B RID: 107
		private int _modelVersion;

		// Token: 0x0400006C RID: 108
		private Random _random;

		// Token: 0x0400006D RID: 109
		private int[] _coreSelectionCounts;

		// Token: 0x0400006E RID: 110
		private const string DefaultModelFileName = "scheduler_model.bin";

		// Token: 0x0200005E RID: 94
		public class EnergyWindow
		{
			// Token: 0x04000469 RID: 1129
			public DateTime StartTime;

			// Token: 0x0400046A RID: 1130
			public DateTime EndTime;

			// Token: 0x0400046B RID: 1131
			public List<OnlineLearningManager.RewardWindow> SubWindows = new List<OnlineLearningManager.RewardWindow>();

			// Token: 0x0400046C RID: 1132
			public float EnergyEfficiency;

			// Token: 0x0400046D RID: 1133
			public bool IsComplete;
		}

		// Token: 0x0200005F RID: 95
		public class RewardWindow
		{
			// Token: 0x0400046E RID: 1134
			public DateTime StartTime;

			// Token: 0x0400046F RID: 1135
			public DateTime EndTime;

			// Token: 0x04000470 RID: 1136
			public float[][] StartState;

			// Token: 0x04000471 RID: 1137
			public float[][] EndState;

			// Token: 0x04000472 RID: 1138
			public List<OnlineLearningManager.SchedulingRecord> Decisions = new List<OnlineLearningManager.SchedulingRecord>();

			// Token: 0x04000473 RID: 1139
			public float ImmediateReward;

			// Token: 0x04000474 RID: 1140
			public float EnergyReward;

			// Token: 0x04000475 RID: 1141
			public bool HasImmediateReward;

			// Token: 0x04000476 RID: 1142
			public bool HasEnergyReward;
		}

		// Token: 0x02000060 RID: 96
		public class SchedulingRecord
		{
			// Token: 0x17000042 RID: 66
			// (get) Token: 0x06000326 RID: 806 RVA: 0x0001EC88 File Offset: 0x0001CE88
			public float TotalReward
			{
				get
				{
					return this.ImmediatePortion + this.EnergyPortion;
				}
			}

			// Token: 0x04000477 RID: 1143
			public float[] ThreadFeatures;

			// Token: 0x04000478 RID: 1144
			public float[][] CoreFeatures;

			// Token: 0x04000479 RID: 1145
			public int SelectedCore;

			// Token: 0x0400047A RID: 1146
			public DateTime Timestamp;

			// Token: 0x0400047B RID: 1147
			public float ActionProb;

			// Token: 0x0400047C RID: 1148
			public int ThreadId;

			// Token: 0x0400047D RID: 1149
			public int? PreviousCore;

			// Token: 0x0400047E RID: 1150
			public float ImmediatePortion;

			// Token: 0x0400047F RID: 1151
			public float EnergyPortion;

			// Token: 0x04000480 RID: 1152
			public float TimeWeight;

			// Token: 0x04000481 RID: 1153
			public float SelectedCoreLLCMissRate;

			// Token: 0x04000482 RID: 1154
			public float SelectedCoreQueueThreads;

			// Token: 0x04000483 RID: 1155
			public float SelectedCoreQueueExecTime;

			// Token: 0x04000484 RID: 1156
			public float SelectedCoreL1MissRateBefore;

			// Token: 0x04000485 RID: 1157
			public float SelectedCoreL1MissRateAfter;

			// Token: 0x04000486 RID: 1158
			public float SelectedCoreIPCBefore;

			// Token: 0x04000487 RID: 1159
			public float SelectedCoreIPCAfter;

			// Token: 0x04000488 RID: 1160
			public float PreviousIPC;

			// Token: 0x04000489 RID: 1161
			public bool HasPreviousIPC;

			// Token: 0x0400048A RID: 1162
			public float ExecutionTime;

			// Token: 0x0400048B RID: 1163
			public float ActualIPC;

			// Token: 0x0400048C RID: 1164
			public float CacheMissRate;

			// Token: 0x0400048D RID: 1165
			public int? ActualCore;

			// Token: 0x0400048E RID: 1166
			public bool HasExecutionResult;
		}

		// Token: 0x02000061 RID: 97
		public class LearningStats
		{
			// Token: 0x17000043 RID: 67
			// (get) Token: 0x06000328 RID: 808 RVA: 0x0001EC9F File Offset: 0x0001CE9F
			public float AverageReward
			{
				get
				{
					return (this.AverageImmediateReward + this.AverageEnergyReward) / 2f;
				}
			}

			// Token: 0x0400048F RID: 1167
			public int TotalDecisions;

			// Token: 0x04000490 RID: 1168
			public int TotalUpdates;

			// Token: 0x04000491 RID: 1169
			public float AverageImmediateReward;

			// Token: 0x04000492 RID: 1170
			public float AverageEnergyReward;

			// Token: 0x04000493 RID: 1171
			public int PendingRecords;

			// Token: 0x04000494 RID: 1172
			public int ModelVersion;

			// Token: 0x04000495 RID: 1173
			public int PendingEnergyWindows;
		}

		// Token: 0x02000062 RID: 98
		public enum ImmediateRewardMode
		{
			// Token: 0x04000497 RID: 1175
			ExecutionBased,
			// Token: 0x04000498 RID: 1176
			CoreStateBased,
			// Token: 0x04000499 RID: 1177
			Combined
		}
	}
}
