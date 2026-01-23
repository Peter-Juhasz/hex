using HexEditor.Core.Classification;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.ViewModel;

internal partial class ConsoleHexView
{
	private ImmutableArray<FormattedTextRun> Format(FormatContext context)
	{
		var span = context.Span.Span;
		if (span.Length == 0)
		{
			return [];
		}

		var memory = context.Data;

		using var builder = new PooledArrayBuilder<FormattedTextRun>();

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
				var data = memory.Slice((int)(lastOffset - span.StartOffset), (int)length);
				var formattedSpan = new FormattedTextRun(
					Span: context.Span.Slice(lastOffset - span.StartOffset, length),
					Data: data,
					Text: ToHexString(data.Span),
					LeftPosition: relativeOffset,
					RenderedWidth: (double)length,
					Tags: [],
					Style: lastStyle
				);
				builder.Add(formattedSpan);

				lastStyle = style;
				lastOffset = absoluteOffset;
			}
		}

		if (lastOffset < span.EndOffset)
		{
			var length = span.EndOffset - lastOffset;
			var data = memory.Slice((int)(lastOffset - span.StartOffset), (int)length);
			var formattedSpan = new FormattedTextRun(
				Span: context.Span.Slice(lastOffset - span.StartOffset, length),
				Data: data, 
				Text: ToHexString(data.Span),
				LeftPosition: lastOffset,
				RenderedWidth: (double)length,
				Tags: [],
				Style: lastStyle
			);
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

	private static string ToHexString(ReadOnlySpan<byte> data) => string.Create(data.Length * 2, data, (span, data) =>
	{
		Convert.TryToHexString(data, span, out var _);
	});

	private readonly record struct FormatContext(
		SnapshotSpan Span,
		ReadOnlyMemory<byte> Data,
		ImmutableArray<ValueFormattingRule> Rules,
		ImmutableArray<TagSpan<ClassificationTag>> Classifications
	);
}