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

	static int CalculateNextGroupingColumnIndex(int currentColumnIndex, int primaryGrouping, int secondaryGrouping)
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

	static int CalculateStartIndexOfAsciiColumnInCharacters(int columnIndex, int primaryGrouping, int secondaryGrouping)
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

	static int CalculateEndIndexOfAsciiColumnInCharacters(int columnIndex, int primaryGrouping, int secondaryGrouping)
	{
		if (primaryGrouping == 0)
		{
			return columnIndex + 1;
		}
		int offset = columnIndex / primaryGrouping;
		if (secondaryGrouping != 0)
		{
			offset += columnIndex / secondaryGrouping;
		}
		return columnIndex + 1 + offset;
	}

	static int CalculateStartIndexOfHexColumnInCharacters(int columnIndex, int primaryGrouping, int secondaryGrouping)
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

	static int CalculateEndIndexOfHexColumnInCharacters(int columnIndex, int primaryGrouping, int secondaryGrouping)
	{
		if (primaryGrouping == 0)
		{
			return columnIndex * 2 + 2;
		}
		int offset = (columnIndex / primaryGrouping) * 1;
		if (secondaryGrouping != 0)
		{
			offset += (columnIndex / secondaryGrouping) * 1;
		}
		return columnIndex * 2 + 2 + offset;
	}


	static double CalculateHexPosition(int columns, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		var start = CalculateStartIndexOfHexColumnInCharacters(columns, primaryGrouping, secondaryGrouping);
		return fontWidth * start;
	}

	static double CalculateTotalHexRowWidth(int columns, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		var end = CalculateEndIndexOfHexColumnInCharacters(columns, primaryGrouping, secondaryGrouping);
		return fontWidth * end;
	}

	static double CalculateAsciiPosition(int columns, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		var start = CalculateStartIndexOfAsciiColumnInCharacters(columns, primaryGrouping, secondaryGrouping);
		return fontWidth * start;
	}

	static double CalculateTotalAsciiRowWidth(int columns, double fontWidth, int primaryGrouping, int secondaryGrouping)
	{
		var end = CalculateTotalCharactersInAsciiRow(columns, primaryGrouping, secondaryGrouping);
		return fontWidth * end;
	}

	static int CalculateTotalCharactersInHexRow(int columns, int primaryGrouping, int secondaryGrouping)
	{
		return CalculateEndIndexOfHexColumnInCharacters(columns, primaryGrouping, secondaryGrouping);
	}

	static int CalculateTotalCharactersInAsciiRow(int columns, int primaryGrouping, int secondaryGrouping)
	{
		return CalculateEndIndexOfAsciiColumnInCharacters(columns, primaryGrouping, secondaryGrouping);
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
			var startX = CalculateHexPosition(approximateColumn, fontWidth, primaryGrouping, secondaryGrouping);
			var endX = CalculateHexPosition(approximateColumn + 1, fontWidth, primaryGrouping, secondaryGrouping);
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
			var startX = CalculateAsciiPosition(approximateColumn, fontWidth, primaryGrouping, secondaryGrouping);
			var endX = CalculateAsciiPosition(approximateColumn + 1, fontWidth, primaryGrouping, secondaryGrouping);
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