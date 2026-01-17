using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace HexEditor.WinUI;

internal class BackgroundTaskQueue
{
	public BackgroundTaskQueue(CancellationToken cancellationToken)
	{
		_workerThreadQueue = Channel.CreateUnbounded<Task>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false,
		});
		_workerThread = Task.Run(() => Worker(cancellationToken), cancellationToken);
	}

	private readonly Channel<Task> _workerThreadQueue;
	private readonly Task _workerThread;

	public void Enqueue(Func<CancellationToken, Task> factory)
	{
		var task = factory(CancellationToken.None);
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
			catch (Exception ex)
			{
				await new MessageDialog(ex.Message).ShowAsync();
			}
		}
	}
}
