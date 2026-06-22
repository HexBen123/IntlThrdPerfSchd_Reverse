using System;
using System.Collections.Generic;
using System.Linq;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000015 RID: 21
	public class Tracker
	{
		// Token: 0x06000171 RID: 369 RVA: 0x000133A4 File Offset: 0x000115A4
		public Tracker(int windowSize = 1000)
		{
			if (windowSize <= 0)
			{
				throw new ArgumentException("窗口大小必须大于0", "windowSize");
			}
			this._windowSize = windowSize;
			this._maxNeedsRecalculation = false;
		}

		// Token: 0x06000172 RID: 370 RVA: 0x000133E4 File Offset: 0x000115E4
		public void AddData(int threadId, double value)
		{
			Tracker.ThreadData threadData = new Tracker.ThreadData(threadId, value);
			object lockObject = this._lockObject;
			lock (lockObject)
			{
				this.InsertInOrder(threadData);
				this.MaintainWindowSize();
				this.UpdateMaxValue(threadData);
			}
		}

		// Token: 0x06000173 RID: 371 RVA: 0x0001343C File Offset: 0x0001163C
		private void InsertInOrder(Tracker.ThreadData newData)
		{
			if (this._dataList.Count == 0 || newData.Timestamp >= this._dataList.Last.Value.Timestamp)
			{
				this._dataList.AddLast(newData);
				return;
			}
			LinkedListNode<Tracker.ThreadData> linkedListNode = this._dataList.First;
			while (linkedListNode != null && linkedListNode.Value.Timestamp <= newData.Timestamp)
			{
				linkedListNode = linkedListNode.Next;
			}
			if (linkedListNode == null)
			{
				this._dataList.AddLast(newData);
				return;
			}
			this._dataList.AddBefore(linkedListNode, newData);
		}

		// Token: 0x06000174 RID: 372 RVA: 0x000134CC File Offset: 0x000116CC
		private void MaintainWindowSize()
		{
			while (this._dataList.Count > this._windowSize)
			{
				Tracker.ThreadData value = this._dataList.First.Value;
				this._dataList.RemoveFirst();
				if (this._currentMax != null && value.Timestamp == this._currentMax.Timestamp)
				{
					this._maxNeedsRecalculation = true;
				}
			}
		}

		// Token: 0x06000175 RID: 373 RVA: 0x0001352C File Offset: 0x0001172C
		private void UpdateMaxValue(Tracker.ThreadData newData)
		{
			if (this._currentMax == null)
			{
				this._currentMax = newData;
				this._maxNeedsRecalculation = false;
				return;
			}
			if (newData.Value > this._currentMax.Value)
			{
				this._currentMax = newData;
				this._maxNeedsRecalculation = false;
				return;
			}
			if (this._maxNeedsRecalculation)
			{
				this.RecalculateMaxValue();
			}
		}

		// Token: 0x06000176 RID: 374 RVA: 0x00013580 File Offset: 0x00011780
		private void RecalculateMaxValue()
		{
			this._currentMax = this._dataList.OrderByDescending((Tracker.ThreadData d) => d.Value).FirstOrDefault<Tracker.ThreadData>();
			this._maxNeedsRecalculation = false;
		}

		// Token: 0x06000177 RID: 375 RVA: 0x000135C0 File Offset: 0x000117C0
		public int GetMaxThreadId()
		{
			object lockObject = this._lockObject;
			int num;
			lock (lockObject)
			{
				if (this._dataList.Count == 0)
				{
					num = -1;
				}
				else
				{
					if (this._maxNeedsRecalculation)
					{
						this.RecalculateMaxValue();
					}
					Tracker.ThreadData currentMax = this._currentMax;
					num = ((currentMax != null) ? currentMax.ThreadId : (-1));
				}
			}
			return num;
		}

		// Token: 0x06000178 RID: 376 RVA: 0x00013630 File Offset: 0x00011830
		public double GetMaxValue()
		{
			object lockObject = this._lockObject;
			double num;
			lock (lockObject)
			{
				if (this._dataList.Count == 0)
				{
					num = double.MinValue;
				}
				else
				{
					if (this._maxNeedsRecalculation)
					{
						this.RecalculateMaxValue();
					}
					Tracker.ThreadData currentMax = this._currentMax;
					num = ((currentMax != null) ? currentMax.Value : double.MinValue);
				}
			}
			return num;
		}

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000179 RID: 377 RVA: 0x000136B0 File Offset: 0x000118B0
		public int DataCount
		{
			get
			{
				object lockObject = this._lockObject;
				int count;
				lock (lockObject)
				{
					count = this._dataList.Count;
				}
				return count;
			}
		}

		// Token: 0x0600017A RID: 378 RVA: 0x000136F8 File Offset: 0x000118F8
		public void Clear()
		{
			object lockObject = this._lockObject;
			lock (lockObject)
			{
				this._dataList.Clear();
				this._currentMax = null;
				this._maxNeedsRecalculation = false;
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x0600017B RID: 379 RVA: 0x0001374C File Offset: 0x0001194C
		public int WindowSize
		{
			get
			{
				return this._windowSize;
			}
		}

		// Token: 0x0600017C RID: 380 RVA: 0x00013754 File Offset: 0x00011954
		public string GetStatistics()
		{
			object lockObject = this._lockObject;
			string text;
			lock (lockObject)
			{
				if (this._dataList.Count == 0)
				{
					text = "没有数据";
				}
				else
				{
					List<string> list = new List<string>
					{
						string.Format("数据总数: {0}", this._dataList.Count),
						string.Format("窗口大小: {0}", this._windowSize),
						string.Format("最早时间戳: {0}", this._dataList.First.Value.Timestamp),
						string.Format("最晚时间戳: {0}", this._dataList.Last.Value.Timestamp),
						string.Format("最大值: {0}", this.GetMaxValue()),
						string.Format("最大值线程ID: {0}", this.GetMaxThreadId())
					};
					text = string.Join(Environment.NewLine, list);
				}
			}
			return text;
		}

		// Token: 0x040003CD RID: 973
		private readonly object _lockObject = new object();

		// Token: 0x040003CE RID: 974
		private readonly LinkedList<Tracker.ThreadData> _dataList = new LinkedList<Tracker.ThreadData>();

		// Token: 0x040003CF RID: 975
		private readonly int _windowSize;

		// Token: 0x040003D0 RID: 976
		private Tracker.ThreadData _currentMax;

		// Token: 0x040003D1 RID: 977
		private bool _maxNeedsRecalculation;

		// Token: 0x02000098 RID: 152
		private class ThreadData
		{
			// Token: 0x17000221 RID: 545
			// (get) Token: 0x060007C2 RID: 1986 RVA: 0x00026DF3 File Offset: 0x00024FF3
			// (set) Token: 0x060007C3 RID: 1987 RVA: 0x00026DFB File Offset: 0x00024FFB
			public int ThreadId { get; set; }

			// Token: 0x17000222 RID: 546
			// (get) Token: 0x060007C4 RID: 1988 RVA: 0x00026E04 File Offset: 0x00025004
			// (set) Token: 0x060007C5 RID: 1989 RVA: 0x00026E0C File Offset: 0x0002500C
			public double Value { get; set; }

			// Token: 0x17000223 RID: 547
			// (get) Token: 0x060007C6 RID: 1990 RVA: 0x00026E15 File Offset: 0x00025015
			// (set) Token: 0x060007C7 RID: 1991 RVA: 0x00026E1D File Offset: 0x0002501D
			public long Timestamp { get; set; }

			// Token: 0x060007C8 RID: 1992 RVA: 0x00026E28 File Offset: 0x00025028
			public ThreadData(int threadId, double value)
			{
				this.ThreadId = threadId;
				this.Value = value;
				this.Timestamp = DateTime.Now.Ticks;
			}
		}
	}
}
