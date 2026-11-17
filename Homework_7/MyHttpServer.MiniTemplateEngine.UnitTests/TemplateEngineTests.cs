namespace MyHttpServer.MiniTemplateEngine.UnitTests;

[TestClass]
public class TemplateEngineTests
{
    //  БАЗОВЫЙ ВЫВОД ПЕРЕМЕННЫХ
    [TestMethod]
    public void Render_SimpleVariable_ShouldReplaceWithValue()
    {
        var result = TemplateEngine.Render("Привет, $Name!", new { Name = "Алексей" });
        Assert.AreEqual("Привет, Алексей!", result);
    }

    [TestMethod]
    public void Render_NestedProperties_ShouldResolve()
    {
        var model = new { User = new { Address = new { City = "Москва" } } };
        var result = TemplateEngine.Render("Город: $User.Address.City", model);
        Assert.AreEqual("Город: Москва", result);
    }

    [TestMethod]
    public void Render_MultipleVariables_ShouldReplaceAll()
    {
        var model = new { First = "Иван", Last = "Петров" };
        var result = TemplateEngine.Render("$First $Last", model);
        Assert.AreEqual("Иван Петров", result);
    }

    [TestMethod]
    public void Render_NullValue_ShouldRenderEmpty()
    {
        var result = TemplateEngine.Render("[$Value]", new { Value = (string?)null });
        Assert.AreEqual("[]", result);
    }

    [TestMethod]
    public void Render_NumericTypes_ShouldFormatCorrectly()
    {
        var model = new { Int = 42, Decimal = 3.14m, Long = 1000000L };
        Assert.AreEqual("42", TemplateEngine.Render("$Int", model));
        Assert.AreEqual("3.14", TemplateEngine.Render("$Decimal", model));
        Assert.AreEqual("1000000", TemplateEngine.Render("$Long", model));
    }

    [TestMethod]
    public void Render_BooleanValues_ShouldRenderAsText()
    {
        var model = new { Flag = true };
        Assert.AreEqual("True", TemplateEngine.Render("$Flag", model));
    }
    
    //  HTML-ЭКРАНИРОВАНИЕ
    [TestMethod]
    public void Render_HtmlEncoding_ShouldPreventXss()
    {
        var model = new { Input = "<script>alert('xss')</script>" };
        var result = TemplateEngine.Render("Данные: $Input", model);
        Assert.AreEqual("Данные: &lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", result);
    }

    [TestMethod]
    public void Render_HtmlEncoding_ShouldEscapeAmpersandAndQuotes()
    {
        var model = new { Text = "A & B \"quoted\"" };
        var result = TemplateEngine.Render("$Text", model);
        Assert.AreEqual("A &amp; B &quot;quoted&quot;", result);
    }

    [TestMethod]
    public void Render_RawExpression_ShouldNotEncodeHtml()
    {
        var model = new { Html = "<b>Жирный</b>" };
        var result = TemplateEngine.Render("Контент: $raw(Html)", model);
        Assert.AreEqual("Контент: <b>Жирный</b>", result);
    }

    [TestMethod]
    public void Render_RawWithNestedProperty_ShouldWork()
    {
        var model = new { User = new { Bio = "<div>Профиль</div>" } };
        var result = TemplateEngine.Render("$raw(User.Bio)", model);
        Assert.AreEqual("<div>Профиль</div>", result);
    }

    [TestMethod]
    public void Render_RawWithNull_ShouldRenderEmpty()
    {
        var model = new { Value = (string?)null };
        var result = TemplateEngine.Render("[$raw(Value)]", model);
        Assert.AreEqual("[]", result);
    }
    //  УСЛОВИЯ (if / else if / else)
    [TestMethod]
    public void Render_IfCondition_TrueBranch()
    {
        var result = TemplateEngine.Render("$if (IsAdmin) { Админ } else { Гость }",
            new { IsAdmin = true });
        Assert.AreEqual(" Админ ", result);
    }

    [TestMethod]
    public void Render_IfCondition_FalseBranch()
    {
        var result = TemplateEngine.Render("$if (IsAdmin) { Админ } else { Гость }",
            new { IsAdmin = false });
        Assert.AreEqual(" Гость ", result);
    }

