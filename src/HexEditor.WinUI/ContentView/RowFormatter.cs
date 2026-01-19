using HexEditor.Model;
using HexEditor.ViewModel;
using System;

namespace HexEditor.WinUI.ContentView;

internal class RowFormatter
{
	public record struct FormatContext(
		IHexView View,
		VisualTheme Theme,
		double Top,
		SnapshotSpan Span,
		ReadOnlyMemory<byte> Data
	);

	public static IHexViewRow Format(FormatContext context)
	{
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
			[new(
				context.Span,
				context.Data,
				FormattedTextRun.ToHexString(context.Data.Span),
				0,
				context.Span.Span.Length * 2 * context.Theme.FontWidth,
				null
			)],
			[new(
				context.Span,
				context.Data,
				FormattedTextRun.ToAsciiString(context.Data.Span),
				0,
				context.Span.Span.Length * context.Theme.FontWidth,
				null
			)]
		);
	}
}
