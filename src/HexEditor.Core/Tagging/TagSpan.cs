using HexEditor.Model;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace HexEditor.Core.Tagging;

public readonly record struct TagSpan<TTag>(SnapshotSpan Span, TTag Tag) where TTag : ITag
{
	public static implicit operator TagSpan(TagSpan<TTag> tagSpan) => new(tagSpan.Span, tagSpan.Tag);
}

public readonly record struct TagSpan(SnapshotSpan Span, ITag Tag);

public static partial class Extensions
{
	extension<TTag>(TagSpan<TTag>[] spans) where TTag : ITag
	{
		internal TagSpan<TTag>[] OverlapsWith(SnapshotSpan span)
		{
			for (var i = 0; i < spans.Length; i++)
			{
				if (!spans[i].Span.Span.OverlapsWith(span.Span))
				{
					using var result = new PooledArrayBuilder<TagSpan<TTag>>();

					if (i > 0)
					{
						result.AddRange(spans.AsSpan(..i));
					}

					for (var j = i + 1; j < spans.Length; j++)
					{
						var tag = spans[j];
						if (tag.Span.Span.OverlapsWith(span.Span))
						{
							result.Add(tag);
						}
					}

					return result.ToArray();
				}
			}
			return spans;
		}
	}

	extension<TTag>(ImmutableArray<TagSpan<TTag>> spans) where TTag : ITag
	{
		public ImmutableArray<TagSpan> ToNonGeneric()
		{
			if (spans.IsEmpty)
			{
				return [];
			}

			var result = new TagSpan[spans.Length];
			for (var i = 0; i < spans.Length; i++)
			{
				result[i] = spans[i];
			}
			return ImmutableCollectionsMarshal.AsImmutableArray(result);
		}

		public void CopyTo(Span<TagSpan> destination)
		{
			if (spans.IsEmpty)
			{
				return;
			}

			for (var i = 0; i < spans.Length; i++)
			{
				destination[i] = spans[i];
			}
		}

		public ImmutableArray<TagSpan<TTag>> OverlapsWith(SnapshotSpan span)
		{
			var array = ImmutableCollectionsMarshal.AsArray(spans);
			if (array == null || array.Length == 0)
			{
				return [];
			}

			var result = array.OverlapsWith(span);
			return ImmutableCollectionsMarshal.AsImmutableArray(result);
		}
	}
}