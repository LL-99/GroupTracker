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

        _state = importedState;
        await PersistAsync();
        return true;
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
