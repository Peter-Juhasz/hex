using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.Core.Syntax;

public abstract class AbstractSyntaxTreeTagger<TTag>(
	IPartialSyntaxTreeProvider syntaxTreeProvider
)
	: ITagger<TTag> where TTag : ITag
{
	private static readonly Task<ImmutableArray<TagSpan<TTag>>> EmptyResult = Task.FromResult(ImmutableArray<TagSpan<TTag>>.Empty);

	public Task<ImmutableArray<TagSpan<TTag>>> GetTagsAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		var syntaxTreeTask = syntaxTreeProvider.GetSyntaxTreeAsync(span, cancellationToken);
		if (syntaxTreeTask.IsCompletedSuccessfully)
		{
			var syntaxTree = syntaxTreeTask.Result; 
			if (syntaxTree == null)
			{
				return EmptyResult;
			}

			var tags = GetTags(syntaxTree, span, cancellationToken);
			return Task.FromResult(tags);
		}

		return WaitGetTagsAsync(syntaxTreeTask, span, cancellationToken);
	}

	public async Task<ImmutableArray<TagSpan<TTag>>> WaitGetTagsAsync(ValueTask<IPartialSyntaxTree?> task, SnapshotSpan span, CancellationToken cancellationToken)
	{
		var syntaxTree = await task.ConfigureAwait(false);
		if (syntaxTree == null)
		{
			return [];
		}

		return GetTags(syntaxTree, span, cancellationToken);
	}

	protected abstract ImmutableArray<TagSpan<TTag>> GetTags(IPartialSyntaxTree syntaxTree, SnapshotSpan span, CancellationToken cancellationToken);
}
