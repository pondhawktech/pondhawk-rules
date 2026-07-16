// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Pondhawk.Rules.Listeners;

/// <summary>
/// Factory for creating <see cref="IEvaluationListener"/> instances for evaluation tracing.
/// </summary>
public interface IEvaluationListenerFactory
{
    /// <summary>
    /// Creates a new <see cref="IEvaluationListener"/> instance for tracing a rule evaluation session.
    /// </summary>
    /// <returns>A new evaluation listener.</returns>
    IEvaluationListener CreateListener();
}

