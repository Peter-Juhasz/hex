using HexEditor.Core.Model;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.ViewModel;

public interface IHexViewRow
{
	IGraphicalHexView View { get; }

	SnapshotSpan Extent { get; }

	ReadOnlySpan<byte> Data { get; }

	ViewportBounds VisualBounds { get; }

	double Baseline { get; }

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

	static int GetNextGroupingColumnIndex(int currentColumnIndex, int primaryGrouping, int secondaryGrouping)
	{
		if (primaryGrouping == 0)
		{
			return 0;
		}

		var nextPrimary = ((currentColumnIndex / primaryGrouping) + 1) * primaryGrouping;
		if (secondaryGrouping == 0)
		{
			return nextPrimary;
		}

		var nextSecondary = ((currentColumnIndex / secondaryGrouping) + 1) * secondaryGrouping;
		return Math.Min(nextPrimary, nextSecondary);
	}

	static int GetStartIndexOfAsciiColumnInCharacters(int columnIndex, int primaryGrouping, int secondaryGrouping)
	{
		if (primaryGrouping == 0)
		{
			return columnIndex;
		}
		int offset = columnIndex / primaryGrouping;
		if (secondaryGrouping != 0)
		{
			offset += columnIndex / secondaryGrouping;
		}
		return columnIndex + offset;
	}

	static int GetEndIndexOfAsciiColumnInCharacters(int columnIndex, int primaryGrouping, int secondaryGrouping) =>
		GetStartIndexOfAsciiColumnInCharacters(columnIndex, primaryGrouping, secondaryGrouping) + 1;

	static int GetStartIndexOfHexColumnInCharacters(int columnIndex, int primaryGrouping, int secondaryGrouping)
	{
		if (primaryGrouping == 0)
		{
			return columnIndex * 2;
		}
		int offset = (columnIndex / primaryGrouping) * 1;
		if (secondaryGrouping != 0)
		{
			offset += (columnIndex / secondaryGrouping) * 1;
		}
		return columnIndex * 2 + offset;
	}

	static int GetEndIndexOfHexColumnInCharacters(int columnIndex, int primaryGrouping, int secondaryGrouping) =>
		GetStartIndexOfHexColumnInCharacters(columnIndex, primaryGrouping, secondaryGrouping) + 2;

	static double GetVisualLeftOfHexColumn(int columnIndex, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		var start = GetStartIndexOfHexColumnInCharacters(columnIndex, primaryGrouping, secondaryGrouping);
		return fontWidth * start;
	}

	static double GetVisualRightOfHexColumn(int columnIndex, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		var start = GetEndIndexOfHexColumnInCharacters(columnIndex, primaryGrouping, secondaryGrouping);
		return fontWidth * start;
	}

	static double GetTotalVisualWidthOfHexRow(int columns, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		var end = GetEndIndexOfHexColumnInCharacters(columns - 1, primaryGrouping, secondaryGrouping);
		return fontWidth * end;
	}

	static double GetVisualLeftOfAsciiColumn(int columnIndex, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		var start = GetStartIndexOfAsciiColumnInCharacters(columnIndex, primaryGrouping, secondaryGrouping);
		return fontWidth * start;
	}

	static double GetVisualRightOfAsciiColumn(int columnIndex, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		var start = GetEndIndexOfAsciiColumnInCharacters(columnIndex, primaryGrouping, secondaryGrouping);
		return fontWidth * start;
	}

	static double GetTotalVisualWidthOfAsciiRow(int columns, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		var end = GetTotalCharactersInAsciiRow(columns - 1, primaryGrouping, secondaryGrouping);
		return fontWidth * end;
	}

	static int GetTotalCharactersInHexRow(int columns, int primaryGrouping, int secondaryGrouping)
	{
		return GetEndIndexOfHexColumnInCharacters(columns - 1, primaryGrouping, secondaryGrouping);
	}

	static int GetTotalCharactersInAsciiRow(int columns, int primaryGrouping, int secondaryGrouping)
	{
		return GetEndIndexOfAsciiColumnInCharacters(columns - 1, primaryGrouping, secondaryGrouping);
	}

	static int GetColumnIndexFromHexPosition(double xCoordinate, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		if (xCoordinate < 0)
		{
			return 0;
		}
		var approximateColumn = (int)(xCoordinate / (fontWidth * 2));
		while (true)
		{
			var startX = GetVisualLeftOfHexColumn(approximateColumn, fontWidth, primaryGrouping, secondaryGrouping);
			var endX = GetVisualLeftOfHexColumn(approximateColumn + 1, fontWidth, primaryGrouping, secondaryGrouping);
			if (xCoordinate >= startX && xCoordinate < endX)
			{
				return approximateColumn;
			}
			else if (xCoordinate < startX)
			{
				approximateColumn--;
			}
			else
			{
				approximateColumn++;
			}
		}
	}

	static int GetColumnIndexFromAsciiPosition(double xCoordinate, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		if (xCoordinate < 0)
		{
			return 0;
		}
		var approximateColumn = (int)(xCoordinate / fontWidth);
		while (true)
		{
			var startX = GetVisualLeftOfAsciiColumn(approximateColumn, fontWidth, primaryGrouping, secondaryGrouping);
			var endX = GetVisualLeftOfAsciiColumn(approximateColumn + 1, fontWidth, primaryGrouping, secondaryGrouping);
			if (xCoordinate >= startX && xCoordinate < endX)
			{
				return approximateColumn;
			}
			else if (xCoordinate < startX)
			{
				approximateColumn--;
			}
			else
			{
				approximateColumn++;
			}
		}
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