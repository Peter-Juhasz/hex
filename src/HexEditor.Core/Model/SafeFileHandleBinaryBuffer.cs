using Microsoft.Win32.SafeHandles;

namespace HexEditor.Model;

public class SafeFileHandleBinaryBuffer(SafeFileHandle handle) : IBinaryDataSource
{
	public long Length { get; } = RandomAccess.GetLength(handle);

	public async ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
    {
		var read = await RandomAccess.ReadAsync(handle, destination, offset, cancellationToken);
		if (read < destination.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(destination));
		}
	}

	public ValueTask DisposeAsync()
	{
		handle.Dispose();
		return ValueTask.CompletedTask;
	}
}
