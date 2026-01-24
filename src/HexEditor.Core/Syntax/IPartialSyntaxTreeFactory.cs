using HexEditor.Model;

namespace HexEditor.Core.Syntax;

public interface IPartialSyntaxTreeFactory
{
	ValueTask<IPartialSyntaxTree?> GetSyntaxTreeAsync(SnapshotSpan span, IPartialSyntaxTree? before, CancellationToken cancellationToken);
}
