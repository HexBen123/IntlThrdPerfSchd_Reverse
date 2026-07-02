using System;
using System.Collections.Generic;
using System.Linq;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000018 RID: 24
	public class ThreadPerformanceTracker
	{
		// Token: 0x0600018C RID: 396 RVA: 0x00015BC4 File Offset: 0x00013DC4
		public ThreadPerformanceTracker(TimeSpan windowDuration)
			: this(windowDuration.Ticks)
		{
		}

		// Token: 0x0600018D RID: 397 RVA: 0x00015BD4 File Offset: 0x00013DD4
		public ThreadPerformanceTracker(long windowTicks)
		{
			this._windowDuration = TimeSpan.FromTicks(windowTicks);
			this._currentWindowData = new Dictionary<int, ThreadPerformanceTracker.ThreadData>();
			this._previousWindowData = new Dictionary<int, ThreadPerformanceTracker.ThreadData>();
			this._currentWindowStartTicks = DateTime.Now.Ticks;
			this._previousWindowStartTicks = this._currentWindowStartTicks - windowTicks;
		}

		// Token: 0x0600018E RID: 398 RVA: 0x00015C38 File Offset: 0x00013E38
		public void AddOrUpdateData(int threadId, long instructionsPerCount)
		{
			object lockObject = this._lockObject;
			lock (lockObject)
			{
				this.CheckAndSwitchWindow();
				long ticks = DateTime.Now.Ticks;
				ThreadPerformanceTracker.ThreadData threadData;
				if (this._currentWindowData.TryGetValue(threadId, out threadData))
				{
					if (instructionsPerCount > threadData.InstructionsPerCount)
					{
						threadData.InstructionsPerCount = instructionsPerCount;
					}
					threadData.LastUpdateTicks = ticks;
				}
				else
				{
					this._currentWindowData[threadId] = new ThreadPerformanceTracker.ThreadData
					{
						ThreadId = threadId,
						InstructionsPerCount = instructionsPerCount,
						LastUpdateTicks = ticks
					};
				}
			}
		}

		// Token: 0x0600018F RID: 399 RVA: 0x00015CD8 File Offset: 0x00013ED8
		public int GetMaxThreadId()
		{
			object lockObject = this._lockObject;
			int num;
			lock (lockObject)
			{
				this.CheckAndSwitchWindow();
				if (this._previousWindowData.Any<KeyValuePair<int, ThreadPerformanceTracker.ThreadData>>())
				{
					ThreadPerformanceTracker.ThreadData threadData = this._previousWindowData.Values.OrderByDescending((ThreadPerformanceTracker.ThreadData data) => data.InstructionsPerCount).FirstOrDefault<ThreadPerformanceTracker.ThreadData>();
					num = ((threadData != null) ? threadData.ThreadId : (-1));
				}
				else if (this._currentWindowData.Any<KeyValuePair<int, ThreadPerformanceTracker.ThreadData>>())
				{
					ThreadPerformanceTracker.ThreadData threadData2 = this._currentWindowData.Values.OrderByDescending((ThreadPerformanceTracker.ThreadData data) => data.InstructionsPerCount).FirstOrDefault<ThreadPerformanceTracker.ThreadData>();
					num = ((threadData2 != null) ? threadData2.ThreadId : (-1));
				}
				else
				{
					num = -1;
				}
			}
			return num;
		}

		// Token: 0x06000190 RID: 400 RVA: 0x00015DBC File Offset: 0x00013FBC
		public List<ThreadPerformanceTracker.QueryResult> GetPreviousWindowRanking(int topN = 0)
		{
			object lockObject = this._lockObject;
			List<ThreadPerformanceTracker.QueryResult> list;
			lock (lockObject)
			{
				this.CheckAndSwitchWindow();
				IEnumerable<ThreadPerformanceTracker.QueryResult> enumerable = from data in this._previousWindowData.Values
					orderby data.InstructionsPerCount descending
					select new ThreadPerformanceTracker.QueryResult
					{
						ThreadId = data.ThreadId,
						InstructionsPerCount = data.InstructionsPerCount,
						WindowStartTime = new DateTime(this._previousWindowStartTicks),
						WindowEndTime = new DateTime(this._previousWindowStartTicks + this._windowDuration.Ticks)
					};
				if (enumerable.Any<ThreadPerformanceTracker.QueryResult>())
				{
					if (topN > 0)
					{
						enumerable = enumerable.Take(topN);
					}
					list = enumerable.ToList<ThreadPerformanceTracker.QueryResult>();
				}
				else if (this._currentWindowData.Any<KeyValuePair<int, ThreadPerformanceTracker.ThreadData>>())
				{
					IEnumerable<ThreadPerformanceTracker.QueryResult> enumerable2 = from data in this._currentWindowData.Values
						orderby data.InstructionsPerCount descending
						select new ThreadPerformanceTracker.QueryResult
						{
							ThreadId = data.ThreadId,
							InstructionsPerCount = data.InstructionsPerCount,
							WindowStartTime = new DateTime(this._currentWindowStartTicks),
							WindowEndTime = DateTime.Now
						};
					if (topN > 0)
					{
						enumerable2 = enumerable2.Take(topN);
					}
					list = enumerable2.ToList<ThreadPerformanceTracker.QueryResult>();
				}
				else
				{
					list = new List<ThreadPerformanceTracker.QueryResult>();
				}
			}
			return list;
		}

		// Token: 0x06000191 RID: 401 RVA: 0x00015EC8 File Offset: 0x000140C8
		public List<ThreadPerformanceTracker.QueryResult> GetCurrentWindowPreview(int topN = 0)
		{
			object lockObject = this._lockObject;
			List<ThreadPerformanceTracker.QueryResult> list;
			lock (lockObject)
			{
				IEnumerable<ThreadPerformanceTracker.QueryResult> enumerable = from data in this._currentWindowData.Values
					orderby data.InstructionsPerCount descending
					select new ThreadPerformanceTracker.QueryResult
					{
						ThreadId = data.ThreadId,
						InstructionsPerCount = data.InstructionsPerCount,
						WindowStartTime = new DateTime(this._currentWindowStartTicks),
						WindowEndTime = DateTime.Now
					};
				if (topN > 0)
				{
					enumerable = enumerable.Take(topN);
				}
				list = enumerable.ToList<ThreadPerformanceTracker.QueryResult>();
			}
			return list;
		}

		// Token: 0x06000192 RID: 402 RVA: 0x00015F58 File Offset: 0x00014158
		public void ForceSwitchWindow()
		{
			object lockObject = this._lockObject;
			lock (lockObject)
			{
				this.SwitchWindow();
			}
		}

		// Token: 0x06000193 RID: 403 RVA: 0x00015F98 File Offset: 0x00014198
		private void CheckAndSwitchWindow()
		{
			long ticks = DateTime.Now.Ticks;
			long num = this._currentWindowStartTicks + this._windowDuration.Ticks;
			if (ticks >= num)
			{
				this.SwitchWindow();
			}
		}

		// Token: 0x06000194 RID: 404 RVA: 0x00015FD4 File Offset: 0x000141D4
		private void SwitchWindow()
		{
			this._previousWindowData = new Dictionary<int, ThreadPerformanceTracker.ThreadData>(this._currentWindowData);
			this._previousWindowStartTicks = this._currentWindowStartTicks;
			this._currentWindowData = new Dictionary<int, ThreadPerformanceTracker.ThreadData>();
			this._currentWindowStartTicks = DateTime.Now.Ticks;
		}

		// Token: 0x06000195 RID: 405 RVA: 0x0001601C File Offset: 0x0001421C
		public string GetStatus()
		{
			object lockObject = this._lockObject;
			string text;
			lock (lockObject)
			{
				text = string.Format("当前窗口: {0} 个线程, 上一个窗口: {1} 个线程, ", this._currentWindowData.Count, this._previousWindowData.Count) + string.Format("当前窗口开始: {0:yyyy-MM-dd HH:mm:ss}, ", new DateTime(this._currentWindowStartTicks)) + string.Format("上一个窗口开始: {0:yyyy-MM-dd HH:mm:ss}", new DateTime(this._previousWindowStartTicks));
			}
			return text;
		}

		// Token: 0x04000433 RID: 1075
		private readonly object _lockObject = new object();

		// Token: 0x04000434 RID: 1076
		private readonly TimeSpan _windowDuration;

		// Token: 0x04000435 RID: 1077
		private Dictionary<int, ThreadPerformanceTracker.ThreadData> _currentWindowData;

		// Token: 0x04000436 RID: 1078
		private Dictionary<int, ThreadPerformanceTracker.ThreadData> _previousWindowData;

		// Token: 0x04000437 RID: 1079
		private long _currentWindowStartTicks;

		// Token: 0x04000438 RID: 1080
		private long _previousWindowStartTicks;

		// Token: 0x0200009F RID: 159
		private class ThreadData
		{
			// Token: 0x17000243 RID: 579
			// (get) Token: 0x06000832 RID: 2098 RVA: 0x0002B008 File Offset: 0x00029208
			// (set) Token: 0x06000833 RID: 2099 RVA: 0x0002B010 File Offset: 0x00029210
			public int ThreadId { get; set; }

			// Token: 0x17000244 RID: 580
			// (get) Token: 0x06000834 RID: 2100 RVA: 0x0002B019 File Offset: 0x00029219
			// (set) Token: 0x06000835 RID: 2101 RVA: 0x0002B021 File Offset: 0x00029221
			public long InstructionsPerCount { get; set; }

			// Token: 0x17000245 RID: 581
			// (get) Token: 0x06000836 RID: 2102 RVA: 0x0002B02A File Offset: 0x0002922A
			// (set) Token: 0x06000837 RID: 2103 RVA: 0x0002B032 File Offset: 0x00029232
			public long LastUpdateTicks { get; set; }
		}

		// Token: 0x020000A0 RID: 160
		public class QueryResult
		{
			// Token: 0x17000246 RID: 582
			// (get) Token: 0x06000839 RID: 2105 RVA: 0x0002B043 File Offset: 0x00029243
			// (set) Token: 0x0600083A RID: 2106 RVA: 0x0002B04B File Offset: 0x0002924B
			public int ThreadId { get; set; }

			// Token: 0x17000247 RID: 583
			// (get) Token: 0x0600083B RID: 2107 RVA: 0x0002B054 File Offset: 0x00029254
			// (set) Token: 0x0600083C RID: 2108 RVA: 0x0002B05C File Offset: 0x0002925C
			public long InstructionsPerCount { get; set; }

			// Token: 0x17000248 RID: 584
			// (get) Token: 0x0600083D RID: 2109 RVA: 0x0002B065 File Offset: 0x00029265
			// (set) Token: 0x0600083E RID: 2110 RVA: 0x0002B06D File Offset: 0x0002926D
			public DateTime WindowStartTime { get; set; }

			// Token: 0x17000249 RID: 585
			// (get) Token: 0x0600083F RID: 2111 RVA: 0x0002B076 File Offset: 0x00029276
			// (set) Token: 0x06000840 RID: 2112 RVA: 0x0002B07E File Offset: 0x0002927E
			public DateTime WindowEndTime { get; set; }
		}
	}
}
