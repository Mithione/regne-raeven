using Regneraeven.Core.Maths;
using Regneraeven.Core.Tasks;
using Regneraeven.Core.Topics;
using Regneraeven.Web.Data;

namespace Regneraeven.Web.Models;

public sealed record HomeModel(Profile Profile, List<GameTask> Daily, List<SavedProblem> RecentProblems, int XpThisWeek, int Streak);

public sealed record ProfilesModel(List<Profile> Profiles, Profile? Current);

public sealed class ProfileForm
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public int Grade { get; set; } = 5;
}

public sealed record StepsModel(string? Input, string? Error, SolveResult? Similar, IReadOnlyList<Topic>? Topics, int Seed);

public sealed record TagCount(string Name, int Count, TopicInfo? Topic);

public sealed record ProblemListModel(List<SavedProblem> Problems, List<TagCount> Tags, string? Query, string? Tag, string? Status, int Total);

public sealed class ProblemForm
{
    public int? Id { get; set; }
    public string? Text { get; set; }
    public string? Note { get; set; }
    public List<string>? Topics { get; set; }
    public string? OwnTags { get; set; }
}

public sealed record ProblemDetailsModel(SavedProblem Problem, bool IsExpression, SolveResult? Solved, string? SolveError);

public sealed record GameHubModel(Profile Profile, List<GameTask> Daily, int Streak, List<XpEvent> RecentXp, Dictionary<Topic, int> SolvedByTopic);

public sealed record GameTaskModel(GameTask Entity, MathTask Task, int FullXp, List<GameTask>? Daily);
