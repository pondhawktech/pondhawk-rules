// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Pondhawk.Rules.Evaluation;

/// <summary>
/// Factory for creating <see cref="EvaluationContext"/> instances pre-configured for a specific rule set.
/// </summary>
public interface IEvaluationContextFactory
{
    /// <summary>Creates a new <see cref="EvaluationContext"/> pre-configured for evaluation.</summary>
    /// <returns>A new evaluation context instance.</returns>
    EvaluationContext CreateContext();
}

