using System.Diagnostics;

namespace HexEditor.Core.Model;

public class FullMemoryCachingBinaryBuffer(IBinaryDataSource inner) : IBinaryDataSource
{
	public long Length => _buffer.Length;

	private bool _hasLoaded = false;
	private readonly byte[] _buffer = new byte[inner.Length];
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public ValueTask CopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(offset);
		cancellationToken.ThrowIfCancellationRequested();

		if (_hasLoaded)
		{
			return InnerCopyToAsync(offset, destination, cancellationToken);
		}

		return LoadAndCopyToAsync(offset, destination, cancellationToken);
	}

	private ValueTask InnerCopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
	{
		Debug.Assert(offset >= 0);
		Debug.Assert(_buffer is not null);

		_buffer.AsMemory((int)offset, destination.Length).CopyTo(destination);
		return ValueTask.CompletedTask;
	}

	private async ValueTask LoadAndCopyToAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
	{
		Debug.Assert(offset >= 0);

		await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

		try
		{
			if (!_hasLoaded)
			{
				await inner.CopyToAsync(0, _buffer, cancellationToken).ConfigureAwait(false);
				_hasLoaded = true;
			}
		}
		finally
		{
			_semaphore.Release();
		}

		_buffer.AsMemory((int)offset, destination.Length).CopyTo(destination);
	}

	public async ValueTask DisposeAsync()
	{
		await inner.DisposeAsync();
		_semaphore.Dispose();
	}
}
