using HexEditor.Model;

namespace HexEditor.ViewModel;

public class FullViewBuffer(IBinaryBuffer dataBuffer) : IViewBuffer
{
	private byte[]? _viewBuffer;

	public IBinaryBuffer DataBuffer => dataBuffer;

	public bool TryRead(long offset, int length, out ReadOnlyMemory<byte> data)
	{
		if (_viewBuffer == null)
		{
			data = ReadOnlyMemory<byte>.Empty;
			return false;
		}

		data = new ReadOnlyMemory<byte>(_viewBuffer, (int)offset, length);
		return true;
	}

	public Task LoadChunkAsync(long offset, int length, CancellationToken cancellationToken)
	{
		if (_viewBuffer != null)
		{
			return Task.CompletedTask;
		}

		_viewBuffer = new byte[dataBuffer.Length];
		return dataBuffer.CopyToAsync(_viewBuffer, 0, dataBuffer.Length, cancellationToken).AsTask();
	}
}
