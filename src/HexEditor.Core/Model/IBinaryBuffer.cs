namespace HexEditor.Model;

public interface IBinaryBuffer : IAsyncDisposable
{
	bool TryRead(Span<byte> buffer, long offset, int length);

	ValueTask CopyToAsync(Memory<byte> destination, long offset, int length, CancellationToken cancellationToken);

	long Length { get; }

	ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}
