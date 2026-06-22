using System;
using System.Text;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200000F RID: 15
	public class SchedulerService
	{
		// Token: 0x060000F5 RID: 245 RVA: 0x0000B9CC File Offset: 0x00009BCC
		public SchedulerService()
		{
			this._statusLog = new StringBuilder();
			this._isRunning = false;
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x0000B9F1 File Offset: 0x00009BF1
		public void Start()
		{
			this._scheduler = new TransformerScheduler();
			this._isRunning = true;
			this.LogStatus("Transformer Scheduler Service Started");
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000BA10 File Offset: 0x00009C10
		public void Stop()
		{
			this._isRunning = false;
			this.LogStatus("Transformer Scheduler Service Stopped");
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x0000BA24 File Offset: 0x00009C24
		public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
		{
			int num;
			try
			{
				num = this._scheduler.Schedule(threadFeatures, coreFeatures, numCores);
			}
			catch (Exception ex)
			{
				this.LogStatus("ERROR: " + ex.Message);
				num = 0;
			}
			return num;
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000BA70 File Offset: 0x00009C70
		public void UpdateTAT(float currentTAT, float energyValue)
		{
			try
			{
				this._scheduler.UpdateTAT(currentTAT, energyValue);
			}
			catch (Exception ex)
			{
				this.LogStatus("ERROR in UpdateTAT: " + ex.Message);
			}
		}

		// Token: 0x060000FA RID: 250 RVA: 0x0000BAB8 File Offset: 0x00009CB8
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

		// Token: 0x060000FB RID: 251 RVA: 0x0000BC20 File Offset: 0x00009E20
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

		// Token: 0x04000167 RID: 359
		private TransformerScheduler _scheduler;

		// Token: 0x04000168 RID: 360
		private StringBuilder _statusLog;

		// Token: 0x04000169 RID: 361
		private readonly object _lock = new object();

		// Token: 0x0400016A RID: 362
		private bool _isRunning;
	}
}
