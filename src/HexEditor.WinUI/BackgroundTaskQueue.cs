using HexEditor.Core.Model;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HexEditor.WinUI;

internal sealed class BackgroundTaskQueue
{
	public BackgroundTaskQueue(CancellationToken cancellationToken)
	{
		_cancellationSeries = new CancellationSeries(cancellationToken);
		_workerThreadQueue = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false,
		});
		_workerThread = Task.Run(() => Worker(cancellationToken), cancellationToken);
	}

	private readonly Channel<WorkItem> _workerThreadQueue;
	private readonly Task _workerThread;
	private readonly CancellationSeries _cancellationSeries;

	public void Enqueue(Func<CancellationToken, Task> factory)
	{
		var cancellationToken = _cancellationSeries.GetNext();
		_workerThreadQueue.Writer.TryWrite(new(factory, cancellationToken));
	}

	private async Task Worker(CancellationToken cancellationToken)
	{
		await foreach (var workItem in _workerThreadQueue.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
		{
			if (workItem.CancellationToken.IsCancellationRequested)
			{
				continue;
			}

			try
			{
				await workItem.Factory(workItem.CancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				// ignore
			}
			catch (SnapshotMismatchException)
			{
				// ignore
			}
			catch (Exception ex)
			{
                // TODO
            }
        }
	}

	private record struct WorkItem(Func<CancellationToken, Task> Factory, CancellationToken CancellationToken);
}
