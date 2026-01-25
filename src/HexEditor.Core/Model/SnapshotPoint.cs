using HexEditor.Model;

namespace HexEditor.Core.Model;

public readonly record struct SnapshotPoint(IBinarySnapshot Snapshot, long Position)
{
	public static bool operator <(SnapshotPoint a, SnapshotPoint b)
	{
		SnapshotMismatchException.ThrowIfMismatch(a.Snapshot, b.Snapshot);

		return a.Position < b.Position;
	}

	public static bool operator >(SnapshotPoint a, SnapshotPoint b)
	{
		SnapshotMismatchException.ThrowIfMismatch(a.Snapshot, b.Snapshot);

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

	public static long operator -(SnapshotPoint left, SnapshotPoint right)
	{
		SnapshotMismatchException.ThrowIfMismatch(left.Snapshot, right.Snapshot);

		var newPosition = left.Position - right.Position;
		if (newPosition < 0 || newPosition > left.Snapshot.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(right), "Resulting position is out of bounds.");
		}
		return newPosition;
	}
}
