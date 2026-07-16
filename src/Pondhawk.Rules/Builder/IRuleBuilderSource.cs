// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Pondhawk.Rules.Builder;

/// <summary>
/// Provides <see cref="IBuilder"/> types for rule set construction.
/// </summary>
public interface IRuleBuilderSource
{
    /// <summary>Gets the collection of <see cref="IBuilder"/> types discovered by this source.</summary>
    /// <returns>An enumeration of builder types.</returns>
    IEnumerable<Type> GetTypes();
}


