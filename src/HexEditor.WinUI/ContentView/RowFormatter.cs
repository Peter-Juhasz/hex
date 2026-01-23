using HexEditor.Core.Classification;
using HexEditor.Core.Hyperlinks;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using System;

namespace HexEditor.WinUI.ContentView;

internal class RowFormatter
{
	public record struct FormatContext(
		IHexView View,
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

			// create runs
			var memorySpan = context.Data.Slice(i, (int)nextRun.Span.Length);
			hexRuns.Add(new(
				Span: nextRun,
				Data: memorySpan,
				Text: FormattedTextRun.ToHexString(memorySpan.Span),
				LeftPosition: context.Theme.FontWidth * i * 2,
				RenderedWidth: context.Theme.FontWidth * nextRun.Span.Length * 2,
				Tags: tags,
				Style: effectiveStyle
			));
			asciiRuns.Add(new(
				Span: nextRun,
				Data: memorySpan,
				Text: FormattedTextRun.ToAsciiString(memorySpan.Span),
				LeftPosition: context.Theme.FontWidth * i,
				RenderedWidth: context.Theme.FontWidth * nextRun.Span.Length,
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
