using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace HexEditor.WinUI;

internal sealed class BackgroundTaskQueue
{
	public BackgroundTaskQueue(CancellationToken cancellationToken)
	{
		_cancellationSeries = new CancellationSeries(cancellationToken);
		_workerThreadQueue = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false,
		});
		_workerThread = Task.Run(() => Worker(cancellationToken), cancellationToken);
	}

	private readonly Channel<Task> _workerThreadQueue;
	private readonly Task _workerThread;
	private readonly CancellationSeries _cancellationSeries;

	public void Enqueue(Func<CancellationToken, Task> factory)
	{
		var cancellationToken = _cancellationSeries.GetNext();
		var task = factory(cancellationToken);
		_workerThreadQueue.Writer.TryWrite(task);
	}

	private async Task Worker(CancellationToken cancellationToken)
	{
		await foreach (var task in _workerThreadQueue.Reader.ReadAllAsync(cancellationToken))
		{
			try
			{
				await task;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				// ignore
			}
			catch (Exception ex)
			{
				await new MessageDialog(ex.Message).ShowAsync();
			}
		}
	}
}
