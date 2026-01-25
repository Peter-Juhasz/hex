using HexEditor.Model;

namespace HexEditor.Core.Model;

public class SnapshotMismatchException(IBinarySnapshot expected, IBinarySnapshot actual) : Exception
{
	public IBinarySnapshot Expected { get; } = expected;

	public IBinarySnapshot Actual { get; } = actual;

	public static void ThrowIfMismatch(IBinarySnapshot expected, IBinarySnapshot actual)
	{
		if (expected != actual)
		{
			throw new SnapshotMismatchException(expected, actual);
		}
	}
}
