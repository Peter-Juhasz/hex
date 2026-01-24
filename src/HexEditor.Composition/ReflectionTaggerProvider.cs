using HexEditor.Core.Tagging;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace HexEditor.Composition;

[RequiresUnreferencedCode("Uses reflection to create tagger instances.")]
public class ReflectionTaggerProvider(
	IEnumerable<Assembly> assemblies
)
	: ITaggerProvider
{
	public ImmutableArray<ITagger<TTag>> CreateTaggers<TTag>(ImmutableArray<string> contentTypes) where TTag : ITag
	{
		var builder = ImmutableArray.CreateBuilder<ITagger<TTag>>();
		var taggerType = typeof(ITagger<TTag>);
		foreach (var assembly in assemblies)
		{
			var applicableTypes = assembly.GetExportedTypes()
				.Where(t => 
					!t.IsAbstract && !t.IsInterface && 
					taggerType.IsAssignableFrom(t) &&
					t.GetCustomAttribute<ContentTypeAttribute>() is ContentTypeAttribute attr && 
					contentTypes.Contains(attr.Type)
				);
			foreach (var type in applicableTypes)
			{
				ITagger<TTag>? tagger = null;
				try
				{
					tagger = (ITagger<TTag>)Activator.CreateInstance(type)!;
				} 
				catch (Exception) 
				{
					// TODO: log
				}

				if (tagger != null)
				{
					builder.Add(tagger);
				}
			}
		}
		return builder.ToImmutable();
	}
}