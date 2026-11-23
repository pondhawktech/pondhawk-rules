// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Evaluation;
using Pondhawk.Rules.Factory;
using Shouldly;
using Xunit;

namespace Pondhawk.Rules.Tests;

public class NestedEvaluationTests
{

    private static EvaluationResults EvaluateSafe(RuleSet ruleSet, params object[] facts)
    {
        var ctx = ruleSet.GetEvaluationContext();
        ctx.ThrowValidationException = false;
        ctx.ThrowNoRulesException = false;
        ctx.AddAllFacts(facts);
        return ruleSet.Evaluate(ctx);
    }


    [Fact]
    public void Evaluate_InnerEvaluationInConsequence_OuterContextIsRestored()
    {
        var inner = new RuleSet();
        inner.AddRule<Order>("inner-rule")
            .When(o => o.Total > 0)
            .Then("Inner", "Inner fired for {0}", o => o.OrderId);

        EvaluationResults innerResult = null;

        var outer = new RuleSet();
        outer.AddRule<Person>("outer-first")
            .WithSalience(1)
            .Then(p => innerResult = EvaluateSafe(inner, new Order { OrderId = 7, Total = 10m }));
        outer.AddRule<Person>("outer-second")
            .WithSalience(2)
            .Then("Outer", "Outer fired for {0}", p => p.Name);

        var result = EvaluateSafe(outer, new Person { Name = "Alice", Age = 30 });

        innerResult.ShouldNotBeNull();
        innerResult.Events.ShouldContain(e => e.Group == "Inner");
        innerResult.Events.ShouldNotContain(e => e.Group == "Outer");

        result.TotalFired.ShouldBe(2);
        result.Events.ShouldContain(e => e.Group == "Outer" && e.Message == "Outer fired for Alice");
        result.Events.ShouldNotContain(e => e.Group == "Inner");
    }

    [Fact]
    public void Evaluate_AfterCompletion_ContextIsClearedFromThread()
    {
        var ruleSet = new RuleSet();
        ruleSet.AddRule<Person>("any")
            .Then("Group", "Fired");

        EvaluateSafe(ruleSet, new Person { Name = "Bob" });

        RuleThreadLocalStorage.CurrentContext.ShouldBeNull();
    }

}
