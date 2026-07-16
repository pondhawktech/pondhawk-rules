// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Tree;

namespace Pondhawk.Rules.Builder;

/// <summary>
///  Types that implement this interface create rules to added to a RuleSet for
/// runtime evaluation
/// </summary>
public interface IBuilder
{

    /// <summary>
    /// Gets the set name these rules are associated with. Typically, this is the
    /// namespace the rules are defined in but this can be changed to any name desired
    /// </summary>
    string SetName { get; }


    /// <summary>
    /// Gets the rules that this builder defines. This is nomally called to add the
    /// rules to a rule set for eventual evealuation
    /// </summary>
    void LoadRules(IRuleSink sink);


}
