using HexEditor.Core.ViewModel;
using System;

namespace HexEditor.WinUI;

public sealed class IndirectViewAccessor(Func<IGraphicalHexView> viewGetter) : IViewAccessor
{
	private IGraphicalHexView? _view;

	public IGraphicalHexView View => _view ??= viewGetter();
}