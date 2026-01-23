using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.UI;

namespace HexEditor.WinUI.Outlining;

internal sealed class HexOutliningHighlightLayer : Canvas
{
	public HexOutliningHighlightLayer(WinUIHexView view, OutliningMargin outliningMargin, VisualTheme theme) : base()
	{
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = (theme.Columns * 2) * theme.FontWidth;

		_canvas = this;

		_view = view;
		_theme = theme;

		outliningMargin.OutliningRegionSelectionRequested += OnOutliningRegionSelectionRequested;
		outliningMargin.OutliningRegionDismissRequested += OnDismissed;
	}


	private readonly Canvas _canvas;

	private readonly WinUIHexView _view;
	private readonly VisualTheme _theme;
	private readonly Brush _pointerOverBrush = new SolidColorBrush(Color.FromArgb(255, 235, 238, 244));

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
				Fill = _pointerOverBrush,
				IsHitTestVisible = false,
			};
			Canvas.SetZIndex(_regionPath, -1);
			_canvas.Children.Add(_regionPath);
		}

		var points = _view.MapToVisualHex(e.Span.Span);
		var figure = ((PathGeometry)_regionPath.Data).Figures[0];
		figure.Fill(points);
		_regionPath.Visibility = Visibility.Visible;
	}

	private void OnDismissed(object? sender, EventArgs e)
	{
		_regionPath?.Visibility = Visibility.Collapsed;
	}
}
