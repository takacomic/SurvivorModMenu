using System.Linq;

namespace SurvivorModMenu.ModMenu;

/// <summary>
/// Registry used by mods to expose settings pages and sections inside SurvivorModMenu.
/// </summary>
public static class ModMenuRegistry
{
    private const string PrimarySectionId = "__primary";
    private static readonly List<ModMenuEntry> Entries = new();
    internal static int Version { get; private set; }

    /// <summary>
    /// Registers or replaces a primary settings page for a mod.
    /// </summary>
    /// <param name="id">Stable unique mod identifier.</param>
    /// <param name="displayName">Display name shown in the mod list.</param>
    /// <param name="build">Callback that populates menu controls for the mod.</param>
    /// <param name="sortOrder">Sort priority; lower values appear first.</param>
    public static void Register(string id, string displayName, Action<ModMenuBuilder> build, int sortOrder = 0)
    {
        RegisterSection(id, displayName, PrimarySectionId, build, sortOrder);
    }

    /// <summary>
    /// Registers or replaces an additional section for an existing mod page.
    /// </summary>
    /// <param name="id">Mod identifier to attach the section to.</param>
    /// <param name="sectionId">Stable unique section identifier for that mod.</param>
    /// <param name="build">Callback that populates only this section.</param>
    public static void RegisterSupplement(string id, string sectionId, Action<IModMenuSectionBuilder> build)
    {
        if (build == null)
        {
            return;
        }

        if (!TryNormalizeKey(id, out var normalizedId))
        {
            return;
        }

        RegisterSection(normalizedId, normalizedId, sectionId,
            builder => build(new ModMenuSectionBuilderAdapter(builder)));
    }

    private static void RegisterSection(string id, string displayName, string sectionId, Action<ModMenuBuilder> build,
        int sortOrder = 0)
    {
        if (!TryNormalizeKey(id, out var normalizedId))
        {
            return;
        }

        if (!TryNormalizeKey(sectionId, out var normalizedSection))
        {
            return;
        }

        if (build == null)
        {
            return;
        }

        var normalizedName = string.IsNullOrWhiteSpace(displayName)
            ? normalizedId
            : displayName.Trim();

        var entry = FindEntry(normalizedId);
        if (entry == null)
        {
            entry = new ModMenuEntry(normalizedId, normalizedName, sortOrder);
            Entries.Add(entry);
        }
        else
        {
            entry.DisplayName = normalizedName;
            entry.SortOrder = sortOrder;
        }

        entry.SetSection(normalizedSection, build);
        Version++;
    }

    /// <summary>
    /// Removes all registered sections for a mod id.
    /// </summary>
    /// <param name="id">Mod identifier to remove.</param>
    /// <returns><c>true</c> when at least one entry was removed; otherwise <c>false</c>.</returns>
    public static bool Unregister(string id)
    {
        if (!TryNormalizeKey(id, out var normalizedId))
        {
            return false;
        }

        var removed = Entries.RemoveAll(entry =>
            entry.Id.Equals(normalizedId, StringComparison.OrdinalIgnoreCase));
        if (removed <= 0)
        {
            return false;
        }

        Version++;
        return true;
    }

    /// <summary>
    /// Removes a single supplemental section from a mod entry.
    /// </summary>
    /// <param name="id">Mod identifier.</param>
    /// <param name="sectionId">Section identifier to remove.</param>
    /// <returns><c>true</c> when the section existed and was removed; otherwise <c>false</c>.</returns>
    public static bool UnregisterSupplement(string id, string sectionId)
    {
        if (!TryNormalizeKey(id, out var normalizedId))
        {
            return false;
        }

        if (!TryNormalizeKey(sectionId, out var normalizedSection))
        {
            return false;
        }

        var entry = FindEntry(normalizedId);
        if (entry == null)
        {
            return false;
        }

        var removed = entry.RemoveSection(normalizedSection);
        if (!removed)
        {
            return false;
        }

        if (!entry.HasSections)
        {
            Entries.Remove(entry);
        }

        Version++;
        return true;
    }

    internal static IReadOnlyList<ModMenuEntry> GetEntries()
    {
        return Entries;
    }

    internal static Dictionary<string, List<Action<ModMenuBuilder>>> GetModOptionsById()
    {
        var modOptions = new Dictionary<string, List<Action<ModMenuBuilder>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
            {
                continue;
            }

            modOptions[entry.Id] = entry.GetSectionBuildActions();
        }

        return modOptions;
    }

    internal static ModMenuEntry FindEntry(string id)
    {
        if (!TryNormalizeKey(id, out var normalizedId))
        {
            return null;
        }

        return Entries.FirstOrDefault(entry => entry.Id.Equals(normalizedId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryNormalizeKey(string value, out string normalizedValue)
    {
        normalizedValue = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        normalizedValue = value.Trim();
        return true;
    }
}

internal sealed class ModMenuEntry
{
    private readonly List<ModMenuSection> _sections = new();

    internal ModMenuEntry(string id, string displayName, int sortOrder)
    {
        Id = id;
        DisplayName = displayName;
        SortOrder = sortOrder;
    }

    internal string Id { get; }
    internal string DisplayName { get; set; }
    internal int SortOrder { get; set; }
    internal bool HasSections => _sections.Count > 0;

    internal void Build(ModMenuBuilder builder)
    {
        for (var i = 0; i < _sections.Count; i++)
        {
            _sections[i].Build(builder);
        }
    }

    internal void SetSection(string sectionId, Action<ModMenuBuilder> build)
    {
        var index = FindSectionIndex(sectionId);
        var section = new ModMenuSection(sectionId, build);
        if (index >= 0)
        {
            _sections[index] = section;
            return;
        }

        _sections.Add(section);
    }

    internal bool RemoveSection(string sectionId)
    {
        var index = FindSectionIndex(sectionId);
        if (index < 0)
        {
            return false;
        }

        _sections.RemoveAt(index);
        return true;
    }

    internal List<Action<ModMenuBuilder>> GetSectionBuildActions()
    {
        var sectionBuilds = new List<Action<ModMenuBuilder>>(_sections.Count);
        foreach (var section in _sections)
        {
            if (section.Build == null)
            {
                continue;
            }

            sectionBuilds.Add(section.Build);
        }

        return sectionBuilds;
    }

    private int FindSectionIndex(string sectionId)
    {
        for (var i = 0; i < _sections.Count; i++)
        {
            if (!_sections[i].Id.Equals(sectionId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private readonly struct ModMenuSection
    {
        internal ModMenuSection(string id, Action<ModMenuBuilder> build)
        {
            Id = id;
            Build = build;
        }

        internal string Id { get; }
        internal Action<ModMenuBuilder> Build { get; }
    }
}
