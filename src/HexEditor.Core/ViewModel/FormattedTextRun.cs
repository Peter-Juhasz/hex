using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.ViewModel;

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
			span[i] = ToAsciiByte(b);
		}
	});

	public static char ToAsciiByte(byte b) => (b >= 32 && b <= 126) ? (char)b : '.';

	public static char ToHexLowHalf(byte b) => (char)((b & 0x0F) < 10 ? '0' + (b & 0x0F) : 'A' + ((b & 0x0F) - 10));

	public static char ToHexHighHalf(byte b) => (char)((b >> 4) < 10 ? '0' + (b >> 4) : 'A' + ((b >> 4) - 10));


	public static string ToHexString(ReadOnlySpan<byte> data, int startColumnIndex, int primaryGrouping, int secondaryGrouping)
	{
		var startOffset = IHexViewRow.CalculateEndIndexOfHexColumnInCharacters(data.Length - 1 + startColumnIndex, primaryGrouping, secondaryGrouping);
		var endOffset = IHexViewRow.CalculateStartIndexOfHexColumnInCharacters(startColumnIndex, primaryGrouping, secondaryGrouping);
		var stringLength = startOffset - endOffset;
		return string.Create<ReadOnlySpan<byte>>(stringLength, data, (span, state) =>
		{
			int spanIndex = 0;
			for (int i = 0; i < state.Length; i++)
			{
				if (i > 0)
				{
					var previousGroupingIndex = IHexViewRow.CalculateNextGroupingColumnIndex(i - 1 + startColumnIndex, primaryGrouping, secondaryGrouping);
					var currentGroupingIndex = IHexViewRow.CalculateNextGroupingColumnIndex(i + startColumnIndex, primaryGrouping, secondaryGrouping);
					if (currentGroupingIndex != previousGroupingIndex)
					{
						span[spanIndex++] = ' ';
					}
				}
				var byteValue = state[i];
				span[spanIndex++] = FormattedTextRun.ToHexHighHalf(byteValue);
				span[spanIndex++] = FormattedTextRun.ToHexLowHalf(byteValue);
			}
		});
	}

	public static string ToAsciiString(ReadOnlySpan<byte> data, int startColumnIndex, int primaryGrouping, int secondaryGrouping)
	{
		var startOffset = IHexViewRow.CalculateEndIndexOfAsciiColumnInCharacters(data.Length - 1 + startColumnIndex, primaryGrouping, secondaryGrouping);
		var endOffset = IHexViewRow.CalculateStartIndexOfAsciiColumnInCharacters(startColumnIndex, primaryGrouping, secondaryGrouping);
		var stringLength = startOffset - endOffset;
		return string.Create<ReadOnlySpan<byte>>(stringLength, data, (span, state) =>
		{
			int spanIndex = 0;
			for (int i = 0; i < state.Length; i++)
			{
				if (i > 0)
				{
					var previousGroupingIndex = IHexViewRow.CalculateNextGroupingColumnIndex(i - 1 + startColumnIndex, primaryGrouping, secondaryGrouping);
					var currentGroupingIndex = IHexViewRow.CalculateNextGroupingColumnIndex(i + startColumnIndex, primaryGrouping, secondaryGrouping);
					if (currentGroupingIndex != previousGroupingIndex)
					{
						span[spanIndex++] = ' ';
					}
				}
				var byteValue = state[i];
				span[spanIndex++] = FormattedTextRun.ToAsciiByte(byteValue);
			}
		});
	}
}
