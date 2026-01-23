using HexEditor.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace HexEditor.Core.Tagging;

public class TagSpanSplitMap
{
	public TagSpanSplitMap(IEnumerable<TagSpan> spans)
	{
		_spans = [.. spans];
		_spans.Sort(TagSpanComparer.Instance);
	}
	private TagSpanSplitMap(TagSpan[] spans)
	{
		_spans = spans;
	}

	private readonly TagSpan[] _spans;

	private static readonly TagSpanSplitMap Empty = new(Array.Empty<TagSpan>());

	public ImmutableArray<TagSpan> GetOverlappingTags(SnapshotSpan span)
	{
		using var builder = new PooledArrayBuilder<TagSpan>();
		foreach (var tagSpan in _spans)
		{
			if (tagSpan.Span.Span.OverlapsWith(span.Span))
			{
				builder.Add(tagSpan);
			}
			else if (tagSpan.Span.Start.Position > span.End.Position)
			{
				break;
			}
		}
		return builder.ToImmutableArray();
	}

	public TagSpanSplitMap Slice(SnapshotSpan span)
	{
		var overlappingTags = GetOverlappingTags(span);
		if (overlappingTags.IsEmpty)
		{
			return Empty;
		}

		return new TagSpanSplitMap(ImmutableCollectionsMarshal.AsArray(overlappingTags)!);
	}

	public void GetClosestSplitPoint(SnapshotSpan span, out SnapshotSpan firstSpan, out ImmutableArray<TagSpan> tags)
	{
		// find closest intersection point
		var closestIntersectionPoint = 0L;
		foreach (var tagSpan in _spans)
		{
			// check intersection
			if (tagSpan.Span.Span.OverlapsWith(span.Span))
			{
				// adjust closest split point
				var tagIntersectionPoint = span.Span.Length;
				if (span.Contains(tagSpan.Span.End))
				{
					var relativeEnd = tagSpan.Span.End.Position - span.Start.Position;
					if (relativeEnd < tagIntersectionPoint)
					{
						tagIntersectionPoint = relativeEnd;
					}
				}
				if (span.Contains(tagSpan.Span.Start))
				{
					var relativeStart = tagSpan.Span.Start.Position - span.Start.Position;
					if (relativeStart < tagIntersectionPoint)
					{
						tagIntersectionPoint = relativeStart;
					}
				}

				if (closestIntersectionPoint == 0L)
				{
					closestIntersectionPoint = tagIntersectionPoint;
				}
				else
				{
					if (tagIntersectionPoint < closestIntersectionPoint)
					{
						closestIntersectionPoint = tagIntersectionPoint;
					}
				}
			}
			else if (tagSpan.Span.Start.Position > span.End.Position)
			{
				break;
			}
		}

		// get first span and tags
		if (closestIntersectionPoint == 0L)
		{
			firstSpan = span;
		}
		else
		{
			firstSpan = span.Slice(0, closestIntersectionPoint);
		}
		tags = GetOverlappingTags(firstSpan);
	}

	private sealed class TagSpanComparer : IComparer<TagSpan>
	{
		public static readonly TagSpanComparer Instance = new();

		public int Compare(TagSpan x, TagSpan y)
		{
			var startCompare = x.Span.Start.Position.CompareTo(y.Span.Start.Position);
			if (startCompare != 0)
			{
				return startCompare;
			}

			return x.Span.End.Position.CompareTo(y.Span.End.Position);
		}
	}
}