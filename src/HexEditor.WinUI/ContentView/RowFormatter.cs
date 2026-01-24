using HexEditor.Core.Classification;
using HexEditor.Core.Hyperlinks;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using HexEditor.WinUI.Theming;
using System;

namespace HexEditor.WinUI.ContentView;

internal class RowFormatter
{
	public record struct FormatContext(
		IGraphicalHexView View,
		VisualTheme Theme,
		double Top,
		SnapshotSpan Span,
		ReadOnlyMemory<byte> Data,
		TagSpanSplitMap Tags
	);

	public static IHexViewRow Format(FormatContext context)
	{
		using var hexRuns = new PooledArrayBuilder<FormattedTextRun>();
		using var asciiRuns = new PooledArrayBuilder<FormattedTextRun>();

		var hexPrimaryGrouping = context.Theme.HexViewStyle?.PrimaryGrouping ?? 0;
		var hexSecondaryGrouping = context.Theme.HexViewStyle?.SecondaryGrouping ?? 0;
		var asciiPrimaryGrouping = context.Theme.AsciiViewStyle?.PrimaryGrouping ?? 0;
		var asciiSecondaryGrouping = context.Theme.AsciiViewStyle?.SecondaryGrouping ?? 0;

		for (int i = 0; i < context.Span.Span.Length;)
		{
			// determine next split point
			var remainingSpan = context.Span.Slice(i);
			context.Tags.GetClosestSplitPoint(remainingSpan, out var nextRun, out var tags);

			// compute effective style
			var effectiveStyle = WinUITextRunStyle.None;
			foreach (var tagSpan in tags)
			{
				if (tagSpan.Tag is ClassificationTag classificationTag)
				{
					if (context.Theme.ClassificationStyleMap?.TryGetValue(classificationTag.Type, out var style) == true)
					{
						effectiveStyle = WinUITextRunStyle.Merge(effectiveStyle, style);
					}
				}
				else if (tagSpan.Tag is UrlTag)
				{
					if (context.Theme.HyperlinkStyle is not null)
					{
						effectiveStyle = WinUITextRunStyle.Merge(effectiveStyle, context.Theme.HyperlinkStyle);
					}
				}
			}

			// get data
			var dataMemory = context.Data.Slice(i, (int)nextRun.Span.Length);

			// create runs
			var hexStartInCharacters = IHexViewRow.CalculateStartIndexOfHexColumnInCharacters(i, hexPrimaryGrouping, hexSecondaryGrouping);
			var hexEndInCharacters = IHexViewRow.CalculateEndIndexOfHexColumnInCharacters(i + (int)nextRun.Span.Length, hexPrimaryGrouping, hexSecondaryGrouping);
			hexRuns.Add(new(
				Span: nextRun,
				Data: dataMemory,
				Text: FormattedTextRun.ToHexString(dataMemory.Span, i, hexPrimaryGrouping, hexSecondaryGrouping),
				LeftPosition: hexStartInCharacters * context.Theme.FontWidth,
				RenderedWidth: (hexEndInCharacters - hexStartInCharacters) * context.Theme.FontWidth,
				Tags: tags,
				Style: effectiveStyle
			));

			var asciiStartInCharacters = IHexViewRow.CalculateStartIndexOfAsciiColumnInCharacters(i, asciiPrimaryGrouping, asciiSecondaryGrouping);
			var asciiEndInCharacters = IHexViewRow.CalculateEndIndexOfAsciiColumnInCharacters(i + (int)nextRun.Span.Length, asciiPrimaryGrouping, asciiSecondaryGrouping);
			asciiRuns.Add(new(
				Span: nextRun,
				Data: dataMemory,
				Text: FormattedTextRun.ToAsciiString(dataMemory.Span, i, asciiPrimaryGrouping, asciiSecondaryGrouping),
				LeftPosition: asciiStartInCharacters * context.Theme.FontWidth,
				RenderedWidth: (asciiEndInCharacters - asciiStartInCharacters) * context.Theme.FontWidth,
				Tags: tags,
				Style: effectiveStyle
			));
			i += (int)nextRun.Span.Length;
		}

		return new HexViewRow(
			context.View,
			new ViewportBounds(
				Left: 0,
				Top: context.Top,
				Width: context.Theme.FontWidth * context.Span.Span.Length,
				Height: context.Theme.RowHeight
			),
			context.Span,
			context.Data,
			hexRuns.ToImmutableArray(),
			asciiRuns.ToImmutableArray()
		);
	}
}
