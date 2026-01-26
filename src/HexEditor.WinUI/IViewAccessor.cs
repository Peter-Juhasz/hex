using HexEditor.Core.ViewModel;
using System;

namespace HexEditor.WinUI;

public interface IViewAccessor
{
	IGraphicalHexView View { get; }
}

public class IndirectViewAccessor(Func<IGraphicalHexView> viewGetter) : IViewAccessor
{
	private IGraphicalHexView? _view;

	public IGraphicalHexView View => _view ??= viewGetter();
}