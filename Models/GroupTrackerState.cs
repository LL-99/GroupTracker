namespace GroupTracker.Models;

public sealed class GroupTrackerState
{
    public List<GroupEntry> Groups { get; set; } = [];

    public static GroupTrackerState CreateDefault() =>
        new()
        {
            Groups =
            [
                new GroupEntry
                {
                    Name = "Crimson Comets",
                    PlayerNames = ["Ava", "Noah", "Mila", "Leon"],
                    TotalScore = 42
                },
                new GroupEntry
                {
                    Name = "Blue Phoenix",
                    PlayerNames = ["Emma", "Luca", "Sofia", "Finn"],
                    TotalScore = 39
                },
                new GroupEntry
                {
                    Name = "Emerald Wolves",
                    PlayerNames = ["Mia", "Ben", "Ella", "Jonas"],
                    TotalScore = 34
                },
                new GroupEntry
                {
                    Name = "Amber Arrows",
                    PlayerNames = ["Lina", "Theo", "Nora", "Paul"],
                    TotalScore = 27
                }
            ]
        };
}