    [TestMethod]
    public void Render_IfWithoutElse_ShouldRenderNothing_WhenFalse()
    {
        var result = TemplateEngine.Render("A$if (Show) { B }C", new { Show = false });
        Assert.AreEqual("AC", result);
    }

    [TestMethod]
    public void Render_ElseIfChain_ShouldPickCorrectBranch()
    {
        var template = "$if (Score > 90) { A } else if (Score > 70) { B } else if (Score > 50) { C } else { D }";
        Assert.AreEqual(" A ", TemplateEngine.Render(template, new { Score = 95 }));
        Assert.AreEqual(" B ", TemplateEngine.Render(template, new { Score = 80 }));
        Assert.AreEqual(" C ", TemplateEngine.Render(template, new { Score = 60 }));
        Assert.AreEqual(" D ", TemplateEngine.Render(template, new { Score = 30 }));
    }

    [TestMethod]
    public void Render_IfWithComparison_ShouldWork()
    {
        var result = TemplateEngine.Render("$if (Count > 5) { Много } else { Мало }",
            new { Count = 3 });
        Assert.AreEqual(" Мало ", result);
    }

    [TestMethod]
    public void Render_IfWithStringComparison_ShouldWork()
    {
        var result = TemplateEngine.Render("$if (Name == \"admin\") { Да } else { Нет }",
            new { Name = "admin" });
        Assert.AreEqual(" Да ", result);
    }

    [TestMethod]
    public void Render_IfWithLogicalAnd_ShouldWork()
    {
        var result = TemplateEngine.Render("$if (A && B) { Оба } else { Нет }",
            new { A = true, B = true });
        Assert.AreEqual(" Оба ", result);
    }

    [TestMethod]
    public void Render_IfWithLogicalOr_ShouldWork()
    {
        var result = TemplateEngine.Render("$if (A || B) { Один } else { Нет }",
            new { A = false, B = true });
        Assert.AreEqual(" Один ", result);
    }

    [TestMethod]
    public void Render_IfWithNegation_ShouldWork()
    {
        var result = TemplateEngine.Render("$if (!Hidden) { Видимо } else { Скрыто }",
            new { Hidden = false });
        Assert.AreEqual(" Видимо ", result);
    }

    [TestMethod]
    public void Render_NestedIf_ShouldWork()
    {
        var template = "$if (A) {$if (B) {AB} else {A}} else {нет A}";
        Assert.AreEqual("AB", TemplateEngine.Render(template, new { A = true, B = true }));
        Assert.AreEqual("A", TemplateEngine.Render(template, new { A = true, B = false }));
        Assert.AreEqual("нет A", TemplateEngine.Render(template, new { A = false, B = true }));
    }
    //  ЦИКЛЫ (foreach)
    [TestMethod]
    public void Render_ForeachLoop_ShouldIterateCollection()
    {
        var model = new { Items = new List<string> { "A", "B", "C" } };
        var result = TemplateEngine.Render("$foreach (var item in Items) { [$item] }", model);
        Assert.AreEqual(" [A]  [B]  [C] ", result);
    }

    [TestMethod]
    public void Render_ForeachWithNestedProperties_ShouldWork()
    {
        var model = new
        {
            Users = new List<object>
            {
                new { Name = "Иван" },
                new { Name = "Мария" }
            }
        };
        var result = TemplateEngine.Render("<ul>$foreach (var user in Users) { <li>$user.Name</li> }</ul>", model);
        Assert.AreEqual("<ul> <li>Иван</li>  <li>Мария</li> </ul>", result);
    }

    [TestMethod]
    public void Render_ForeachWithArray_ShouldWork()
    {
        var model = new { Nums = new[] { 1, 2, 3 } };
        var result = TemplateEngine.Render("$foreach (var n in Nums) { $n }", model);
        Assert.AreEqual(" 1  2  3 ", result);
    }

    [TestMethod]
    public void Render_ForeachWithEmptyCollection_ShouldRenderNothing()
    {
        var model = new { Items = new List<string>() };
        var result = TemplateEngine.Render("[$foreach (var x in Items) { $x }]", model);
        Assert.AreEqual("[]", result);
    }

