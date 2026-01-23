using HexEditor.Core.ContentType;
using HexEditor.Formats.Text;
using HexEditor.Model;
using HexEditor.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
				var binaryBuffer = await MemoryBinaryBuffer.CreateAsync(new SafeFileHandleBinaryDataSource(handle), default);
				var snapshot = new BinaryDataSourceSnapshot(binaryBuffer);
				await CreateEditorAsync(testFile, snapshot);
				break;
			}
		}
	}

	private async Task CreateEditorAsync(string filePath, IBinarySnapshot snapshot)
	{
		if (_editor != null)
		{
			MainGrid.Children.Remove(_editor);
		}

		// determine content type
		var contentTypeDefinitionType = typeof(ContentTypeDefinition);
		var contentTypeDefinitions = typeof(TextContentTypeDefinition).Assembly
			.GetExportedTypes()
			.Where(t => !t.IsAbstract && contentTypeDefinitionType.IsAssignableFrom(t));
		var contentType = "binary";
		foreach (var contentTypeDefinition in contentTypeDefinitions)
		{
			try
			{
				var definition = (ContentTypeDefinition)Activator.CreateInstance(contentTypeDefinition)!;
				if (await definition.MatchesAsync(filePath, snapshot, default))
				{
					contentType = definition.Type;
					break;
				}
			}
			catch (Exception ex) 
			{ 
				// TODO: log
			}
		}

		// create editor
		var editorHost = new WinUI.EditorHost(snapshot, contentType);
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
		var binaryBuffer = new SafeFileHandleBinaryDataSource(handle);
		var snapshot = new BinaryDataSourceSnapshot(binaryBuffer);
		await CreateEditorAsync(file.Path, snapshot);
	}
}
