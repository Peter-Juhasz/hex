using HexEditor.Classification;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.ViewModel;

internal partial class ConsoleHexView
{
	private ImmutableArray<FormattedSpan> Format(FormatContext context)
	{
		var span = context.Span;
		if (span.Length == 0)
		{
			return [];
		}

		if (!_viewBuffer.TryRead(span, out var memory))
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

			// style rules
			var style = MatchRule(value, new(Offset: absoluteOffset, Column: relativeOffset), context.Rules);
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

	private static ConsoleStyle? MatchRule(byte value, ValueFormattingRule.Context context, ImmutableArray<ValueFormattingRule> rules)
	{
		if (rules.IsDefaultOrEmpty)
		{
			return null;
		}

		foreach (var rule in rules)
		{
			if (rule.IsMatch(value, context))
			{
				return rule.Style;
			}
		}

		return null;
	}

	private readonly record struct FormatContext(
		MemorySpan Span, 
		ImmutableArray<ValueFormattingRule> Rules,
		ImmutableArray<ClassificationSpan> Classifications
	);
}