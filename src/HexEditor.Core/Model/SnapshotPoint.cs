using HexEditor.Model;

namespace HexEditor.Core.Model;

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

	public static SnapshotPoint operator +(SnapshotPoint point, long offset)
	{
		var newPosition = point.Position + offset;
		if (newPosition < 0 || newPosition > point.Snapshot.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(offset), "Resulting position is out of bounds.");
		}

		return new SnapshotPoint(point.Snapshot, newPosition);
	}

	public static SnapshotPoint operator -(SnapshotPoint point, long offset)
	{
		var newPosition = point.Position - offset;
		if (newPosition < 0 || newPosition > point.Snapshot.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(offset), "Resulting position is out of bounds.");
		}
		return new SnapshotPoint(point.Snapshot, newPosition);
	}
}
