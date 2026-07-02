using System;
using System.Text;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000011 RID: 17
	public class SchedulerService
	{
		// Token: 0x060000FE RID: 254 RVA: 0x0000D758 File Offset: 0x0000B958
		public SchedulerService()
		{
			this._statusLog = new StringBuilder();
			this._isRunning = false;
		}

		// Token: 0x060000FF RID: 255 RVA: 0x0000D77D File Offset: 0x0000B97D
		public void Start()
		{
			this._scheduler = new TransformerScheduler();
			this._isRunning = true;
			this.LogStatus("Transformer Scheduler Service Started");
		}

		// Token: 0x06000100 RID: 256 RVA: 0x0000D79C File Offset: 0x0000B99C
		public void Stop()
		{
			this._isRunning = false;
			this.LogStatus("Transformer Scheduler Service Stopped");
		}

		// Token: 0x06000101 RID: 257 RVA: 0x0000D7B0 File Offset: 0x0000B9B0
		public ScheduleResult Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
		{
			ScheduleResult scheduleResult;
			try
			{
				scheduleResult = this._scheduler.Schedule(threadFeatures, coreFeatures, numCores);
			}
			catch (Exception ex)
			{
				this.LogStatus("ERROR: " + ex.Message);
				scheduleResult = new ScheduleResult
				{
					BestCoreIndex = 0,
					AffinityMask = IntPtr.Zero
				};
			}
			return scheduleResult;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x0000D818 File Offset: 0x0000BA18
		public void UpdateTAT(float currentTAT, float energyValue, float newMetricValue, float extraMetricValue, int little_num, int big_num)
		{
			try
			{
				this._scheduler.UpdateTAT(currentTAT, energyValue, newMetricValue, extraMetricValue, little_num, big_num);
			}
			catch (Exception ex)
			{
				this.LogStatus("ERROR in UpdateTAT: " + ex.Message);
			}
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000D864 File Offset: 0x0000BA64
		public string GetStatusReport()
		{
			object @lock = this._lock;
			string text;
			lock (@lock)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss}] Transformer Scheduler Status", DateTime.Now));
				stringBuilder.AppendLine(string.Format("Running: {0}", this._isRunning));
				stringBuilder.AppendLine(string.Format("Current TAT: {0:F2}ms", this._scheduler.GetCurrentTAT()));
				stringBuilder.AppendLine(string.Format("Baseline TAT: {0:F2}ms", this._scheduler.GetBaselineTAT()));
				stringBuilder.AppendLine(string.Format("Recent Reward: {0:F6}", this._scheduler.GetRecentAvgReward()));
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
				TransformerScheduler scheduler = this._scheduler;
				stringBuilder.AppendLine(((scheduler != null) ? scheduler.GetLearningReport() : null) ?? "");
				stringBuilder.AppendLine();
				TransformerScheduler scheduler2 = this._scheduler;
				stringBuilder.AppendLine(((scheduler2 != null) ? scheduler2.GetRecentDecisions(5) : null) ?? "");
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("=== Recent Log ===");
				stringBuilder.Append(this._statusLog);
				text = stringBuilder.ToString();
			}
			return text;
		}

		// Token: 0x06000104 RID: 260 RVA: 0x0000D9CC File Offset: 0x0000BBCC
		private void LogStatus(string message)
		{
			object @lock = this._lock;
			lock (@lock)
			{
				string text = string.Format("[{0:HH:mm:ss}] {1}", DateTime.Now, message);
				this._statusLog.Insert(0, text + Environment.NewLine);
				if (this._statusLog.Length > 10000)
				{
					this._statusLog.Length = 10000;
				}
			}
		}

		// Token: 0x040001B3 RID: 435
		private TransformerScheduler _scheduler;

		// Token: 0x040001B4 RID: 436
		private StringBuilder _statusLog;

		// Token: 0x040001B5 RID: 437
		private readonly object _lock = new object();

		// Token: 0x040001B6 RID: 438
		private bool _isRunning;
	}
}
