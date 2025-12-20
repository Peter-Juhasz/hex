using HexEditor.Model;

namespace HexEditor.ViewModel;

public class LazyViewBuffer(IBinaryBuffer dataBuffer) : IViewBuffer
{
	public IBinaryBuffer DataBuffer => dataBuffer;

	private ViewBufferChunk? _loadedChunk;

	public bool TryRead(long offset, int length, out ReadOnlyMemory<byte> data)
	{
		var chunk = _loadedChunk;
		if (chunk == null)
		{
			data = null;
			return false;
		}

		if (chunk.Offset <= offset && chunk.Offset + chunk.Data.Length >= offset + length)
		{
			data = new ReadOnlyMemory<byte>(chunk.Data, (int)(offset - chunk.Offset), length);
			return true;
		}
		
		data = null;
		return false;
	}

	public async Task LoadChunkAsync(long offset, int length, CancellationToken cancellationToken)
	{
		if (_loadedChunk != null && _loadedChunk.Offset <= offset && _loadedChunk.Offset + _loadedChunk.Data.Length >= offset + length)
		{
			return;
		}
		var buffer = new byte[length];
		await dataBuffer.CopyToAsync(buffer, offset, length, cancellationToken);
		_loadedChunk = new ViewBufferChunk(offset, buffer);
	}

	private record class ViewBufferChunk(long Offset, byte[] Data);
}