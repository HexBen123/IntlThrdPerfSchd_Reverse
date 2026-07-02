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

		public ScheduleResult Schedule(ReadOnlySpan<float> threadFeatures, float[][] coreFeatures, int numCores)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			try
			{
				ScheduleResult result = _scheduler.Schedule(threadFeatures, coreFeatures, numCores);
				stopwatch.Stop();
				Log($"Scheduled to Core {result.BestCoreIndex} in {(double)stopwatch.ElapsedTicks * 1000000.0 / (double)Stopwatch.Frequency:F1} μs");
				return result;
			}
			catch (Exception ex)
			{
				Log("ERROR: " + ex.Message);
				throw;
			}
		}

		public void UpdateTAT(float currentTAT, float energyValue, float newMetricValue, float extraMetricValue, int little_num, int big_num)
		{
			_scheduler.UpdateTAT(currentTAT, energyValue, newMetricValue, extraMetricValue, little_num, big_num);
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
