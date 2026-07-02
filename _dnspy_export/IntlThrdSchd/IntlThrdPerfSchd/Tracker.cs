using System;
using System.Collections.Generic;
using System.Linq;

namespace IntlThrdPerfSchd
{
	// Token: 0x02000017 RID: 23
	public class Tracker
	{
		// Token: 0x06000180 RID: 384 RVA: 0x000156E8 File Offset: 0x000138E8
		public Tracker(int windowSize = 1000)
		{
			if (windowSize <= 0)
			{
				throw new ArgumentException("窗口大小必须大于0", "windowSize");
			}
			this._windowSize = windowSize;
			this._maxNeedsRecalculation = false;
		}

		// Token: 0x06000181 RID: 385 RVA: 0x00015728 File Offset: 0x00013928
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

		// Token: 0x06000182 RID: 386 RVA: 0x00015780 File Offset: 0x00013980
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

		// Token: 0x06000183 RID: 387 RVA: 0x00015810 File Offset: 0x00013A10
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

		// Token: 0x06000184 RID: 388 RVA: 0x00015870 File Offset: 0x00013A70
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

		// Token: 0x06000185 RID: 389 RVA: 0x000158C4 File Offset: 0x00013AC4
		private void RecalculateMaxValue()
		{
			this._currentMax = this._dataList.OrderByDescending((Tracker.ThreadData d) => d.Value).FirstOrDefault<Tracker.ThreadData>();
			this._maxNeedsRecalculation = false;
		}

		// Token: 0x06000186 RID: 390 RVA: 0x00015904 File Offset: 0x00013B04
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

		// Token: 0x06000187 RID: 391 RVA: 0x00015974 File Offset: 0x00013B74
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
		// (get) Token: 0x06000188 RID: 392 RVA: 0x000159F4 File Offset: 0x00013BF4
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

		// Token: 0x06000189 RID: 393 RVA: 0x00015A3C File Offset: 0x00013C3C
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
		// (get) Token: 0x0600018A RID: 394 RVA: 0x00015A90 File Offset: 0x00013C90
		public int WindowSize
		{
			get
			{
				return this._windowSize;
			}
		}

		// Token: 0x0600018B RID: 395 RVA: 0x00015A98 File Offset: 0x00013C98
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

		// Token: 0x0400042E RID: 1070
		private readonly object _lockObject = new object();

		// Token: 0x0400042F RID: 1071
		private readonly LinkedList<Tracker.ThreadData> _dataList = new LinkedList<Tracker.ThreadData>();

		// Token: 0x04000430 RID: 1072
		private readonly int _windowSize;

		// Token: 0x04000431 RID: 1073
		private Tracker.ThreadData _currentMax;

		// Token: 0x04000432 RID: 1074
		private bool _maxNeedsRecalculation;

		// Token: 0x0200009D RID: 157
		private class ThreadData
		{
			// Token: 0x17000240 RID: 576
			// (get) Token: 0x06000828 RID: 2088 RVA: 0x0002AF83 File Offset: 0x00029183
			// (set) Token: 0x06000829 RID: 2089 RVA: 0x0002AF8B File Offset: 0x0002918B
			public int ThreadId { get; set; }

			// Token: 0x17000241 RID: 577
			// (get) Token: 0x0600082A RID: 2090 RVA: 0x0002AF94 File Offset: 0x00029194
			// (set) Token: 0x0600082B RID: 2091 RVA: 0x0002AF9C File Offset: 0x0002919C
			public double Value { get; set; }

			// Token: 0x17000242 RID: 578
			// (get) Token: 0x0600082C RID: 2092 RVA: 0x0002AFA5 File Offset: 0x000291A5
			// (set) Token: 0x0600082D RID: 2093 RVA: 0x0002AFAD File Offset: 0x000291AD
			public long Timestamp { get; set; }

			// Token: 0x0600082E RID: 2094 RVA: 0x0002AFB8 File Offset: 0x000291B8
			public ThreadData(int threadId, double value)
			{
				this.ThreadId = threadId;
				this.Value = value;
				this.Timestamp = DateTime.Now.Ticks;
			}
		}
	}
}
