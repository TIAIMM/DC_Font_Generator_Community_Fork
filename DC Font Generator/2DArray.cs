using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Array2D
{
	public abstract class SparseArray2DBase<T>
	{
		protected readonly Dictionary<(int, int), T> dictionary = new();
		protected int maxX = -1;
		protected int maxY = -1;
		protected bool isEmpty = true;

		public int MaxX => maxX;
		public int MaxY => maxY;
		public bool IsEmpty => isEmpty;
		public Dictionary<(int, int), T> GetAllItems => new(dictionary);

		protected virtual T DefaultValue => default;

		public T this[int x, int y]
		{
			get => dictionary.TryGetValue((x, y), out T value) ? value : DefaultValue;
			set
			{
				var key = (x, y);

				if (EqualityComparer<T>.Default.Equals(value, DefaultValue))
				{
					if (dictionary.Remove(key))
					{
						UpdateBoundsAfterRemove();
					}
				}
				else
				{
					dictionary[key] = value;
					UpdateBoundsAfterAdd(x, y);
				}
			}
		}

		public void Clear()
		{
			dictionary.Clear();
			maxX = -1;
			maxY = -1;
			isEmpty = true;
		}

		private void UpdateBoundsAfterAdd(int x, int y)
		{
			isEmpty = false;
			if (x > maxX) maxX = x;
			if (y > maxY) maxY = y;
		}

		private void UpdateBoundsAfterRemove()
		{
			if (dictionary.Count == 0)
			{
				isEmpty = true;
				maxX = -1;
				maxY = -1;
			}
			else
			{
				maxX = dictionary.Keys.Max(k => k.Item1);
				maxY = dictionary.Keys.Max(k => k.Item2);
			}
		}
	}

	public class List2D<T> : SparseArray2DBase<T>, IEnumerable<T>
	{
		public IEnumerator<T> GetEnumerator() => dictionary.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	public class Char2D : SparseArray2DBase<char>
	{
		protected override char DefaultValue => '\0';
	}
}