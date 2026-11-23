// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Evaluation;
using Pondhawk.Rules.Factory;
using Pondhawk.Rules.Listeners;
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


    // ========== Listener failures ==========

    private sealed class ThrowingListener : IEvaluationListener
    {
        public bool ThrowOnBegin { get; init; }
        public bool ThrowOnEnd { get; init; }
        public int BeginCalls { get; private set; }
        public int EndCalls { get; private set; }

        public void BeginEvaluation()
        {
            BeginCalls++;
            if (ThrowOnBegin)
                throw new InvalidOperationException("begin failed");
        }

        public void EndEvaluation()
        {
            EndCalls++;
            if (ThrowOnEnd)
                throw new InvalidOperationException("end failed");
        }

        public void BeginTupleEvaluation(object[] facts) { }
        public void FiringRule(Builder.IRule rule) { }
        public void FiredRule(Builder.IRule rule, bool modified) { }
        public void EndTupleEvaluation(object[] facts) { }
        public void EventCreated(RuleEvent evalEvent) { }
        public void Debug(string template, params object[] markers) { }
        public void Warning(string template, params object[] markers) { }
    }

    private static EvaluationContext ContextWith(RuleSet ruleSet, IEvaluationListener listener, params object[] facts)
    {
        var ctx = ruleSet.GetEvaluationContext();
        ctx.ThrowValidationException = false;
        ctx.ThrowNoRulesException = false;
        ctx.Listener = listener;
        ctx.AddAllFacts(facts);
        return ctx;
    }

    private static RuleSet SimpleRuleSet()
    {
        var ruleSet = new RuleSet();
        ruleSet.AddRule<Person>("any")
            .Then("Group", "Fired");
        return ruleSet;
    }

    [Fact]
    public void Evaluate_BeginEvaluationThrows_ContextClearedAndEndNotCalled()
    {
        var ruleSet = SimpleRuleSet();
        var listener = new ThrowingListener { ThrowOnBegin = true };
        var ctx = ContextWith(ruleSet, listener, new Person { Name = "Bob" });

        Should.Throw<InvalidOperationException>(() => ruleSet.Evaluate(ctx)).Message.ShouldBe("begin failed");

        RuleThreadLocalStorage.CurrentContext.ShouldBeNull();
        listener.BeginCalls.ShouldBe(1);
        listener.EndCalls.ShouldBe(0);
    }

    [Fact]
    public void Evaluate_EndEvaluationThrows_ContextCleared()
    {
        var ruleSet = SimpleRuleSet();
        var listener = new ThrowingListener { ThrowOnEnd = true };
        var ctx = ContextWith(ruleSet, listener, new Person { Name = "Bob" });

        Should.Throw<InvalidOperationException>(() => ruleSet.Evaluate(ctx)).Message.ShouldBe("end failed");

        RuleThreadLocalStorage.CurrentContext.ShouldBeNull();
        listener.EndCalls.ShouldBe(1);
    }

    [Fact]
    public void Evaluate_NestedEndEvaluationThrows_OuterContextRestored()
    {
        var inner = SimpleRuleSet();
        var innerListener = new ThrowingListener { ThrowOnEnd = true };

        EvaluationContext outerContext = null;
        EvaluationContext contextAfterInner = null;

        var outer = new RuleSet();
        outer.AddRule<Order>("outer")
            .Then(o =>
            {
                outerContext = RuleThreadLocalStorage.CurrentContext;
                var innerCtx = ContextWith(inner, innerListener, new Person { Name = "Bob" });
                Should.Throw<InvalidOperationException>(() => inner.Evaluate(innerCtx));
                contextAfterInner = RuleThreadLocalStorage.CurrentContext;
            });

        EvaluateSafe(outer, new Order { OrderId = 1 });

        outerContext.ShouldNotBeNull();
        contextAfterInner.ShouldBeSameAs(outerContext);
        RuleThreadLocalStorage.CurrentContext.ShouldBeNull();
    }

}
