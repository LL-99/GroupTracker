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
                    Name = "Group A",
                    PlayerNames = ["Player 1", "Player 2"],
                    TotalScore = 0
                },
                new GroupEntry
                {
                    Name = "Group B",
                    PlayerNames = ["Player 3", "Player 4"],
                    TotalScore = 0
                },
                new GroupEntry
                {
                    Name = "Group C",
                    PlayerNames = ["Player 5", "Player 6"],
                    TotalScore = 0
                },
                new GroupEntry
                {
                    Name = "Group D",
                    PlayerNames = ["Player 7", "Player 8"],
                    TotalScore = 0
                }
            ]
        };
}
