using HexEditor.Model;
using System;

namespace HexEditor.WinUI.Selection;

public interface ISelection
{
	SelectionSpan? Span { get; }

	event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

	void Select(SnapshotPoint anchorPoint, SnapshotPoint activePoint);
	void Select(SnapshotSpan span, bool isReversed = false);
	void Clear();
}