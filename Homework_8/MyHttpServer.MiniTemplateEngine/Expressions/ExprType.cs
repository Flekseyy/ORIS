namespace MyHttpServer.MiniTemplateEngine.Expressions;

/// <summary>Виды фрагментов, на которые ExprLexer разбивает выражение.</summary>
public enum ExprType
{
    Identifier,
    Number,
    String,
    Dot,
    LParen,
    RParen,
    LBracket,
    RBracket,
    Not,
    Minus,
    And,
    Or,
    Eq,
    NoEq,
    Lt,
    Gt,
    Le,
    Ge,
    EndOfFrag,
}
