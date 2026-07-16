// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Evaluation;

namespace Pondhawk.Rules.Builder;

internal class SubRule<TParent, TFact>(IEnumerable<TFact> facts, Action<TFact> consequence, TParent parent, Func<TParent, object> modifyFunc) : IRule
{
    private IEnumerable<TFact> Facts { get; } = facts;
    private Action<TFact> Consequence { get; } = consequence;

    private TParent Parent { get; } = parent;
    private Func<TParent, object> ModifyFunc { get; } = modifyFunc;

    public string Namespace { get; set; }
    public string Name { get; set; }
    public int Salience { get; set; }
    public bool OnlyFiresOnce { get; set; }
    public string Mutex { get; set; }
    public DateTime Inception { get; set; }
    public DateTime Expiration { get; set; }


    public IRule EvaluateRule(object[] fact) => this;

    public void FireRule(object[] offered)
    {

        foreach (var fact in Facts)
            Consequence(fact);

        if (ModifyFunc is not null)
        {
            var modified = ModifyFunc(Parent);
            if (modified is Array arr)
            {
                foreach (var o in arr)
                    RuleThreadLocalStorage.CurrentContext.ModifyFact(o);
            }
            else if (modified is not null)
                RuleThreadLocalStorage.CurrentContext.ModifyFact(modified);
        }

        RuleThreadLocalStorage.CurrentContext.Results.TotalFired++;

    }

}
