// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Builder;

namespace Pondhawk.Rules.Listeners;

/// <summary>
/// A no-op implementation of <see cref="IEvaluationListener"/> that discards all events.
/// </summary>
public sealed class NoopEvaluationListener : IEvaluationListener
{
    /// <inheritdoc />
    public void BeginEvaluation()
    {
    }

    /// <inheritdoc />
    public void BeginTupleEvaluation(object[] facts)
    {
    }

    /// <inheritdoc />
    public void FiringRule(IRule rule)
    {
    }

    /// <inheritdoc />
    public void FiredRule(IRule rule, bool modified)
    {
    }

    /// <inheritdoc />
    public void EndTupleEvaluation(object[] facts)
    {
    }

    /// <inheritdoc />
    public void EndEvaluation()
    {
    }

    /// <inheritdoc />
    public void Debug(string template, params object[] markers)
    {
    }

    /// <inheritdoc />
    public void Warning(string template, params object[] markers)
    {
    }

    /// <inheritdoc />
    public void EventCreated(RuleEvent evalEvent)
    {
    }

}

