using Microsoft.Win32.SafeHandles;

namespace HexEditor.Model;

public class SafeFileHandleBinaryBuffer(SafeFileHandle handle) : IBinaryBuffer
{
	private long _length = -1;

	public long Length
	{
		get
		{
			if (_length == -1)
			{
				_length = (int)RandomAccess.GetLength(handle);
			}

			return _length;
		}
	}

	public async ValueTask CopyToAsync(MemoryBinarySpan span, Memory<byte> destination, CancellationToken cancellationToken)
    {
		var read = await RandomAccess.ReadAsync(handle, destination, span.StartOffset, cancellationToken);
		if (read < span.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(span));
		}
	}

	public bool TryRead(Span<byte> buffer, MemoryBinarySpan span)
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
