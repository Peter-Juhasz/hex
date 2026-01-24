using HexEditor.Model;

namespace HexEditor.Core.Syntax;

public class PartialSyntaxTreeProvider(
	IPartialSyntaxTreeFactory factory
) : IPartialSyntaxTreeProvider
{
	public ValueTask<IPartialSyntaxTree?> GetSyntaxTreeAsync(SnapshotSpan span, CancellationToken cancellationToken)
	{
		return factory.GetSyntaxTreeAsync(span, null, cancellationToken);
	}
}