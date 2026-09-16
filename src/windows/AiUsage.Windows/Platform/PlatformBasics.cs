using System.ComponentModel;
using AiUsage.Features.Presentation;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.ApplicationModel.Resources;

namespace AiUsage.Platform;

internal sealed class UiDispatcher(DispatcherQueue queue) : IUiDispatcher
{
    public bool HasThreadAccess => queue.HasThreadAccess;

    public void Post(Action action)
    {
        if (!queue.TryEnqueue(() => action()))
            throw new InvalidOperationException("The UI dispatcher is shutting down.");
    }
}

/// <summary>English strings from Strings/en-US/Resources.resw through MRT; a missing key renders the key so it is visible in review.</summary>
internal sealed class ResourceText : ITextResources
{
    private readonly ResourceLoader loader = new();
    private readonly Dictionary<string, string> cache = new(StringComparer.Ordinal);

    public string Get(string key)
    {
        if (cache.TryGetValue(key, out var value))
            return value;
        value = loader.GetString(key);
        if (string.IsNullOrEmpty(value))
            value = key;
        cache[key] = value;
        return value;
    }
}

/// <summary>Typed page navigation with a back stack. The main window hosts the frame and follows <see cref="Navigated"/>.</summary>
internal sealed class NavigationService : INavigationService
{
    private readonly Stack<NavigationRequest> back = new();
    private NavigationRequest current = new(PageKey.Overview);

    public PageKey Current => current.Page;
    public bool CanGoBack => back.Count > 0;
    public event EventHandler<NavigationRequest>? Navigated;

    public void Navigate(NavigationRequest request)
    {
        if (request == current && request.AccountId is null && request.Tab is null)
        {
            Navigated?.Invoke(this, request);
            return;
        }
        back.Push(current);
        if (back.Count > 50)
        {
            var kept = back.Take(50).Reverse().ToArray();
            back.Clear();
            foreach (var item in kept)
                back.Push(item);
        }
        current = request;
        Navigated?.Invoke(this, request);
    }

    public void GoBack()
    {
        if (back.Count == 0)
            return;
        current = back.Pop();
        Navigated?.Invoke(this, current);
    }
}

/// <summary>Layout facts shared by views: compact width (&lt; 720 effective px) and row density.</summary>
internal sealed class AppLayout : INotifyPropertyChanged
{
    public static AppLayout Current { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsCompact
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            PropertyChanged?.Invoke(this, new(nameof(IsCompact)));
        }
    }

    public bool CompactDensity
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            PropertyChanged?.Invoke(this, new(nameof(CompactDensity)));
        }
    }

    public bool ProviderHues
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            PropertyChanged?.Invoke(this, new(nameof(ProviderHues)));
        }
    }
}
