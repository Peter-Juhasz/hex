using HexEditor.Core.Model;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.ViewModel;

public interface IHexViewRow
{
	IHexView View { get; }

	SnapshotSpan Extent { get; }

	ReadOnlySpan<byte> Data { get; }

	ViewportBounds VisualBounds { get; }

	ImmutableArray<FormattedTextRun> HexRuns { get; }

	ImmutableArray<FormattedTextRun> AsciiRuns { get; }

	int GetRelativePositionFromHex(double xCoordinate)
	{
		if (xCoordinate < 0)
		{
			return 0;
		}

		var lastRun = HexRuns[^1];
		if (xCoordinate > lastRun.LeftPosition + lastRun.RenderedWidth)
		{
			return Data.Length;
		}

		var bytesBefore = 0;

		foreach (var run in HexRuns)
		{
			if (xCoordinate >= run.LeftPosition && xCoordinate < run.LeftPosition + run.RenderedWidth)
			{
				var runRelativeX = xCoordinate - run.LeftPosition;
				var byteIndexInRun = (int)(runRelativeX / (run.RenderedWidth / run.Data.Length));
				bytesBefore += byteIndexInRun;
				break;
			}

			bytesBefore += run.Data.Length;
		}

		return bytesBefore;
	}

	int GetRelativePositionFromAscii(double xCoordinate)
	{
		if (xCoordinate < 0)
		{
			return 0;
		}

		var lastRun = HexRuns[^1];
		if (xCoordinate > lastRun.LeftPosition + lastRun.RenderedWidth)
		{
			return Data.Length;
		}

		var bytesBefore = 0;

		foreach (var run in AsciiRuns)
		{
			if (xCoordinate >= run.LeftPosition && xCoordinate < run.LeftPosition + run.RenderedWidth)
			{
				var runRelativeX = xCoordinate - run.LeftPosition;
				var byteIndexInRun = (int)(runRelativeX / (run.RenderedWidth / run.Data.Length));
				bytesBefore += byteIndexInRun;
				break;
			}

			bytesBefore += run.Data.Length;
		}

		return bytesBefore;
	}
}

public static partial class Extensions
{
	extension(IHexViewRow row)
	{
		public SnapshotPoint GetPositionFromHexView(double xCoordinate)
		{
			var relativePosition = row.GetRelativePositionFromHex(xCoordinate);
			return row.Extent.Start + relativePosition;
		}

		public SnapshotPoint GetPositionFromAsciiView(double xCoordinate)
		{
			var relativePosition = row.GetRelativePositionFromAscii(xCoordinate);
			return row.Extent.Start + relativePosition;
		}
	}
}