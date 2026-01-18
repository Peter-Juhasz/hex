using HexEditor.Core.Model;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace HexEditor.Core.Tagging;

[RequiresUnreferencedCode("Uses reflection to create tagger instances.")]
public class ReflectionTaggerProvider(
	IEnumerable<Assembly> assemblies
)
	: ITaggerProvider
{
	public IEnumerable<ITagger<TTag>> CreateTaggers<TTag>(string contentType) where TTag : ITag
	{
		var taggerType = typeof(ITagger<TTag>);
		foreach (var assembly in assemblies)
		{
			var applicableTypes = assembly.GetExportedTypes()
				.Where(t => 
					!t.IsAbstract && !t.IsInterface && 
					taggerType.IsAssignableFrom(t) &&
					t.GetCustomAttribute<ContentTypeAttribute>()?.Type == contentType
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
					yield return tagger;
				}
			}
		}
	}
}