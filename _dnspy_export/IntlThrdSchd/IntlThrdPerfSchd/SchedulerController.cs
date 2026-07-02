using System;
using System.Diagnostics;
using System.Text;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000012 RID: 18
	public class SchedulerController
	{
		// Token: 0x06000105 RID: 261 RVA: 0x0000DA58 File Offset: 0x0000BC58
		public SchedulerController()
		{
			this._scheduler = new TransformerScheduler();
			this._log = new StringBuilder();
		}

		// Token: 0x06000106 RID: 262 RVA: 0x0000DA84 File Offset: 0x0000BC84
		public ScheduleResult Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			ScheduleResult scheduleResult2;
			try
			{
				ScheduleResult scheduleResult = this._scheduler.Schedule(threadFeatures, coreFeatures, numCores);
				stopwatch.Stop();
				this.Log(string.Format("Scheduled to Core {0} in {1:F1} μs", scheduleResult.BestCoreIndex, (double)stopwatch.ElapsedTicks * 1000000.0 / (double)Stopwatch.Frequency));
				scheduleResult2 = scheduleResult;
			}
			catch (Exception ex)
			{
				this.Log("ERROR: " + ex.Message);
				throw;
			}
			return scheduleResult2;
		}

		// Token: 0x06000107 RID: 263 RVA: 0x0000DB14 File Offset: 0x0000BD14
		public void UpdateTAT(float currentTAT, float energyValue, float newMetricValue, float extraMetricValue, int little_num, int big_num)
		{
			this._scheduler.UpdateTAT(currentTAT, energyValue, newMetricValue, extraMetricValue, little_num, big_num);
			this.Log(string.Format("TAT updated: {0:F2}μs, Energy: {1:F2}, Reward: {2:F6}", currentTAT, energyValue, this._scheduler.GetRecentAvgReward()));
		}

		// Token: 0x06000108 RID: 264 RVA: 0x0000DB64 File Offset: 0x0000BD64
		public string GetStatusReport()
		{
			object @lock = this._lock;
			string text;
			lock (@lock)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss}] Transformer Scheduler Status", DateTime.Now));
				stringBuilder.AppendLine(string.Format("Current TAT: {0:F2}ms", this._scheduler.GetCurrentTAT()));
				stringBuilder.AppendLine(string.Format("Baseline TAT: {0:F2}ms", this._scheduler.GetBaselineTAT()));
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(this._scheduler.GetLearningReport());
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(this._scheduler.GetRecentDecisions(10));
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(this._scheduler.ExportModel());
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("=== Recent Log ===");
				stringBuilder.Append(this._log);
				text = stringBuilder.ToString();
			}
			return text;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x0000DC7C File Offset: 0x0000BE7C
		private void Log(string message)
		{
			object @lock = this._lock;
			lock (@lock)
			{
				string text = string.Format("[{0:HH:mm:ss.fff}] {1}", DateTime.Now, message);
				this._log.Insert(0, text + Environment.NewLine);
				if (this._log.Length > 5000)
				{
					this._log.Length = 5000;
				}
			}
		}

		// Token: 0x040001B7 RID: 439
		private readonly TransformerScheduler _scheduler;

		// Token: 0x040001B8 RID: 440
		private readonly StringBuilder _log;

		// Token: 0x040001B9 RID: 441
		private readonly object _lock = new object();
	}
}
