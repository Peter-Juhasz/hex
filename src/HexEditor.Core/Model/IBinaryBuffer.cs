namespace HexEditor.Model;

public interface IBinaryBuffer : IAsyncDisposable
{
	ValueTask CopyToAsync(MemoryBinarySpan span, Memory<byte> destination, CancellationToken cancellationToken);

	long Length { get; }

	ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}