    [TestMethod]
    public void Render_NestedForeach_ShouldWork()
    {
        var model = new
        {
            Rows = new[]
            {
                new { Cols = new[] { "A", "B" } },
                new { Cols = new[] { "C", "D" } }
            }
        };
        var result = TemplateEngine.Render(
            "$foreach (var row in Rows) {$foreach (var col in row.Cols) {$col }}",
            model);
        Assert.AreEqual("A B C D ", result);
    }
    [TestMethod]
    public void Render_ForeachWithConditionInside_ShouldWork()
    {
        var model = new { Items = new[] { 1, 2, 3, 4, 5 } };
        var result = TemplateEngine.Render(
            "$foreach (var n in Items) {$if (n > 3) {[$n] }}",
            model);
        Assert.AreEqual("[4] [5] ", result);
    }
    
    //  ИНДЕКСАЦИЯ И СЛОВАРИ

    [TestMethod]
    public void Render_ArrayIndexing_ShouldWork()
    {
        var model = new { Items = new[] { "первый", "второй", "третий" } };
        var result = TemplateEngine.Render("$Items[1]", model);
        Assert.AreEqual("второй", result);
    }

    //  ЭКРАНИРОВАНИЕ И СПЕЦСИМВОЛЫ


    [TestMethod]
    public void Render_DollarEscaping_ShouldOutputSingleDollar()
    {
        var result = TemplateEngine.Render("Цена: $100", new { });
        Assert.AreEqual("Цена: $100", result);
    }

    [TestMethod]
    public void Render_DollarBeforeNonIdent_ShouldStayAsText()
    {
        var result = TemplateEngine.Render("5$ 10$", new { });
        Assert.AreEqual("5$ 10$", result);
    }

    [TestMethod]
    public void Render_LiteralBracesInBlock_ShouldOutput()
    {
        var result = TemplateEngine.Render("$if (true) { JSON: {\"a\": 1} }", new { });
        Assert.AreEqual(" JSON: {\"a\": 1} ", result);
    }
    
    //  СЛОЖНЫЕ КОМБИНИРОВАННЫЕ ШАБЛОНЫ
    
    [TestMethod]
    public void Render_ComplexTemplate_ShouldWork()
    {
        var model = new
        {
            Title = "Список",
            Users = new[]
            {
                new { Name = "Алексей", Age = 25, IsActive = true },
                new { Name = "Мария", Age = 17, IsActive = false }
            }
        };
        var template =
            "<h1>$Title</h1>" +
            "<ul>$foreach (var u in Users) {" +
            "  <li>$u.Name ($u.Age лет)$if (u.IsActive) { + }</li>" +
            "}</ul>";
        var result = TemplateEngine.Render(template, model);
        Assert.IsTrue(result.Contains("<h1>Список</h1>"));
        Assert.IsTrue(result.Contains("Алексей (25 лет)"));
        Assert.IsTrue(result.Contains("+"));
        Assert.IsTrue(result.Contains("Мария (17 лет)"));
        Assert.IsFalse(result.Contains("Мария (17 лет) +"));
    }
    //  ОШИБКИ
    
    [TestMethod]
    [ExpectedException(typeof(TemplateException))]
    public void Render_InvalidSyntax_ShouldThrow()
    {
        TemplateEngine.Render("$if (true) { забыли закрыть", new { });
    }

    [TestMethod]
    [ExpectedException(typeof(TemplateException))]
    public void Render_UndefinedVariable_ShouldThrow()
    {
        TemplateEngine.Render("$NonExistent", new { });
    }

    [TestMethod]
    [ExpectedException(typeof(TemplateException))]
    public void Render_NullCollectionInForeach_ShouldThrow()
    {
        TemplateEngine.Render("$foreach (var x in Items) { $x }", new { Items = (object?)null });
    }

    [TestMethod]
    [ExpectedException(typeof(TemplateException))]
    public void Render_NonIterableInForeach_ShouldThrow()
    {
        TemplateEngine.Render("$foreach (var x in Value) { $x }", new { Value = 42 });
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Render_NullTemplate_ShouldThrow()
    {
        TemplateEngine.Render(null!, new { });
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Render_NullModel_ShouldThrow()
    {
        TemplateEngine.Render("test", null!);
    }
}