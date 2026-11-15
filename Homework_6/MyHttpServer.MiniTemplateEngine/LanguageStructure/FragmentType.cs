namespace MyHttpServer.MiniTemplateEngine.LanguageStructure;

public enum FragmentType
{
    Text,
    Expression,
    If,
    ElseIf,
    Else,
    Foreach,
    BlockOpen,
    BlockClose,
    Error,
    EndOfFragment,
}