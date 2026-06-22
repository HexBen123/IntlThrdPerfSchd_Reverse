using System;
using System.Diagnostics;
using System.Text;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000010 RID: 16
	public class SchedulerController
	{
		// Token: 0x060000FC RID: 252 RVA: 0x0000BCAC File Offset: 0x00009EAC
		public SchedulerController()
		{
			this._scheduler = new TransformerScheduler();
			this._log = new StringBuilder();
		}

		// Token: 0x060000FD RID: 253 RVA: 0x0000BCD8 File Offset: 0x00009ED8
		public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			int num2;
			try
			{
				int num = this._scheduler.Schedule(threadFeatures, coreFeatures, numCores);
				stopwatch.Stop();
				this.Log(string.Format("Scheduled to Core {0} in {1:F1} μs", num, (double)stopwatch.ElapsedTicks * 1000000.0 / (double)Stopwatch.Frequency));
				num2 = num;
			}
			catch (Exception ex)
			{
				this.Log("ERROR: " + ex.Message);
				throw;
			}
			return num2;
		}

		// Token: 0x060000FE RID: 254 RVA: 0x0000BD64 File Offset: 0x00009F64
		public void UpdateTAT(float currentTAT, float energyValue)
		{
			this._scheduler.UpdateTAT(currentTAT, energyValue);
			this.Log(string.Format("TAT updated: {0:F2}μs, Energy: {1:F2}, Reward: {2:F6}", currentTAT, energyValue, this._scheduler.GetRecentAvgReward()));
		}

		// Token: 0x060000FF RID: 255 RVA: 0x0000BDA0 File Offset: 0x00009FA0
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

		// Token: 0x06000100 RID: 256 RVA: 0x0000BEB8 File Offset: 0x0000A0B8
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

		// Token: 0x0400016B RID: 363
		private readonly TransformerScheduler _scheduler;

		// Token: 0x0400016C RID: 364
		private readonly StringBuilder _log;

		// Token: 0x0400016D RID: 365
		private readonly object _lock = new object();
	}
}
