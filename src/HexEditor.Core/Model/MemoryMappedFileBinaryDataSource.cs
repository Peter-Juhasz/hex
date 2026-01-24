using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace HexEditor.Core.Model;

// more efficient implementation is blocked on https://github.com/dotnet/runtime/issues/122815

public class MemoryMappedFileBinaryDataSource(MemoryMappedFile file, int length) : IBinaryDataSource
{
	private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create();

	public ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(offset);
		cancellationToken.ThrowIfCancellationRequested();

		using var _accessor = file.CreateViewAccessor(offset, destination.Length, MemoryMappedFileAccess.Read);
		var buffer = _arrayPool.Rent(destination.Length);
		try
		{
			_accessor.ReadArray(0, buffer, 0, destination.Length);
			buffer.CopyTo(destination.Span);
		}
		finally
		{
			_arrayPool.Return(buffer);
		}
		return ValueTask.CompletedTask;
	}

	public long Length { get; } = length;

	public ValueTask DisposeAsync()
	{
		file.Dispose();
		return ValueTask.CompletedTask;
	}
}
