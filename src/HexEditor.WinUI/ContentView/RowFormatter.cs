using HexEditor.Core.Classification;
using HexEditor.Core.Hyperlinks;
using HexEditor.Core.Tagging;
using HexEditor.Core.Unnecessary;
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
		TagIntersectionMap Tags
	);

	public static IHexViewRow Format(FormatContext context)
	{
		using var hexRuns = new PooledArrayBuilder<FormattedTextRun>();
		using var asciiRuns = new PooledArrayBuilder<FormattedTextRun>();

		var hexPrimaryGrouping = context.Theme.HexView?.PrimaryGrouping ?? 0;
		var hexSecondaryGrouping = context.Theme.HexView?.SecondaryGrouping ?? 0;
		var asciiPrimaryGrouping = context.Theme.AsciiView?.PrimaryGrouping ?? 0;
		var asciiSecondaryGrouping = context.Theme.AsciiView?.SecondaryGrouping ?? 0;

		for (int i = 0; i < context.Span.Span.Length;)
		{
			// determine next split point
			var remainingSpan = context.Span.Slice(i);
			context.Tags.GetClosestSplitPoint(remainingSpan, out var nextRun, out var tags);

			// compute effective style
			var effectiveStyle = TextRunStyle.None;
			foreach (var tagSpan in tags)
			{
				switch (tagSpan.Tag)
				{
					case ClassificationTag classificationTag:
						{
							if (context.Theme.ClassificationMap?.TryGetValue(classificationTag.Type, out var style) == true)
							{
								effectiveStyle = TextRunStyle.Merge(effectiveStyle, style);
							}

							break;
						}

					case UrlTag:
						if (context.Theme.Hyperlink is not null)
						{
							effectiveStyle = TextRunStyle.Merge(effectiveStyle, context.Theme.Hyperlink);
						}
						break;

					case UnnecessaryTag:
						if (context.Theme.Unnecessary is not null)
						{
							effectiveStyle = TextRunStyle.Merge(effectiveStyle, context.Theme.Unnecessary);
						}
						break;
				}
			}

			// get data
			var dataMemory = context.Data.Slice(i, (int)nextRun.Span.Length);

			// create runs
			var hexStartInCharacters = IHexViewRow.GetStartIndexOfHexColumnInCharacters(i, hexPrimaryGrouping, hexSecondaryGrouping);
			var hexEndInCharacters = IHexViewRow.GetEndIndexOfHexColumnInCharacters(i + (int)nextRun.Span.Length, hexPrimaryGrouping, hexSecondaryGrouping);
			hexRuns.Add(new(
				Span: nextRun,
				Data: dataMemory,
				Text: FormattedTextRun.ToHexString(dataMemory.Span, i, hexPrimaryGrouping, hexSecondaryGrouping),
				LeftPosition: hexStartInCharacters * context.Theme.FontWidth,
				RenderedWidth: (hexEndInCharacters - hexStartInCharacters) * context.Theme.FontWidth,
				Tags: tags,
				Style: effectiveStyle
			));

			var asciiStartInCharacters = IHexViewRow.GetStartIndexOfAsciiColumnInCharacters(i, asciiPrimaryGrouping, asciiSecondaryGrouping);
			var asciiEndInCharacters = IHexViewRow.GetEndIndexOfAsciiColumnInCharacters(i + (int)nextRun.Span.Length, asciiPrimaryGrouping, asciiSecondaryGrouping);
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
