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

        await PersistAsync();
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

    private async Task PersistAsync()
    {
        if (_state is null)
        {
            return;
        }

        var payload = JsonConvert.SerializeObject(_state, SerializerSettings);
        await jsRuntime.InvokeVoidAsync("groupTrackerStorage.set", StorageKey, payload);
    }
}
