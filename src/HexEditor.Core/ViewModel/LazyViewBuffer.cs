using HexEditor.Model;

namespace HexEditor.ViewModel;

public class LazyViewBuffer(IBinaryBuffer dataBuffer) : IViewBuffer
{
	public IBinaryBuffer DataBuffer => dataBuffer;

	private ViewBufferChunk? _loadedChunk;

	public bool TryRead(MemoryBinarySpan span, out ReadOnlyMemory<byte> data)
	{
		var chunk = _loadedChunk;
		if (chunk == null)
		{
			data = null;
			return false;
		}

		if (chunk.Offset <= span.StartOffset && chunk.Offset + chunk.Data.Length >= span.EndOffset)
		{
			data = new ReadOnlyMemory<byte>(chunk.Data, (int)(span.StartOffset - chunk.Offset), span.Length);
			return true;
		}
		
		data = null;
		return false;
	}

	public async Task LoadChunkAsync(MemoryBinarySpan span, CancellationToken cancellationToken)
	{
		if (_loadedChunk != null && _loadedChunk.Offset <= span.StartOffset && _loadedChunk.Offset + _loadedChunk.Data.Length >= span.EndOffset)
		{
			return;
		}

		var buffer = new byte[span.Length];
		await dataBuffer.CopyToAsync(span, buffer, cancellationToken);
		_loadedChunk = new ViewBufferChunk(span.StartOffset, buffer);
	}

	private record class ViewBufferChunk(long Offset, byte[] Data);
}