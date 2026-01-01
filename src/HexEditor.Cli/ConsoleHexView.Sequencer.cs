using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.ViewModel;

internal partial class ConsoleHexView
{
	private ImmutableArray<FormattedSpan> Format(MemorySpan span)
	{
		if (!_viewBuffer.TryRead(span, out var memory))
		{
			return [];
		}

		if (memory.Length == 0)
		{
			return [];
		}

		using var builder = new PooledArrayBuilder<FormattedSpan>();

		ConsoleStyle? lastStyle = null;
		long lastOffset = span.StartOffset;

		for (var relativeOffset = 0; relativeOffset < memory.Length; relativeOffset++)
		{
			var absoluteOffset = span.StartOffset + relativeOffset;
			var value = memory.Span[relativeOffset];
			var style = MatchRule(value, new(Offset: absoluteOffset, Column: relativeOffset));
			if (style != lastStyle)
			{
				var length = absoluteOffset - lastOffset;
				var formattedSpan = new FormattedSpan(memory.Slice((int)(lastOffset - span.StartOffset), (int)length), lastStyle);
				builder.Add(formattedSpan);

				lastStyle = style;
				lastOffset = absoluteOffset;
			}
		}

		if (lastOffset != span.Length)
		{
			var length = span.EndOffset - lastOffset;
			var formattedSpan = new FormattedSpan(memory.Slice((int)(lastOffset - span.StartOffset), (int)length), lastStyle);
			builder.Add(formattedSpan);
		}

		return builder.ToImmutableArray();
	}
}