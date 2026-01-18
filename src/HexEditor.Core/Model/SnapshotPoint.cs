namespace HexEditor.Model;

public readonly record struct SnapshotPoint(IBinarySnapshot Snapshot, long Position)
{
	public static bool operator <(SnapshotPoint a, SnapshotPoint b)
	{
		if (a.Snapshot != b.Snapshot)
		{
			throw new ArgumentException("SnapshotPoints must belong to the same snapshot.");
		}

		return a.Position < b.Position;
	}

	public static bool operator >(SnapshotPoint a, SnapshotPoint b)
	{
		if (a.Snapshot != b.Snapshot)
		{
			throw new ArgumentException("SnapshotPoints must belong to the same snapshot.");
		}

		return a.Position > b.Position;
	}
}
