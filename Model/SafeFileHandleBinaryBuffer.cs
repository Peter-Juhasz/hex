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

	public async ValueTask CopyToAsync(Memory<byte> destination, long offset, long length, CancellationToken cancellationToken)
	{
		var read = await RandomAccess.ReadAsync(handle, destination, offset, cancellationToken);
		if (read < length)
		{
			throw new ArgumentOutOfRangeException(nameof(offset));
		}
	}

	public bool TryRead(Span<byte> buffer, long offset, int length)
	{
		if (!handle.IsAsync)
		{
			var read = RandomAccess.Read(handle, buffer, offset);
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
