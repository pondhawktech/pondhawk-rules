// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using CommunityToolkit.Diagnostics;
using Pondhawk.Rules.Builder;

namespace Pondhawk.Rules.Tree;

/// <summary>
/// Indexes rules by their fact types for fast lookup with polymorphic type matching.
/// </summary>
public sealed class RuleTree : IRuleBase, IRuleSink
{

    private volatile bool _isBuilt;
    private readonly object _buildLock = new();

    private Dictionary<int, RuleRoot> RootMap { get; } = new();

    /// <inheritdoc />
    public int MaxAxisCount => RootMap.Count > 0 ? RootMap.Keys.Max() : 0;


    /// <summary>
    /// Seals the tree and eagerly builds the type-match caches.
    /// After this call, any attempt to add rules will throw.
    /// Called automatically on the first query if not called explicitly.
    /// </summary>
    public void Build()
    {
        if (_isBuilt) return;
        lock (_buildLock)
        {
            if (_isBuilt) return;
            foreach (var root in RootMap.Values)
                root.Build();
            _isBuilt = true;
        }
    }

    private void EnsureBuilt()
    {
        if (!_isBuilt) Build();
    }

    private void ThrowIfBuilt()
    {
        if (_isBuilt)
            throw new InvalidOperationException("Cannot add rules to a RuleTree after it has been built. All rules must be added before the first evaluation.");
    }


    /// <inheritdoc />
    public bool HasRules(Type[] factTypes)
    {
        Guard.IsNotNull(factTypes);

        return HasRules(factTypes, Array.Empty<string>());
    }

    /// <inheritdoc />
    public bool HasRules(Type[] factTypes, IEnumerable<string> namespaces)
    {
        Guard.IsNotNull(factTypes);
        Guard.IsNotNull(namespaces);

        EnsureBuilt();

        RootMap.TryGetValue(factTypes.Length, out var targetRoot);
        if (targetRoot is null)
            return false;

        var ns = namespaces as List<string> ?? namespaces.ToList();
        return targetRoot.HasRules(factTypes, ns);
    }



    /// <inheritdoc />
    public ISet<IRule> FindRules(Type[] factTypes)
    {
        Guard.IsNotNull(factTypes);

        EnsureBuilt();

        RootMap.TryGetValue(factTypes.Length, out var targetRoot);
        return targetRoot is null ? new HashSet<IRule>() : targetRoot.FindRules(factTypes);
    }

    /// <inheritdoc />
    public ISet<IRule> FindRules(Type[] factTypes, IEnumerable<string> namespaces)
    {
        Guard.IsNotNull(factTypes);
        Guard.IsNotNull(namespaces);

        EnsureBuilt();

        RootMap.TryGetValue(factTypes.Length, out var targetRoot);
        if (targetRoot is null)
            return new HashSet<IRule>();

        var ns = namespaces as List<string> ?? namespaces.ToList();
        return targetRoot.FindRules(factTypes, ns);
    }


    /// <inheritdoc />
    public void Add(Type factType, IRule rules)
    {
        ThrowIfBuilt();

        if (!RootMap.TryGetValue(1, out var targetRoot))
        {
            targetRoot = new();
            RootMap[1] = targetRoot;
        }

        targetRoot.Add([factType], [rules]);

    }


    /// <inheritdoc />
    public void Add(Type[] factTypes, IEnumerable<IRule> rules)
    {
        ThrowIfBuilt();

        var axisCount = factTypes.Length;

        if (!RootMap.TryGetValue(axisCount, out var targetRoot))
        {
            targetRoot = new();
            RootMap[axisCount] = targetRoot;
        }

        targetRoot.Add(factTypes, rules);
    }


    /// <summary>Removes all rules from the tree and resets the built state.</summary>
    public void Clear()
    {
        foreach (var r in RootMap.Values)
            r.Clear();

        _isBuilt = false;
    }
}
