// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using CommunityToolkit.Diagnostics;

namespace Pondhawk.Rules.Builder;

/// <summary>
/// Discovers and collects <see cref="IBuilder"/> types from assemblies or explicit type lists.
/// </summary>
public class RuleBuilderSource : IRuleBuilderSource
{

    private static Func<Type, bool> Predicate { get; } = t => typeof(IBuilder).IsAssignableFrom(t);

    /// <summary>Discovers and adds all <see cref="IBuilder"/> types from the specified assemblies.</summary>
    /// <param name="assemblies">The assemblies to scan for builder types.</param>
    public void AddTypes(params Assembly[] assemblies)
    {

        Guard.IsNotNull(assemblies);

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()).Where(Predicate))
            Types.Add(type);
    }

    /// <summary>Adds the specified types that implement <see cref="IBuilder"/>.</summary>
    /// <param name="types">The types to add.</param>
    public void AddTypes(params Type[] types)
    {

        Guard.IsNotNull(types);

        foreach (var type in types.Where(Predicate))
            Types.Add(type);
    }

    /// <summary>Adds types from the candidate collection that implement <see cref="IBuilder"/>.</summary>
    /// <param name="candidates">The candidate types to filter and add.</param>
    public void AddTypes(IEnumerable<Type> candidates)
    {

        Guard.IsNotNull(candidates);

        foreach (var type in candidates.Where(Predicate))
            Types.Add(type);
    }

    private HashSet<Type> Types { get; } = new();

    /// <inheritdoc />
    public IEnumerable<Type> GetTypes()
    {
        return Types;
    }

}
