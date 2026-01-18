using HexEditor.Model;
using System;

namespace HexEditor.WinUI.Selection;

public class SelectionManager
{
	public SelectionManager(WinUIHexView view)
	{
		_view = view;
	}

	private readonly WinUIHexView _view;
	private BinarySelectionSpan? _selection;

	public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

	public void Select(SnapshotPoint anchorPoint, SnapshotPoint activePoint)
	{
		if (_selection is BinarySelectionSpan existing)
		{
			if (existing.AnchorPoint == anchorPoint && existing.ActivePoint == activePoint)
			{
				return;
			}
		}

		_selection = new BinarySelectionSpan(this, anchorPoint, activePoint);
		SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selection));
	}

	public void Select(SnapshotSpan span, bool isReversed = false)
	{
		_selection = isReversed ? new BinarySelectionSpan(this, span.End, span.Start) : new BinarySelectionSpan(this, span.Start, span.End);
		SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selection));
	}

	public void Clear()
	{
		_selection = null;
		SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(null));
	}
}
