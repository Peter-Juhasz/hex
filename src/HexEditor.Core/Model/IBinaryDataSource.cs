using HexEditor.Core.ContentType;

namespace HexEditor.Core.Model;

public interface IBinaryDataSource : IAsyncDisposable
{
	ContentTypeDefinition? ContentType => null;

	ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken);

	long Length { get; }

	ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}
