namespace GroupTracker.Models;

public sealed class GroupEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public List<string> PlayerNames { get; set; } = [];

    public int TotalScore { get; set; }
}
