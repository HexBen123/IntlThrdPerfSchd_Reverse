using System;
using System.Collections.Generic;
using System.Linq;

namespace IntlThrdPerfSchd
{
	public class ThreadPerformanceTracker
	{
		private class ThreadData
		{
			public int ThreadId { get; set; }

			public long InstructionsPerCount { get; set; }

			public long LastUpdateTicks { get; set; }
		}

		public class QueryResult
		{
			public int ThreadId { get; set; }

			public long InstructionsPerCount { get; set; }

			public DateTime WindowStartTime { get; set; }

			public DateTime WindowEndTime { get; set; }
		}

		private readonly object _lockObject = new object();

		private readonly TimeSpan _windowDuration;

		private Dictionary<int, ThreadData> _currentWindowData;

		private Dictionary<int, ThreadData> _previousWindowData;

		private long _currentWindowStartTicks;

		private long _previousWindowStartTicks;

		public ThreadPerformanceTracker(TimeSpan windowDuration)
			: this(windowDuration.Ticks)
		{
		}

		public ThreadPerformanceTracker(long windowTicks)
		{
			_windowDuration = TimeSpan.FromTicks(windowTicks);
			_currentWindowData = new Dictionary<int, ThreadData>();
			_previousWindowData = new Dictionary<int, ThreadData>();
			_currentWindowStartTicks = DateTime.Now.Ticks;
			_previousWindowStartTicks = _currentWindowStartTicks - windowTicks;
		}

		public void AddOrUpdateData(int threadId, long instructionsPerCount)
		{
			lock (_lockObject)
			{
				CheckAndSwitchWindow();
				long ticks = DateTime.Now.Ticks;
				if (_currentWindowData.TryGetValue(threadId, out var value))
				{
					if (instructionsPerCount > value.InstructionsPerCount)
					{
						value.InstructionsPerCount = instructionsPerCount;
					}
					value.LastUpdateTicks = ticks;
				}
				else
				{
					_currentWindowData[threadId] = new ThreadData
					{
						ThreadId = threadId,
						InstructionsPerCount = instructionsPerCount,
						LastUpdateTicks = ticks
					};
				}
			}
		}

		public int GetMaxThreadId()
		{
			lock (_lockObject)
			{
				CheckAndSwitchWindow();
				if (_previousWindowData.Any())
				{
					return _previousWindowData.Values.OrderByDescending((ThreadData data) => data.InstructionsPerCount).FirstOrDefault()?.ThreadId ?? (-1);
				}
				if (_currentWindowData.Any())
				{
					return _currentWindowData.Values.OrderByDescending((ThreadData data) => data.InstructionsPerCount).FirstOrDefault()?.ThreadId ?? (-1);
				}
				return -1;
			}
		}

		public List<QueryResult> GetPreviousWindowRanking(int topN = 0)
		{
			lock (_lockObject)
			{
				CheckAndSwitchWindow();
				IEnumerable<QueryResult> source = from data in _previousWindowData.Values
					orderby data.InstructionsPerCount descending
					select new QueryResult
					{
						ThreadId = data.ThreadId,
						InstructionsPerCount = data.InstructionsPerCount,
						WindowStartTime = new DateTime(_previousWindowStartTicks),
						WindowEndTime = new DateTime(_previousWindowStartTicks + _windowDuration.Ticks)
					};
				if (source.Any())
				{
					if (topN > 0)
					{
						source = source.Take(topN);
					}
					return source.ToList();
				}
				if (_currentWindowData.Any())
				{
					IEnumerable<QueryResult> source2 = from data in _currentWindowData.Values
						orderby data.InstructionsPerCount descending
						select new QueryResult
						{
							ThreadId = data.ThreadId,
							InstructionsPerCount = data.InstructionsPerCount,
							WindowStartTime = new DateTime(_currentWindowStartTicks),
							WindowEndTime = DateTime.Now
						};
					if (topN > 0)
					{
						source2 = source2.Take(topN);
					}
					return source2.ToList();
				}
				return new List<QueryResult>();
			}
		}

		public List<QueryResult> GetCurrentWindowPreview(int topN = 0)
		{
			lock (_lockObject)
			{
				IEnumerable<QueryResult> source = from data in _currentWindowData.Values
					orderby data.InstructionsPerCount descending
					select new QueryResult
					{
						ThreadId = data.ThreadId,
						InstructionsPerCount = data.InstructionsPerCount,
						WindowStartTime = new DateTime(_currentWindowStartTicks),
						WindowEndTime = DateTime.Now
					};
				if (topN > 0)
				{
					source = source.Take(topN);
				}
				return source.ToList();
			}
		}

		public void ForceSwitchWindow()
		{
			lock (_lockObject)
			{
				SwitchWindow();
			}
		}

		private void CheckAndSwitchWindow()
		{
			long ticks = DateTime.Now.Ticks;
			long num = _currentWindowStartTicks + _windowDuration.Ticks;
			if (ticks >= num)
			{
				SwitchWindow();
			}
		}

		private void SwitchWindow()
		{
			_previousWindowData = new Dictionary<int, ThreadData>(_currentWindowData);
			_previousWindowStartTicks = _currentWindowStartTicks;
			_currentWindowData = new Dictionary<int, ThreadData>();
			_currentWindowStartTicks = DateTime.Now.Ticks;
		}

		public string GetStatus()
		{
			lock (_lockObject)
			{
				return $"当前窗口: {_currentWindowData.Count} 个线程, 上一个窗口: {_previousWindowData.Count} 个线程, " + $"当前窗口开始: {new DateTime(_currentWindowStartTicks):yyyy-MM-dd HH:mm:ss}, " + $"上一个窗口开始: {new DateTime(_previousWindowStartTicks):yyyy-MM-dd HH:mm:ss}";
			}
		}
	}
}
