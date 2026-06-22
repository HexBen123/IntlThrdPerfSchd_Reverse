using System;

namespace IntlThrdPerfSchd
{
	// Token: 0x0200000C RID: 12
	public class CircularBuffer<T>
	{
		// Token: 0x060000B0 RID: 176 RVA: 0x00007F56 File Offset: 0x00006156
		public CircularBuffer(int capacity)
		{
			this._capacity = capacity;
			this._buffer = new T[capacity];
			this._head = 0;
			this._count = 0;
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x060000B1 RID: 177 RVA: 0x00007F7F File Offset: 0x0000617F
		public int Count
		{
			get
			{
				return this._count;
			}
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00007F88 File Offset: 0x00006188
		public void Add(T item)
		{
			this._buffer[this._head] = item;
			this._head = (this._head + 1) % this._capacity;
			if (this._count < this._capacity)
			{
				this._count++;
			}
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00007FD8 File Offset: 0x000061D8
		public T Get(int index)
		{
			if (index < 0 || index >= this._count)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			int num = (this._head - this._count + index + this._capacity) % this._capacity;
			return this._buffer[num];
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00008027 File Offset: 0x00006227
		public Span<T> GetRecent(int n)
		{
			n = Math.Min(n, this._count);
			return new Span<T>(this._buffer, (this._head - n + this._capacity) % this._capacity, n);
		}

		// Token: 0x040000DE RID: 222
		private readonly T[] _buffer;

		// Token: 0x040000DF RID: 223
		private readonly int _capacity;

		// Token: 0x040000E0 RID: 224
		private int _head;

		// Token: 0x040000E1 RID: 225
		private int _count;
	}
}
