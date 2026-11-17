using MiniHttpServer.ORM;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Controllers;
using MyHttpServer.Framework.Result;

namespace MyHttpServer.Controllers;

[Controller("user")]
public class UserController : BaseController
{
    [Get("list")]
    public async Task<IActionResult> ListGet()
    {
        var orm = new OrmContext("Host=localhost;Port=5432;Database=SemestrBd;Username=postgres;Password=18041804;");
        var users = await orm.GetAllAsync<User>("users");
        return PageResult("testTemplate/test.html", new { Users = users });
    }
    [Get("create")]
    public IActionResult CreateForm()
    {
        return View("testTemplate/create.html");
    }
    [Post("create")]
    public async Task<IActionResult> Create(string name, string email, int age)
    {
        var orm = new OrmContext("Host=localhost;Port=5432;Database=SemestrBd;Username=postgres;Password=18041804;");
        var newUser = new User
        {

            id = Guid.NewGuid(),
            name = name,
            email = email,
            age = age
        };


        var affectedRows = await orm.InsertAsync("users", newUser);

        if (affectedRows > 0)
        {
            return new RedirectResult("/user/list");
        }

        return Json(new { status = "error", message = "Не удалось добавить пользователя" });
    }
}

public class User
{
    public Guid id { get; set; }
    
    public string name { get; set; } = string.Empty;
    
    public string email { get; set; } = string.Empty;
    
    public int age { get; set; }
}