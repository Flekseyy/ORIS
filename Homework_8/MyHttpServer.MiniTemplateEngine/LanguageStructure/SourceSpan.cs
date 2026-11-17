namespace MyHttpServer.MiniTemplateEngine.LanguageStructure;

public readonly record struct SourceSpan(SourceLocation Start, int Length)
{
    public int End => Start.Offset + Length;
}