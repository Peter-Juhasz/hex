using HexEditor.Model;
using System;

namespace HexEditor.WinUI.Selection;

public class SelectionSpan
{
	public SelectionSpan(SelectionManager manager, SnapshotPoint anchorPoint, SnapshotPoint activePoint)
	{
		if (anchorPoint.Snapshot != activePoint.Snapshot)
		{
			throw new ArgumentException("Anchor point and active point must belong to the same snapshot.");
		}

		this.manager = manager;
		AnchorPoint = anchorPoint;
		ActivePoint = activePoint;
	}

	private readonly SelectionManager manager;

	public SnapshotPoint AnchorPoint { get; }

	public SnapshotPoint ActivePoint { get; }

	public bool IsReversed => ActivePoint < AnchorPoint;

	public SnapshotSpan Span => IsReversed
		? SnapshotSpan.Create(ActivePoint, AnchorPoint)
		: SnapshotSpan.Create(AnchorPoint, ActivePoint);
}
