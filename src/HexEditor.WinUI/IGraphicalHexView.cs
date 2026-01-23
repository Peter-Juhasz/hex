using HexEditor.ViewModel;
using HexEditor.WinUI.Caret;
using HexEditor.WinUI.Scrolling;
using HexEditor.WinUI.Selection;
using System;
using System.Collections.Immutable;

namespace HexEditor.WinUI;

public interface IGraphicalHexView
{
	ImmutableArray<IHexViewRow> VisibleRows { get; }

	ISelection Selection { get; }

	ICaret Caret { get; }

	IViewport Viewport { get; }

	double ScrollableHeight { get; }

	event EventHandler<VisibleRowsChangedEventArgs>? VisibleRowsChanged;

	event EventHandler<HeightChangedEventArgs>? ScrollableHeightChanged;
}
