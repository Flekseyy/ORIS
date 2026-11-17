namespace MyHttpServer.MiniTemplateEngine.LanguageStructure;

public readonly record struct Fragments(FragmentType FragmentType, ReadOnlyMemory<char> Value, SourceSpan SourceSpan)
{
    public override string ToString() => $"{FragmentType}({Value}) - {SourceSpan.Start}";
}