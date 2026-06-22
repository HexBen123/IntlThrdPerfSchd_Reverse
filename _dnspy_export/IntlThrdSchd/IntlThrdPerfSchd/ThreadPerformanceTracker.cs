using System;
using System.Collections.Generic;
using System.Linq;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000016 RID: 22
	public class ThreadPerformanceTracker
	{
		// Token: 0x0600017D RID: 381 RVA: 0x00013880 File Offset: 0x00011A80
		public ThreadPerformanceTracker(TimeSpan windowDuration)
			: this(windowDuration.Ticks)
		{
		}

		// Token: 0x0600017E RID: 382 RVA: 0x00013890 File Offset: 0x00011A90
		public ThreadPerformanceTracker(long windowTicks)
		{
			this._windowDuration = TimeSpan.FromTicks(windowTicks);
			this._currentWindowData = new Dictionary<int, ThreadPerformanceTracker.ThreadData>();
			this._previousWindowData = new Dictionary<int, ThreadPerformanceTracker.ThreadData>();
			this._currentWindowStartTicks = DateTime.Now.Ticks;
			this._previousWindowStartTicks = this._currentWindowStartTicks - windowTicks;
		}

		// Token: 0x0600017F RID: 383 RVA: 0x000138F4 File Offset: 0x00011AF4
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

		// Token: 0x06000180 RID: 384 RVA: 0x00013994 File Offset: 0x00011B94
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

		// Token: 0x06000181 RID: 385 RVA: 0x00013A78 File Offset: 0x00011C78
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

		// Token: 0x06000182 RID: 386 RVA: 0x00013B84 File Offset: 0x00011D84
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

		// Token: 0x06000183 RID: 387 RVA: 0x00013C14 File Offset: 0x00011E14
		public void ForceSwitchWindow()
		{
			object lockObject = this._lockObject;
			lock (lockObject)
			{
				this.SwitchWindow();
			}
		}

		// Token: 0x06000184 RID: 388 RVA: 0x00013C54 File Offset: 0x00011E54
		private void CheckAndSwitchWindow()
		{
			long ticks = DateTime.Now.Ticks;
			long num = this._currentWindowStartTicks + this._windowDuration.Ticks;
			if (ticks >= num)
			{
				this.SwitchWindow();
			}
		}

		// Token: 0x06000185 RID: 389 RVA: 0x00013C90 File Offset: 0x00011E90
		private void SwitchWindow()
		{
			this._previousWindowData = new Dictionary<int, ThreadPerformanceTracker.ThreadData>(this._currentWindowData);
			this._previousWindowStartTicks = this._currentWindowStartTicks;
			this._currentWindowData = new Dictionary<int, ThreadPerformanceTracker.ThreadData>();
			this._currentWindowStartTicks = DateTime.Now.Ticks;
		}

		// Token: 0x06000186 RID: 390 RVA: 0x00013CD8 File Offset: 0x00011ED8
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

		// Token: 0x040003D2 RID: 978
		private readonly object _lockObject = new object();

		// Token: 0x040003D3 RID: 979
		private readonly TimeSpan _windowDuration;

		// Token: 0x040003D4 RID: 980
		private Dictionary<int, ThreadPerformanceTracker.ThreadData> _currentWindowData;

		// Token: 0x040003D5 RID: 981
		private Dictionary<int, ThreadPerformanceTracker.ThreadData> _previousWindowData;

		// Token: 0x040003D6 RID: 982
		private long _currentWindowStartTicks;

		// Token: 0x040003D7 RID: 983
		private long _previousWindowStartTicks;

		// Token: 0x0200009A RID: 154
		private class ThreadData
		{
			// Token: 0x17000224 RID: 548
			// (get) Token: 0x060007CC RID: 1996 RVA: 0x00026E78 File Offset: 0x00025078
			// (set) Token: 0x060007CD RID: 1997 RVA: 0x00026E80 File Offset: 0x00025080
			public int ThreadId { get; set; }

			// Token: 0x17000225 RID: 549
			// (get) Token: 0x060007CE RID: 1998 RVA: 0x00026E89 File Offset: 0x00025089
			// (set) Token: 0x060007CF RID: 1999 RVA: 0x00026E91 File Offset: 0x00025091
			public long InstructionsPerCount { get; set; }

			// Token: 0x17000226 RID: 550
			// (get) Token: 0x060007D0 RID: 2000 RVA: 0x00026E9A File Offset: 0x0002509A
			// (set) Token: 0x060007D1 RID: 2001 RVA: 0x00026EA2 File Offset: 0x000250A2
			public long LastUpdateTicks { get; set; }
		}

		// Token: 0x0200009B RID: 155
		public class QueryResult
		{
			// Token: 0x17000227 RID: 551
			// (get) Token: 0x060007D3 RID: 2003 RVA: 0x00026EB3 File Offset: 0x000250B3
			// (set) Token: 0x060007D4 RID: 2004 RVA: 0x00026EBB File Offset: 0x000250BB
			public int ThreadId { get; set; }

			// Token: 0x17000228 RID: 552
			// (get) Token: 0x060007D5 RID: 2005 RVA: 0x00026EC4 File Offset: 0x000250C4
			// (set) Token: 0x060007D6 RID: 2006 RVA: 0x00026ECC File Offset: 0x000250CC
			public long InstructionsPerCount { get; set; }

			// Token: 0x17000229 RID: 553
			// (get) Token: 0x060007D7 RID: 2007 RVA: 0x00026ED5 File Offset: 0x000250D5
			// (set) Token: 0x060007D8 RID: 2008 RVA: 0x00026EDD File Offset: 0x000250DD
			public DateTime WindowStartTime { get; set; }

			// Token: 0x1700022A RID: 554
			// (get) Token: 0x060007D9 RID: 2009 RVA: 0x00026EE6 File Offset: 0x000250E6
			// (set) Token: 0x060007DA RID: 2010 RVA: 0x00026EEE File Offset: 0x000250EE
			public DateTime WindowEndTime { get; set; }
		}
	}
}
