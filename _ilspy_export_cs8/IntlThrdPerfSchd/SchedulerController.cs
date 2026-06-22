using System;
using System.Diagnostics;
using System.Text;

namespace IntlThrdPerfSchd
{
	public class SchedulerController
	{
		private readonly TransformerScheduler _scheduler;

		private readonly StringBuilder _log;

		private readonly object _lock = new object();

		public SchedulerController()
		{
			_scheduler = new TransformerScheduler();
			_log = new StringBuilder();
		}

		public int Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			try
			{
				int num = _scheduler.Schedule(threadFeatures, coreFeatures, numCores);
				stopwatch.Stop();
				Log($"Scheduled to Core {num} in {(double)stopwatch.ElapsedTicks * 1000000.0 / (double)Stopwatch.Frequency:F1} μs");
				return num;
			}
			catch (Exception ex)
			{
				Log("ERROR: " + ex.Message);
				throw;
			}
		}

		public void UpdateTAT(float currentTAT, float energyValue)
		{
			_scheduler.UpdateTAT(currentTAT, energyValue);
			Log($"TAT updated: {currentTAT:F2}μs, Energy: {energyValue:F2}, Reward: {_scheduler.GetRecentAvgReward():F6}");
		}

		public string GetStatusReport()
		{
			lock (_lock)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Transformer Scheduler Status");
				stringBuilder.AppendLine($"Current TAT: {_scheduler.GetCurrentTAT():F2}ms");
				stringBuilder.AppendLine($"Baseline TAT: {_scheduler.GetBaselineTAT():F2}ms");
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(_scheduler.GetLearningReport());
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(_scheduler.GetRecentDecisions());
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(_scheduler.ExportModel());
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("=== Recent Log ===");
				stringBuilder.Append(_log);
				return stringBuilder.ToString();
			}
		}

		private void Log(string message)
		{
			lock (_lock)
			{
				string text = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
				_log.Insert(0, text + Environment.NewLine);
				if (_log.Length > 5000)
				{
					_log.Length = 5000;
				}
			}
		}
	}
}
