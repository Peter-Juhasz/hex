using HexEditor.Composition;
using HexEditor.Core.ContentType;
using HexEditor.Core.Diagnostics;
using HexEditor.Core.Model;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Formats;
using HexEditor.Model;
using HexEditor.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Immutable;
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
				var binaryBuffer = new FullMemoryCachingBinaryBuffer(new SafeFileHandleBinaryDataSource(handle));
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

		var services = new ServiceCollection();
		services.AddHexEditor();
		services.AddContent(typeof(UrlTagger).Assembly);
		services.AddSingleton<IViewAccessor>(new IndirectViewAccessor(() => _editor!.View));
		var serviceProvider = services.BuildServiceProvider();

		// determine content type
		var contentTypeRegistry = serviceProvider.GetRequiredService<IContentTypeRegistry>();
		var contentType = await contentTypeRegistry.MatchAsync(filePath, snapshot, default);
		if (contentType != null)
		{
			var newSource = new BinaryDataSourceWithContentType(snapshot.Source, contentType);
			snapshot = new BinaryDataSourceSnapshot(newSource);
		}
		var interestedContentTypes = contentTypeRegistry.GetBaseTypesAndSelf(contentType).Select(t => t.Type).ToImmutableArray();

		// create tag aggregators
		var taggerProvider = serviceProvider.GetRequiredService<ITaggerProvider>();
		var diagnosticTagAggregator = new LockingTagAggregator<DiagnosticTag>(
			new FullCachingTagAggregator<DiagnosticTag>(
				new ParallelTagAggregator<DiagnosticTag>(
					taggerProvider.CreateTaggers<DiagnosticTag>(interestedContentTypes)
				)
			)
		);

		// create editor
		var editorHost = new WinUI.EditorHost(serviceProvider, snapshot, taggerProvider, contentTypeRegistry);
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
