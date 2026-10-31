// 12
// Вариант № 12. Рейтинг компьютерных игр
// Разработайте модуль публикации оценок компьютерных игр.
// Маршруты:
// 1.	GET /reviews — вывести оценки с фильтрацией по минимальной оценке minScore.
// 2.	GET /reviews/details?id=1 — получить подробную информацию об оценке.
// 3.	POST /reviews/create — создать запись с полями game и score.
// 4.	PUT /reviews/update?id=1 — изменить название игры, оценку и статус публикации.
//     Условия:
//     	Название игры — от 2 до 80 символов.
//     	Оценка — целое число от 1 до 5.
//     	Статус — draft или published; при создании — draft.
//     	minScore — целое число от 1 до 5.
//     Дополнительное задание: рассчитать среднюю оценку записей, попавших в результат фильтрации, с точностью до одного знака после запятой.

using System.Net;
using System.Text;
using System.Web;

namespace SelfGuidedWork;

public static class Program
{
    public static void Main()
    {
        var server = new HttpListener();
        server.Prefixes.Add("http://localhost:8080/");
        server.Start();
        Review review = new Review();
        Review.Create("Игра грабителей",5);
        Review.Create("Игра копателей",4);
        Review.Create("Игра моряков",5);
        Review.Create("Игра воинов",1);
        Review.Create("Смутная игра",1);
        
        Console.WriteLine("Сервер запущен на http://localhost:8080/");
        while (true)
        {
            var context = server.GetContext();
            HandleRequest(context);
        }
    }

    static void HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath;
        var query = HttpUtility.ParseQueryString(request.Url.Query);

        try
        {
            switch (path)
            {
                case "/reviews" when request.HttpMethod == "GET":
                {
                    int? minScore = null;
                    if (query["minScore"] != null)
                    {
                        if (!int.TryParse(query["minScore"], out int ms))
                        {
                            SendError(response, 400, "minScore должен быть целым числом");
                            return;
                        }
                        var (valid, err) = Validation.ValidateMinScore(ms);
                        if (!valid) { SendError(response, 400, err); return; }
                        minScore = ms;
                    }

                    var reviews = Review.GetAll(minScore);
                    var reviewsList = ""; 
                    foreach (var review in reviews)
                    {
                        reviewsList +=($"{review}<br>");
                    }
                    SendHtml(response, $"<html><body> <h1> {reviewsList} </h1> </body></html>");
                    return;
                }
                case "/reviews/details" when request.HttpMethod == "GET":
                {
                    if (query["id"] == null || !int.TryParse(query["id"], out int id))
                    {
                        SendError(response, 400, "Параметр id не указан или некорректен");
                        return;
                    }
                    var review = Review.GetById(id);
                    SendHtml(response, $"<html><body> <h1> Оценки {review.ToString()} </h1> </body></html>");
                    return;
                }
                case "/reviews/create" when request.HttpMethod == "POST":
                {
                    string body;
                    
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                        body = reader.ReadToEnd();

                    var formQuerry = HttpUtility.ParseQueryString(body);
                    
                    var name = formQuerry["game"];
                    int? score = null;
                    
                    if (formQuerry["score"] != null && int.TryParse(formQuerry["score"], out int s))
                        score = s;

                    var (gameValid, gameErr) = Validation.ValidateGame(name);
                    if (!gameValid) { SendError(response, 400, gameErr); return; }

                    var (scoreValid, scoreErr) = Validation.ValidateScore(score);
                    if (!scoreValid) { SendError(response, 400, scoreErr); return; }

                    Review.Create(name, score.Value);
                    response.StatusCode = 303;
                    response.Close();
                    return;
                }
                case "/reviews/update" when (request.HttpMethod == "PUT" || request.HttpMethod == "POST"):
                {
                    if (query["id"] == null || !int.TryParse(query["id"], out int id))
                    {
                        SendError(response, 400, "Параметр id не указан или некорректен");
                        return;
                    }

                    string body;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                        body = reader.ReadToEnd();

                    var formData = HttpUtility.ParseQueryString(body);
                    var game = formData["game"];
                    int? score = null;
                    if (formData["score"] != null && int.TryParse(formData["score"], out int s))
                        score = s;
                    var status = formData["status"];

                    var (gameValid, gameErr) = Validation.ValidateGame(game);
                    if (!gameValid) { SendError(response, 400, gameErr); return; }

                    var (scoreValid, scoreErr) = Validation.ValidateScore(score);
                    if (!scoreValid) { SendError(response, 400, scoreErr); return; }

                    var (statusValid, statusErr) = Validation.ValidateStatus(status);
                    if (!statusValid) { SendError(response, 400, statusErr); return; }

                    var existing = Review.GetById(id);
                    if (existing == null)
                    {
                        SendError(response, 404, "Запись не найдена");
                        return;
                    }

                    Review.Update(id, game, score.Value, status);
                    response.StatusCode = 204;
                    response.Close();
                    return;
                }
                default:
                    SendError(response, 404, "Маршрут не найден");
                    break;
            }
        }
        catch (Exception ex)
        {
            SendError(response, 500, ex.Message);
        }
    }

    static void SendHtml(HttpListenerResponse response, string html)
    {
        response.ContentType = "text/html; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(html);
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.Close();
    }

    static void SendError(HttpListenerResponse response, int code, string message)
    {
        response.StatusCode = code;
        response.ContentType = "text/html; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes($"<html><body> <h1>{code} - ERROR - {message} </h1> </body></html>");
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.Close();
    }
}