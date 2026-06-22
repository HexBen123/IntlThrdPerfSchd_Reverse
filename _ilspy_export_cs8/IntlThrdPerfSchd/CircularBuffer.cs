using System;

namespace IntlThrdPerfSchd
{
	public class CircularBuffer<T>
	{
		private readonly T[] _buffer;

		private readonly int _capacity;

		private int _head;

		private int _count;

		public int Count => _count;

		public CircularBuffer(int capacity)
		{
			_capacity = capacity;
			_buffer = new T[capacity];
			_head = 0;
			_count = 0;
		}

		public void Add(T item)
		{
			_buffer[_head] = item;
			_head = (_head + 1) % _capacity;
			if (_count < _capacity)
			{
				_count++;
			}
		}

		public T Get(int index)
		{
			if (index < 0 || index >= _count)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			int num = (_head - _count + index + _capacity) % _capacity;
			return _buffer[num];
		}

		public Span<T> GetRecent(int n)
		{
			n = Math.Min(n, _count);
			return new Span<T>(_buffer, (_head - n + _capacity) % _capacity, n);
		}
	}
}
