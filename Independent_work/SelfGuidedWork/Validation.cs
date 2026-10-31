namespace SelfGuidedWork;

public static class Validation
{
    public static (bool isValid, string error) ValidateGame(string game)
    {
        if (string.IsNullOrWhiteSpace(game))
            return (false, "Название игры не может быть пустым");
        if (game.Length < 2 || game.Length > 80)
            return (false, "Название игры должно быть от 2 до 80 символов");
        return (true, null);
    }

    public static (bool isValid, string error) ValidateScore(int? score)
    {
        if (!score.HasValue)
            return (false, "Оценка не указана");
        if (score.Value < 1 || score.Value > 5)
            return (false, "Оценка должна быть от 1 до 5");
        return (true, null);
    }

    public static (bool isValid, string error) ValidateMinScore(int? minScore)
    {
        if (!minScore.HasValue)
            return (true, null); // необязательный параметр
        if (minScore.Value < 1 || minScore.Value > 5)
            return (false, "minScore должен быть от 1 до 5");
        return (true, null);
    }

    public static (bool isValid, string error) ValidateStatus(string status)
    {
        if (status != "draft" && status != "published")
            return (false, "Статус должен быть draft или published");
        return (true, null);
    }
}