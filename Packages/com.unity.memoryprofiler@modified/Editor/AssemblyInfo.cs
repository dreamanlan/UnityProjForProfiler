using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.MemoryProfilerExtension.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.MemoryProfilerExtension.Editor.PerformanceTests")]
[assembly: InternalsVisibleTo("Unity.MemoryProfilerExtension.TestProject.Editor.Tests")]
// Moq uses DynamicProxyGenAssembly2 to generate proxies, which therefore requires access to internals to generate proxies of public classes.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
