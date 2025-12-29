namespace HexEditor.Model;

public interface IBinaryBuffer : IAsyncDisposable
{
	ValueTask CopyToAsync(MemorySpan span, Memory<byte> destination, CancellationToken cancellationToken);

	long Length { get; }

	ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}
