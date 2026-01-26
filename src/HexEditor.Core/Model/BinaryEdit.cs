using System.Collections.Immutable;

namespace HexEditor.Model;

public readonly record struct BinaryEdit(ImmutableArray<BinaryChange> Changes);
