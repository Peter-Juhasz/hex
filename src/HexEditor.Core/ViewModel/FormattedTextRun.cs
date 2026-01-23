using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.ViewModel;

public readonly record struct FormattedTextRun(
	SnapshotSpan Span,
	ReadOnlyMemory<byte> Data,
	string Text,
	double LeftPosition,
	double RenderedWidth,
	ImmutableArray<TagSpan> Tags,
	object? Style
)
{
	public static string ToHexString(ReadOnlySpan<byte> data) => string.Create(data.Length * 2, data, (span, bytes) =>
	{
		Convert.TryToHexString(bytes, span, out _);
	});

	public static string ToAsciiString(ReadOnlySpan<byte> data) => string.Create(data.Length, data, (span, bytes) =>
	{
		for (int i = 0; i < bytes.Length; i++)
		{
			var b = bytes[i];
			span[i] = (b >= 32 && b <= 126) ? (char)b : '.';
		}
	});
}
