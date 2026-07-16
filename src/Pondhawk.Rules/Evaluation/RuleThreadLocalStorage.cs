// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Pondhawk.Rules.Evaluation;
/// <summary>
/// Thread-local storage for the current <see cref="EvaluationContext"/>, enabling rules to insert, modify,
/// and retract facts and emit events during evaluation.
/// </summary>
public static class RuleThreadLocalStorage
{
    [ThreadStatic] private static EvaluationContext _currentContext;

    /// <summary>Gets or sets the current <see cref="EvaluationContext"/> for the executing thread.</summary>
    public static EvaluationContext CurrentContext
    {
        get => _currentContext;
        set => _currentContext = value;
    }

    /// <summary>Clears the current evaluation context from thread-local storage.</summary>
    public static void ClearCurrentContext()
    {
        CurrentContext = null;
    }
}

