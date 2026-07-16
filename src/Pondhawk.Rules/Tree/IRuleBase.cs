// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Builder;

namespace Pondhawk.Rules.Tree;

/// <summary>
/// An indexed collection of rules organized by fact types for efficient lookup during evaluation.
/// </summary>
public interface IRuleBase
{
    /// <summary>Gets the maximum fact-type arity (number of fact types per rule) in this rule base.</summary>
    int MaxAxisCount { get; }

    /// <summary>Returns <c>true</c> if any rules exist for the given fact types.</summary>
    /// <param name="factTypes">The fact types to check.</param>
    /// <returns><c>true</c> if matching rules exist; otherwise <c>false</c>.</returns>
    bool HasRules(Type[] factTypes);

    /// <summary>Returns <c>true</c> if any rules exist for the given fact types within the specified namespaces.</summary>
    /// <param name="factTypes">The fact types to check.</param>
    /// <param name="namespaces">The namespace filters to apply.</param>
    /// <returns><c>true</c> if matching rules exist; otherwise <c>false</c>.</returns>
    bool HasRules(Type[] factTypes, IEnumerable<string> namespaces);

    /// <summary>Finds all rules that match the given fact types.</summary>
    /// <param name="factTypes">The fact types to match against.</param>
    /// <returns>A set of matching rules.</returns>
    ISet<IRule> FindRules(Type[] factTypes);

    /// <summary>Finds all rules that match the given fact types within the specified namespaces.</summary>
    /// <param name="factTypes">The fact types to match against.</param>
    /// <param name="namespaces">The namespace filters to apply.</param>
    /// <returns>A set of matching rules.</returns>
    ISet<IRule> FindRules(Type[] factTypes, IEnumerable<string> namespaces);
}

