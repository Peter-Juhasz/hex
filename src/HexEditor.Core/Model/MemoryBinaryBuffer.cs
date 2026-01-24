namespace HexEditor.Core.Model;

public class MemoryBinaryBuffer(ReadOnlyMemory<byte> buffer) : IBinaryDataSource
{
	public long Length { get; } = buffer.Length;

	public ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
    {
		ArgumentOutOfRangeException.ThrowIfNegative(offset);
		cancellationToken.ThrowIfCancellationRequested();

		if (offset + destination.Length > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(offset));

		buffer.Slice((int)offset, destination.Length).CopyTo(destination);

		return ValueTask.CompletedTask;
	}
}
