using GroupTracker.Models;
using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace GroupTracker.Services;

public sealed class GroupTrackerStorageService(IJSRuntime jsRuntime)
{
    private const string StorageKey = "group-tracker-state";
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented
    };

    private GroupTrackerState? _state;

    public event Action? StateChanged;

    public async Task<GroupTrackerState> GetStateAsync()
    {
        if (_state is not null)
        {
            return _state;
        }

        var rawState = await jsRuntime.InvokeAsync<string?>("groupTrackerStorage.get", StorageKey);

        _state = string.IsNullOrWhiteSpace(rawState)
            ? GroupTrackerState.CreateDefault()
            : JsonConvert.DeserializeObject<GroupTrackerState>(rawState) ?? GroupTrackerState.CreateDefault();

        NormalizeState(_state);
        await PersistAsync(notifyStateChanged: false);
        return _state;
    }

    public async Task UpdateScoreAsync(Guid groupId, int delta)
    {
        var state = await GetStateAsync();
        var group = state.Groups.FirstOrDefault(entry => entry.Id == groupId);

        if (group is null)
        {
            return;
        }

        group.TotalScore += delta;
        await PersistAsync();
    }

    public async Task UpdateGroupAsync(Guid groupId, string name, IReadOnlyCollection<string> playerNames)
    {
        var state = await GetStateAsync();
        var group = state.Groups.FirstOrDefault(entry => entry.Id == groupId);

        if (group is null)
        {
            return;
        }

        group.Name = name;
        group.PlayerNames = playerNames.ToList();
        await PersistAsync();
    }

    public async Task ResetAllScoresAsync()
    {
        var state = await GetStateAsync();

        foreach (var group in state.Groups)
        {
            group.TotalScore = 0;
        }

        await PersistAsync();
    }

    public async Task AddGroupAsync()
    {
        var state = await GetStateAsync();
        var nextGroupNumber = state.Groups.Count + 1;
        var firstPlayerNumber = ((nextGroupNumber - 1) * 2) + 1;

        state.Groups.Add(new GroupEntry
        {
            Name = $"Group {nextGroupNumber}",
            PlayerNames =
            [
                $"Player {firstPlayerNumber}",
                $"Player {firstPlayerNumber + 1}"
            ]
        });

        await PersistAsync();
    }

    public async Task<bool> RemoveGroupAsync(Guid groupId)
    {
        var state = await GetStateAsync();
        if (state.Groups.Count <= 1)
        {
            return false;
        }

        var removed = state.Groups.RemoveAll(group => group.Id == groupId) > 0;
        if (!removed)
        {
            return false;
        }

        NormalizeState(state);
        await PersistAsync();
        return true;
    }

    public async Task<string> ExportStateAsync()
    {
        var state = await GetStateAsync();
        return JsonConvert.SerializeObject(state, SerializerSettings);
    }

    public async Task<bool> ImportStateAsync(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        var importedState = JsonConvert.DeserializeObject<GroupTrackerState>(json);
        if (importedState?.Groups is null)
        {
            return false;
        }

        foreach (var group in importedState.Groups)
        {
            group.Id = group.Id == Guid.Empty ? Guid.NewGuid() : group.Id;
            group.Name = group.Name?.Trim() ?? string.Empty;
            group.PlayerNames = group.PlayerNames?
                .Where(player => !string.IsNullOrWhiteSpace(player))
                .Select(player => player.Trim())
                .ToList() ?? [];
        }

        NormalizeState(importedState);
        _state = importedState;
        await PersistAsync();
        return true;
    }

    private static void NormalizeState(GroupTrackerState state)
    {
        state.Groups ??= [];

        if (state.Groups.Count == 0)
        {
            state.Groups.Add(new GroupEntry
            {
                Name = "Group 1",
                PlayerNames = ["Player 1", "Player 2"]
            });
        }

        foreach (var group in state.Groups)
        {
            group.Id = group.Id == Guid.Empty ? Guid.NewGuid() : group.Id;
            group.Name = string.IsNullOrWhiteSpace(group.Name) ? "Unnamed Group" : group.Name.Trim();
            group.PlayerNames = group.PlayerNames
                .Where(player => !string.IsNullOrWhiteSpace(player))
                .Select(player => player.Trim())
                .ToList();
        }
    }

    private async Task PersistAsync(bool notifyStateChanged = true)
    {
        if (_state is null)
        {
            return;
        }

        var payload = JsonConvert.SerializeObject(_state, SerializerSettings);
        await jsRuntime.InvokeVoidAsync("groupTrackerStorage.set", StorageKey, payload);

        if (notifyStateChanged)
        {
            StateChanged?.Invoke();
        }
    }
}
