using HexEditor.Core.Tagging;

namespace HexEditor.Core.Diagnostics;

public record class DiagnosticTag(DiagnosticDescriptor Descriptor) : ITag;
