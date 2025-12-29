using System.Collections.Immutable;

namespace HexEditor.Formats;

public readonly ref struct IndexOfReadOnlySpanEnumerable<T>
{
	public IndexOfReadOnlySpanEnumerable(ReadOnlySpan<T> span, ReadOnlySpan<T> ch)
	{
		this.span = span;
		this.ch = ch;
	}

	private readonly ReadOnlySpan<T> span;
	private readonly ReadOnlySpan<T> ch;

	public Enumerator GetEnumerator() => new(span, ch);

	public ref struct Enumerator
	{
		public Enumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> ch)
		{
			this.span = span;
			this.ch = ch;
			Reset();
		}

		private readonly ReadOnlySpan<T> span;
		private readonly ReadOnlySpan<T> ch;
		private int _currentIndex;

		public readonly int Current => _currentIndex;

		public void Reset() => _currentIndex = -1;

		public bool MoveNext()
		{
			var startIndex = _currentIndex;
			var newRelativeIndex = span[(_currentIndex + 1)..].IndexOf(ch);
			if (newRelativeIndex == -1)
			{
				return false;
			}

			_currentIndex = startIndex + 1 + newRelativeIndex;
			return true;
		}
	}
}

public static partial class Extensions
{
	extension(ReadOnlySpan<byte> @this)
	{
		public IndexOfReadOnlySpanEnumerable<byte> IndexesOf(ReadOnlySpan<byte> value) => new(@this, value);
	}
}