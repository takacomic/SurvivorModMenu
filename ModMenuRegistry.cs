using System;
using System.Collections.Generic;

namespace SurvivorModMenu;

public static class ModMenuRegistry
{
    private const string PrimarySectionId = "__primary";
    private static readonly List<ModMenuEntry> Entries = new();
    internal static int Version { get; private set; }

    public static void Register(string id, string displayName, Action<ModMenuBuilder> build, int sortOrder = 0)
    {
        RegisterSection(id, displayName, PrimarySectionId, build, sortOrder);
    }

    public static void RegisterSupplement(string id, string sectionId, Action<ModMenuBuilder> build)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        var displayName = id.Trim();
        RegisterSection(id, displayName, sectionId, build);
    }

    public static void RegisterSupplement(string id, string sectionId, Action<IModMenuSectionBuilder> build)
    {
        if (build == null)
            return;

        RegisterSupplement(id, sectionId, builder => build(new ModMenuSectionBuilderAdapter(builder)));
    }

    public static void RegisterSection(string id, string displayName, string sectionId, Action<ModMenuBuilder> build, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (string.IsNullOrWhiteSpace(sectionId))
            return;

        if (build == null)
            return;

        var normalizedId = id.Trim();
        var normalizedName = string.IsNullOrWhiteSpace(displayName) ? normalizedId : displayName.Trim();
        var normalizedSection = sectionId.Trim();

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

    public static bool Unregister(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        var removed = Entries.RemoveAll(entry => entry.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
        if (removed <= 0)
            return false;

        Version++;
        return true;
    }

    public static bool UnregisterSupplement(string id, string sectionId)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        if (string.IsNullOrWhiteSpace(sectionId))
            return false;

        var entry = FindEntry(id);
        if (entry == null)
            return false;

        var removed = entry.RemoveSection(sectionId.Trim());
        if (!removed)
            return false;

        if (!entry.HasSections)
            Entries.Remove(entry);

        Version++;
        return true;
    }

    internal static IReadOnlyList<ModMenuEntry> GetEntries()
    {
        return Entries;
    }

    internal static ModMenuEntry FindEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var normalizedId = id.Trim();
        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (!entry.Id.Equals(normalizedId, StringComparison.OrdinalIgnoreCase))
                continue;

            return entry;
        }

        return null;
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
            _sections[i].Build(builder);
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
            return false;

        _sections.RemoveAt(index);
        return true;
    }

    private int FindSectionIndex(string sectionId)
    {
        for (var i = 0; i < _sections.Count; i++)
        {
            if (!_sections[i].Id.Equals(sectionId, StringComparison.OrdinalIgnoreCase))
                continue;

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
