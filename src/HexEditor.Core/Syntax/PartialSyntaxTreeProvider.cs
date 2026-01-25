using HexEditor.Model;

namespace HexEditor.Core.Syntax;

public class PartialSyntaxTreeProvider(
	IPartialSyntaxTreeFactory factory
) : IPartialSyntaxTreeProvider
{
	private CacheItem? _lastCacheItem = null;
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public ValueTask<IPartialSyntaxTree?> GetSyntaxTreeAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		// check cache first
		IPartialSyntaxTree? oldSyntaxTree = null;
		if (_lastCacheItem is { } cacheItem && cacheItem.Snapshot == span.Snapshot)
		{
			oldSyntaxTree = cacheItem.SyntaxTree;

			if (oldSyntaxTree.CoveredSpan.Contains(span))
			{
				return new(oldSyntaxTree);
			}
		}

		return GetCoreAsync(span, oldSyntaxTree, cancellationToken);
	}

	private async ValueTask<IPartialSyntaxTree?> GetCoreAsync(SnapshotSpan span, IPartialSyntaxTree? oldSyntaxTree, CancellationToken cancellationToken)
	{
		await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

		// re-check cache after acquiring the semaphore
		if (_lastCacheItem is { } cacheItem && cacheItem.Snapshot == span.Snapshot)
		{
			oldSyntaxTree = cacheItem.SyntaxTree;

			if (oldSyntaxTree.CoveredSpan.Contains(span))
			{
				_semaphore.Release();
				return oldSyntaxTree;
			}
		}

		// create new syntax tree
		try
		{
			var newSyntaxTree = await factory.GetSyntaxTreeAsync(span, oldSyntaxTree, cancellationToken).ConfigureAwait(false);
			if (newSyntaxTree is null)
			{
				return null;
			}

			_lastCacheItem = new CacheItem(span.Snapshot, newSyntaxTree);
			return newSyntaxTree;
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private record class CacheItem(IBinarySnapshot Snapshot, IPartialSyntaxTree SyntaxTree);
}