using HexEditor.Model;
using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace HexEditor.Core.Model;

// more efficient implementation is blocked on https://github.com/dotnet/runtime/issues/122815

public class MemoryMappedFileBinaryBuffer(MemoryMappedFile file, int length) : IBinaryBuffer
{
	private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create();

	public ValueTask CopyToAsync(MemorySpan span, Memory<byte> destination, CancellationToken cancellationToken)
	{
		using var _accessor = file.CreateViewAccessor(span.StartOffset, span.Length, MemoryMappedFileAccess.Read);
		var buffer = _arrayPool.Rent(span.Length);
		try
		{
			_accessor.ReadArray(span.StartOffset, buffer, 0, span.Length);
			buffer.AsSpan(0, span.Length).CopyTo(destination.Span);
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
