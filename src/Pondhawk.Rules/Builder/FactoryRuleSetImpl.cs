// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Evaluation;
using Pondhawk.Rules.Tree;

namespace Pondhawk.Rules.Builder;

internal class FactoryRuleSetImpl(IRuleBase ruleBase, IEnumerable<string> namespaces, IEvaluationContextFactory contextFactory) : AbstractRuleSet
{
    private IRuleBase RuleBase { get; } = ruleBase;

    internal IEnumerable<string> Namespaces { get; } = namespaces;

    protected override IRuleBase GetRuleBase()
    {
        return RuleBase;
    }

    protected override IEnumerable<string> GetNamespaces()
    {
        return Namespaces;
    }


    private IEvaluationContextFactory ContextFactory { get; } = contextFactory;

    public override EvaluationContext GetEvaluationContext()
    {
        return ContextFactory == null ? new() : ContextFactory.CreateContext();
    }
}

