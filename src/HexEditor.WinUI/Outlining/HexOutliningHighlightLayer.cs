using HexEditor.Core.ViewModel;
using HexEditor.WinUI.Theming;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;

namespace HexEditor.WinUI.Outlining;

internal sealed class HexOutliningHighlightLayer : Canvas
{
	public HexOutliningHighlightLayer(IGraphicalHexView view, OutliningMargin outliningMargin, VisualTheme theme) : base()
	{
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = (view.Columns * 2) * theme.FontWidth;
		this.IsHitTestVisible = false;

		_canvas = this;

		_view = view;
		_theme = theme;

		outliningMargin.OutliningRegionSelectionRequested += OnOutliningRegionSelectionRequested;
		outliningMargin.OutliningRegionDismissRequested += OnDismissed;
	}


	private readonly Canvas _canvas;

	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;

	private Path? _regionPath;

	private void OnOutliningRegionSelectionRequested(object? sender, OutliningRegionSelectionRequestedEventArgs e)
	{
		if (_regionPath == null)
		{
			_regionPath = new Path()
			{
				Data = new PathGeometry()
				{
					Figures = [new PathFigure()
					{
						IsFilled = true,
						IsClosed = true,
					}],
				},
				IsHitTestVisible = false,
			};
			Canvas.SetZIndex(_regionPath, -1);
			_canvas.Children.Add(_regionPath);
		}
		if ((_theme.HexView?.OutliningRegionHighlight ?? _theme.OutliningRegionHighlight) is { } style)
		{
			_regionPath.Apply(style);
		}

		var points = _view.MapToVisualHex(e.Span.Span);
		var figure = ((PathGeometry)_regionPath.Data).Figures[0];
		figure.Fill(points);
		this.Visibility = Visibility.Visible;
	}

	private void OnDismissed(object? sender, EventArgs e)
	{
		this.Visibility = Visibility.Collapsed;
	}
}
