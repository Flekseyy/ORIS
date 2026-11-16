
# MiniTemplateEngine - внутреннее устройство
*Документ описывает архитектуру и поток выполнения шаблонизатора MiniTemplateEngine.*
# Источники вдохновения
 [The Lox Language](https://craftinginterpreters.com/a-tree-walk-interpreter.html) - Термины и идея шаблонизатора.
 [Scriban](https://github.com/scriban/scriban) - полная практическая реализация этой идеи.
# Поддерживаемый синтаксис

| Конструкция           | Пример                                                     | Описание                                           |
| --------------------- | ---------------------------------------------------------- | -------------------------------------------------- |
| **Переменная**        | `$Name` или `$User.Address.City`                           | Вывод значения с HTML-экранированием по умолчанию. |
| **Без экранирования** | $raw(User.HtmlBio)                                         | Вывод HTML-кода без экранирования.                 |
| **Экранирование `$`** | Цена: 100$$                                                | Выводится как `Цена: 100$`.                        |
| **Условие**           | ```<br>$if (Age > 18) { взрослый } else { ребенок }<br>``` | Поддержка `else/if` и вложенных условий.           |
| **Цикл**              | ```$foreach (var item in Items) { [$item] }<br>```         | Перебор любых `IEnumerable`.                       |


# Быстрый старт
```csharp
var html = TemplateEngine.Render("Привет, $Name!", new { Name = "Мир" });
// "Привет, Мир!"
```
# Архитектура
##  ШАГ 1. Точка входа - TemplateEngine
ТКласс реализует паттерн **Фасад**. Метод `Render(template, model)`:
1. Проверяет `template` и `model` на `null` (иначе `ArgumentNullException`).
2. Обращается к **кэшу AST** (`ConcurrentDictionary<string, TemplateNode>`):
    - Если шаблон уже парсился - берёт готовое дерево.
    - Если нет - вызывает `TemplateParser.Parse(template)` и сохраняет результат в кэш.
3. Вызывает `Renderer.Render(tree, model)`.
4. Любые ошибки оборачивает в `TemplateException`.
## ШАГ 2.  Лексический анализ шаблона - TemplateLexer
 `TemplateLexer.Crushing()` - Разбивает исходный текст на **фрагменты** (`Fragments`), не интерпретируя их смысл.
 **Правила лексера:**

- `$$` -> один символ `$` (экранирование)
- `$` перед не-идентификатором (цифрой, пробелом) -> остаётся текстом
- `else` / `else if` распознаются **сразу после `}`** блока `if`, без знака `$`
- `{}` внутри блока считаются парными (литеральные скобки)
**Ключевые методы лексера:**

- `ReadPlainText()` - читает текст до `$`, `{` или `}`.
- `ReadDollar()` - обрабатывает `$`: если `$$` -> `AppendLiteral("$")`, если идентификатор    -> `ReadConstruct()`.
- `ReadCondition()` - парсит заголовок `$if (...)` и ищет парную `)`.
- `ScanBalanced()` - ищет парную скобку с учётом строк в кавычках.
- `TryReadElse()` - после `}` блока `if` ищет `else` / `else if`.
## ШАГ 3. Синтаксический анализ - TemplateParser
`TemplateParser.Parse()` - Строит **дерево (AST)** из фрагментов лексера.
**Ключевые методы парсера:**

- `ParseTemplate()` -> вызывает `ParseNodes(insideBlock: false)`.
- `ParseNodes()` - цикл по фрагментам:
    - `Text` -> `TextNode`
    - `Expression` -> `ParseOutput()`  -> `OutputNode`
    - `If` -> `ParseIf()` -> `IfNode`
    - `Foreach` -> `ParseForeach()` -> `ForeachNode`
    - `BlockClose` -> конец блока
- `ParseBlock()` -> читает `{`, узлы, `}`.
- `ParseIf()` -> собирает ветки `if` / `else if` и опциональный `else`.
## Шаг 4. Рендеринг - Renderer
`Renderer.Render(tree, model)` - Обходит AST и собирает результат в `StringBuilder`.
**Создаётся контекст:** `RenderContext(model)` - хранит модель и стек переменных циклов.
## Шаг 5. Вычисление выражений - ExprEvaluator 
Лексический анализ выражения - `ExprLexer`
Разбивает текст выражения на фрагменты:
```
"Age > 18" -> [Identifier("Age"), Gt(">"), Number(18), EndOfFrag]
```
**Типы фрагментов (`ExprType`):** `Identifier`, `Number`, `String`, `Dot`, `LParen`, `RParen`, `LBracket`, `RBracket`, `Not`, `Minus`, `And`, `Or`, `Eq`, `NoEq`, `Lt`, `Gt`, `Le`, `Ge`, `EndOfFrag`.
### Синтаксический анализ выражения - `ExprParser`

Строит дерево выражения методом **рекурсивного спуска** с учётом приоритетов:
```
Приоритеты (от низшего к высшему):
  ||  (Or)
  &&  (And)
  == !=  (Equality)
  < > <= >=  (Relational)
  ! -  (Unary)
  . []  (Postfix)
  литералы, имена, скобки  (Primary)
```

**Результат для `"Age > 18"`:**
```
BinaryExpr(Gt,
  Left:  IdentifierExpr("Age"),
  Right: LiteralExpr(18)
)
```

### Вычисление - `ExprEvaluator.Evaluate()`

**Поддерживаемые узлы:**

- `LiteralExpr` -> возвращает значение.
- `IdentifierExpr`  -> ищет в переменных цикла, потом в модели.
- `MemberExpr`  -> вычисляет `Target`, затем читает свойство/поле.
- `IndexExpr`  -> вычисляет `Target` и `Index`, вызывает `GetIndex()`.
- `UnaryExpr`  -> `!` (через `IsTruthy`) или `-` (с явным switch по типам).
- `BinaryExpr`  -> `&&`/`||` (short-circuit), сравнения, равенство.
- `RawExpr`  -> вычисляет вложенное выражение (для отключения HTML-экранирования).

Реализовано через **явный switch** по всем числовым типам (`int`, `long`, `short`, `byte`, `sbyte`, `uint`, `ulong`, `ushort`, `float`, `double`, `decimal`) - без boxing'а.
## Шаг 6. Чтение данных модели - `MemberResolver`

**Кэширование делегатов рефлексии:**
```csharp
private static readonly ConcurrentDictionary<Type, Dictionary<string, Func<object, object?>>> PropertyGetters = new();
```
При первом обращении к типу:

1. Сканирует все публичные свойства (`CanRead`, без индексных параметров) и поля.
2. Создаёт делегат `obj => prop.GetValue(obj)` для каждого.
3. Сохраняет в словарь по имени члена.

При последующих обращениях - просто вызывает делегат из кэша (быстрее, чем каждый раз вызывать `GetProperty` + `GetValue`).

**Поддержка:**

- `IDictionary<string, object?>` - поиск по ключу.
- `IDictionary` - поиск по ключу.
- Свойства и поля через рефлексию.
## Шаг 7. Контекст рендеринга -`RenderContext`
Хранит:

- **Модель** (`Model`) - корневой объект.
- **Стек переменных** (`_variables`) - для циклов `foreach`.

**Методы:**

- `PushVariable(name, value)` - добавляет переменную цикла.
- `PopVariable()` - убирает последнюю (после тела цикла).
- `TryGetVariable(name, ...)` - ищет с конца стека (побеждает самая внутренняя переменная).


# Общая схема
```
TemplateEngine.Render(template, model)
│
├─► TemplateCache.GetOrAdd(template, ...)
│     └─► TemplateParser.Parse(template)
│           └─► TemplateLexer(source).Crushing()
│                 └─► List<Fragments>  [Text, Expression, If, ...]
│           └─► TemplateParser(fragments).ParseTemplate()
│                 └─► TemplateNode (AST)
│
└─► Renderer.Render(tree, model)
      └─► RenderContext(model)
      └─► Обход узлов:
            ├─ TextNode        -> output.Append(text)
            ├─ OutputNode      -> Evaluate()  -> ToText()  -> HtmlEncode()  -> output.Append()
            ├─ IfNode          -> Evaluate(condition)  -> IsTruthy()  -> RenderNodes(body)
            └─ ForeachNode     -> Evaluate(collection)  -> foreach  -> PushVariable/PopVariable
                  │
                  └─► ExprEvaluator.Evaluate(expr, context)
                        ├─ ExprLexer(source).Crushing()  -> List<ExprFragment>
                        ├─ ExprParser(fragments).ParseExpression()  -> Expr (дерево)
                        └─ Вычисление узла:
                              ├─ LiteralExpr    -> value
                              ├─ IdentifierExpr  -> context.TryGetVariable / GetMember
                              ├─ MemberExpr     -> Evaluate(Target)  -> GetMember
                              ├─ IndexExpr      -> Evaluate(Target, Index)  -> GetIndex
                              ├─ UnaryExpr      -> ! / -
                              ├─ BinaryExpr     -> && / || / == / != / < / > / <= / >=
                              └─ RawExpr        -> Evaluate(Inner)  [без HtmlEncode]
                                    │
                                    └─► MemberResolver.GetMember(target, name)
                                          └─ PropertyGetters[type][name](target)
```