using HexEditor.Model;

namespace HexEditor.ViewModel;

public class FullViewBuffer(IBinaryBuffer dataBuffer) : IViewBuffer
{
	private byte[]? _viewBuffer;

	public IBinaryBuffer DataBuffer => dataBuffer;

	public bool TryRead(MemorySpan span, out ReadOnlyMemory<byte> data) 
	{
		if (_viewBuffer == null)
		{
			data = ReadOnlyMemory<byte>.Empty;
			return false;
		}

		data = new ReadOnlyMemory<byte>(_viewBuffer, (int)span.StartOffset, span.Length);
		return true;
	}

	public Task LoadChunkAsync(MemorySpan span, CancellationToken cancellationToken)
	{
		if (_viewBuffer != null)
		{
			return Task.CompletedTask;
		}

		_viewBuffer = new byte[dataBuffer.Length];
		return dataBuffer.CopyToAsync(span, _viewBuffer, cancellationToken).AsTask();
	}
}
