using HexEditor.Model;
using HexEditor.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.IO;

namespace HexEditor.Windows;

public sealed partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();

		SetTitleBar(titleBar);
		ExtendsContentIntoTitleBar = true;

		this.AppWindow.Resize(new(840, 512));
	}

	private EditorHost? _editor;

	private async void MainGrid_Loaded(object sender, RoutedEventArgs e)
	{
		foreach (var testFile in new string[] 
		{ 
			@"E:\rock2.mid",
		})
		{
			if (File.Exists(testFile))
			{
				var handle = File.OpenHandle(testFile);
				var binaryBuffer = await MemoryBinaryBuffer.CreateAsync(new SafeFileHandleBinaryBuffer(handle), default);
				var snapshot = new BinaryDataSourceSnapshot(binaryBuffer);
				CreateEditor(snapshot);
				break;
			}
		}
	}

	private void CreateEditor(IBinarySnapshot snapshot)
	{
		if (_editor != null)
		{
			MainGrid.Children.Remove(_editor);
		}

		var editorHost = new WinUI.EditorHost(snapshot);
		Grid.SetRow(editorHost, 2);
		MainGrid.Children.Add(editorHost);
		_editor = editorHost;
	}

	private async void OpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
	{
		var ofd = new FileOpenPicker(AppWindow.Id);
		ofd.FileTypeFilter.Add("*");
		var file = await ofd.PickSingleFileAsync().AsTask();
		if (file == null)
		{
			return;
		}

		var handle = File.OpenHandle(file.Path);
		var binaryBuffer = new SafeFileHandleBinaryBuffer(handle);
		var snapshot = new BinaryDataSourceSnapshot(binaryBuffer);
		CreateEditor(snapshot);
	}
}
