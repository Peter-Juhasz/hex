using HexEditor.Model;

namespace HexEditor.Core.Syntax;

public interface IPartialSyntaxTreeProvider
{
	ValueTask<IPartialSyntaxTree?> GetSyntaxTreeAsync(SnapshotSpan span, CancellationToken cancellationToken);
}
