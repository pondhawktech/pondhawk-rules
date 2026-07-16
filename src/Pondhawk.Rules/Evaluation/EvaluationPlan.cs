// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Tree;
using Pondhawk.Rules.Util;

namespace Pondhawk.Rules.Evaluation;

internal class EvaluationPlan
{
    public EvaluationPlan(IRuleBase ruleBase, IEnumerable<string> namespaces, FactSpace space)
    {
        RuleBase = ruleBase;
        Space = space;
        Namespaces = namespaces;

        Count = ruleBase.MaxAxisCount;

        Queues = new Queue<EvaluationStep>[Count];
        for (int q = 0; q < Count; q++)
            Queues[q] = new(1024);
    }


    private IRuleBase RuleBase { get; }

    private FactSpace Space { get; }

    private IEnumerable<string> Namespaces { get; }

    private HashSet<long> IssuedSelectors { get; } = [];

    private int Count { get; }
    private Queue<EvaluationStep>[] Queues { get; }

    private List<byte[]> CachedVariations { get; set; }
    private int CachedTypeCount { get; set; }


    private static IEnumerable<IEnumerable<byte>> _GetVariations(int maxAxis, int typeCount)
    {
        var source = new byte[typeCount];
        for (int i = 0; i < typeCount; i++)
            source[i] = (byte)i;

        return source.VariationsWithRepetition(maxAxis);
    }


    private int _GenerateSteps(byte[] typeIndices)
    {
        int stepsAdded = 0;

        int len = typeIndices.Length;

        var factIndices = new IList<int>[len];
        for (int i = 0; i < len; i++)
        {
            byte typeIndex = typeIndices[i];
            factIndices[i] = Space.Schema[typeIndex].Members;
        }

        Type[] factTypes = Space.GetFactTypes(typeIndices);
        if (RuleBase.FindRules(factTypes, Namespaces).Count == 0)
            return stepsAdded;

        int signature = Helpers.EncodeSignature(typeIndices);

        if (len == 1)
        {
            foreach (int p in factIndices[0])
            {
                _AddStep(signature, [p], 1);
                stepsAdded++;
            }
        }
        else if (len == 2)
        {
            foreach (int p1 in factIndices[0])
                foreach (int p2 in factIndices[1])
                {
                    _AddStep(signature, [p1, p2], 2);
                    stepsAdded++;
                }
        }
        else if (len == 3)
        {
            foreach (int p1 in factIndices[0])
                foreach (int p2 in factIndices[1])
                    foreach (int p3 in factIndices[2])
                    {
                        _AddStep(signature, [p1, p2, p3], 3);
                        stepsAdded++;
                    }
        }
        else if (len == 4)
        {
            foreach (int p1 in factIndices[0])
                foreach (int p2 in factIndices[1])
                    foreach (int p3 in factIndices[2])
                        foreach (int p4 in factIndices[3])
                        {
                            _AddStep(signature, [p1, p2, p3, p4], 4);
                            stepsAdded++;
                        }
        }

        return stepsAdded;
    }

    private void _AddStep(int signature, int[] indices, int priority)
    {
        long selector = Helpers.EncodeSelector(indices);
        if (IssuedSelectors.Contains(selector))
            return;

        IssuedSelectors.Add(selector);

        var step = new EvaluationStep { Priority = priority, Signature = signature, Selector = selector };

        Queues[priority - 1].Enqueue(step);
    }


    internal BuildStats Build()
    {
        var stats = new BuildStats();

        if (Space.TypeCount == 0)
            return stats;

        int axisCount = RuleBase.MaxAxisCount;

        if (axisCount == 0)
            return stats;

        if (CachedVariations is null || Space.TypeCount != CachedTypeCount)
        {
            CachedTypeCount = Space.TypeCount;
            CachedVariations = [];
            foreach (var v in _GetVariations(axisCount, Space.TypeCount))
                CachedVariations.Add(v.ToArray());
        }

        foreach (var v in CachedVariations)
        {
            stats.VariationsConsidered++;
            int stepsAdded = _GenerateSteps(v);

            if (stepsAdded > 0)
            {
                stats.VariationsFound++;
                stats.StepsAdded += stepsAdded;
            }
        }

        return stats;
    }


    internal void Modify() => Build();


    internal EvaluationStep Next()
    {
        var next = new EvaluationStep { Signature = 0, Selector = 0 };

        for (int q = 0; q < Count; q++)
        {
            if (Queues[q].Count > 0)
            {
                next = Queues[q].Dequeue();
                return next;
            }
        }

        return next;
    }
}
