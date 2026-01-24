using HexEditor.Core.ViewModel;

namespace HexEditor.Core.Tests.ViewModel;

[TestClass]
public class HexViewRowTests
{
	[TestMethod]

	[DataRow(0, 0, 0, 0)] // |000000
	[DataRow(2, 1, 0, 0)] // 00|0000
	[DataRow(4, 2, 0, 0)] // 0000|00

	[DataRow(0, 0, 1, 0)] // |00 00 00
	[DataRow(3, 1, 1, 0)] // 00 |00 00
	[DataRow(6, 2, 1, 0)] // 00 00 |00

	[DataRow(0, 0, 2, 0)] // |0000 00
	[DataRow(2, 1, 2, 0)] // 00|00 00
	[DataRow(5, 2, 2, 0)] // 0000 |00
	public void CalculateStartIndexOfHexColumnInCharacters(int expected, int columnIndex, int primaryGrouping, int secondaryGrouping)
	{
		Assert.AreEqual(expected, IHexViewRow.CalculateStartIndexOfHexColumnInCharacters(columnIndex, primaryGrouping, secondaryGrouping));
	}

	[TestMethod]

	[DataRow(2, 0, 0, 0)] // 00|0000
	[DataRow(4, 1, 0, 0)] // 0000|00
	[DataRow(6, 2, 0, 0)] // 000000|

	[DataRow(2, 0, 1, 0)] // 00| 00 00
	[DataRow(5, 1, 1, 0)] // 00 00| 00
	[DataRow(8, 2, 1, 0)] // 00 00 00|

	[DataRow(2, 0, 2, 0)] // 00|00 00
	[DataRow(4, 1, 2, 0)] // 0000| 00
	[DataRow(7, 2, 2, 0)] // 0000 00|
	public void CalculateEndIndexOfHexColumnInCharacters(int expected, int columnIndex, int primaryGrouping, int secondaryGrouping)
	{
		Assert.AreEqual(expected, IHexViewRow.CalculateEndIndexOfHexColumnInCharacters(columnIndex, primaryGrouping, secondaryGrouping));
	}

	[TestMethod]

	[DataRow("012345", new byte[] { 0x01, 0x23, 0x45 }, 0, 0, 0)]
	[DataRow("01 23 45", new byte[] { 0x01, 0x23, 0x45 }, 0, 1, 0)]
	[DataRow("0123 45", new byte[] { 0x01, 0x23, 0x45 }, 0, 2, 0)]

	[DataRow("012345", new byte[] { 0x01, 0x23, 0x45 }, 1, 0, 0)]
	[DataRow("01 23 45", new byte[] { 0x01, 0x23, 0x45 }, 1, 1, 0)]
	[DataRow("01 2345", new byte[] { 0x01, 0x23, 0x45 }, 1, 2, 0)]

	[DataRow("012345", new byte[] { 0x01, 0x23, 0x45 }, 2, 0, 0)]
	[DataRow("01 23 45", new byte[] { 0x01, 0x23, 0x45 }, 2, 1, 0)]
	[DataRow("0123 45", new byte[] { 0x01, 0x23, 0x45 }, 2, 2, 0)]
	public void FillHexString(string expected, byte[] bytes, int startColumnIndex, int primaryGrouping, int secondaryGrouping)
	{
		Assert.AreEqual(expected, FormattedTextRun.ToHexString(bytes, startColumnIndex, primaryGrouping, secondaryGrouping));
	}
}
