// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Builder;

namespace Pondhawk.Rules.Tree;

/// <summary>
/// Accepts rules during rule set construction, indexing them by their fact types.
/// </summary>
public interface IRuleSink
{
    /// <summary>Adds a single rule indexed by the specified fact type.</summary>
    /// <param name="factType">The fact type this rule operates on.</param>
    /// <param name="rules">The rule to add.</param>
    void Add(Type factType, IRule rules);

    /// <summary>Adds a collection of rules indexed by the specified fact types.</summary>
    /// <param name="factTypes">The fact types these rules operate on.</param>
    /// <param name="rules">The rules to add.</param>
    void Add(Type[] factTypes, IEnumerable<IRule> rules);
}


