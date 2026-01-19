using HexEditor.Model;
using System;

namespace HexEditor.WinUI.Selection;

public class SelectionManager : ISelection
{
	public SelectionManager(WinUIHexView view)
	{
		_view = view;
	}

	private readonly WinUIHexView _view;
	private SelectionSpan? _selection;

	public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

	public SelectionSpan? Span => _selection;

	public void Select(SnapshotPoint anchorPoint, SnapshotPoint activePoint)
	{
		if (_selection is SelectionSpan existing)
		{
			if (existing.AnchorPoint == anchorPoint && existing.ActivePoint == activePoint)
			{
				return;
			}
		}

		_selection = new SelectionSpan(this, anchorPoint, activePoint);
		SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selection));
	}

	public void Select(SnapshotSpan span, bool isReversed = false)
	{
		_selection = isReversed ? new SelectionSpan(this, span.End, span.Start) : new SelectionSpan(this, span.Start, span.End);
		SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selection));
	}

	public void Clear()
	{
		var oldSelection = _selection;

		_selection = null;
		SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(null));
	}
}
