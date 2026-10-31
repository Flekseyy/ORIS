using System.Collections.Concurrent;

namespace SelfGuidedWork;

public class Review
{
    private static readonly ConcurrentDictionary<int, Review> _reviews = new();
    private static int _nextId = 1;
    public string? Name { get; set; }
    public int Grade { get; set; }
    public string Status { get; set; }
    public int Id { get; set; }
    public override  string ToString() => $"{Id} - Название игры: {Name} - Оценка: {Grade} \n";
    
    public static Review Create(string game, int grade)
    {
        var review = new Review
        {
            Id = _nextId++,
            Name = game,
            Grade = grade,
            Status = "Draft"
        };
        _reviews[review.Id] = review;
        return review;
    }
    public static List<Review> GetAll(int? minScore = null)
    {
        var query = _reviews.Values.AsEnumerable();
        if (minScore.HasValue)
            query = query.Where(r => r.Grade >= minScore.Value);
        return query.OrderBy(r => r.Id).ToList();}

    public static Review GetById(int id)
    {
        _reviews.TryGetValue(id, out var review);
        return review;
    }
    public static bool Update(int id, string game, int score, string status)
    {
        if (!_reviews.TryGetValue(id, out var review))
            return false;

        review.Name = game;
        review.Grade = score;
        review.Status = status;
        return true;
    }
}
