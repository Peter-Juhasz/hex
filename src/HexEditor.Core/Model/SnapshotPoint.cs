using HexEditor.Model;

namespace HexEditor.Core.Model;

public readonly struct SnapshotPoint : IEquatable<SnapshotPoint>
{
	public SnapshotPoint(IBinarySnapshot snapshot, long position)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(position);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(position, snapshot.Length);

		Snapshot = snapshot;
		Position = position;
	}

	public IBinarySnapshot Snapshot { get; }
	public long Position { get; }


	public bool Equals(SnapshotPoint other) => this == other;

	public override bool Equals(object? obj) => obj is SnapshotPoint other && this == other;

	public override int GetHashCode() => HashCode.Combine(Snapshot, Position);

	public static bool operator ==(SnapshotPoint left, SnapshotPoint right) =>
		left.Snapshot == right.Snapshot && left.Position == right.Position;

	public static bool operator !=(SnapshotPoint left, SnapshotPoint right) =>
		!(left == right);


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

	public static bool operator <=(SnapshotPoint a, SnapshotPoint b)
	{
		SnapshotMismatchException.ThrowIfMismatch(a.Snapshot, b.Snapshot);

		return a.Position <= b.Position;
	}

	public static bool operator >=(SnapshotPoint a, SnapshotPoint b)
	{
		SnapshotMismatchException.ThrowIfMismatch(a.Snapshot, b.Snapshot);

		return a.Position >= b.Position;
	}

	public static SnapshotPoint operator +(SnapshotPoint point, long offset) => new(point.Snapshot, point.Position + offset);

	public static SnapshotPoint operator -(SnapshotPoint point, long offset) => new(point.Snapshot, point.Position - offset);

	public static long operator -(SnapshotPoint left, SnapshotPoint right)
	{
		SnapshotMismatchException.ThrowIfMismatch(left.Snapshot, right.Snapshot);

		var newPosition = left.Position - right.Position;
		ArgumentOutOfRangeException.ThrowIfNegative(newPosition, nameof(right));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(newPosition, left.Snapshot.Length, nameof(right));
		return newPosition;
	}
}

public static partial class Extensions
{
	extension(SnapshotPoint point)
	{
		public SnapshotPoint Offset(long offset) => new(point.Snapshot, point.Position + offset);
	}
}