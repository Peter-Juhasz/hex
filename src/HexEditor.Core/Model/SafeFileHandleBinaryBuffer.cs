using Microsoft.Win32.SafeHandles;

namespace HexEditor.Model;

public class SafeFileHandleBinaryBuffer(SafeFileHandle handle) : IBinaryBuffer
{
	public long Length { get; } = RandomAccess.GetLength(handle);

	public async ValueTask CopyToAsync(MemorySpan span, Memory<byte> destination, CancellationToken cancellationToken)
    {
		var read = await RandomAccess.ReadAsync(handle, destination, span.StartOffset, cancellationToken);
		if (read < span.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(span));
		}
	}

	public bool TryRead(MemorySpan span, Span<byte> buffer)
	{
		if (!handle.IsAsync)
		{
			var read = RandomAccess.Read(handle, buffer, span.StartOffset);
			return read != -1;
		}

		return false;
	}

	public ValueTask DisposeAsync()
	{
		handle.Dispose();
		return ValueTask.CompletedTask;
	}
}
