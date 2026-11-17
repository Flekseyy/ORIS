namespace MyHttpServer.MiniTemplateEngine.LanguageStructure;

public readonly record struct SourceLocation(int Offset, int Line, int Column)
{
    public override string ToString() => $"{Line}:{Column}";
}


