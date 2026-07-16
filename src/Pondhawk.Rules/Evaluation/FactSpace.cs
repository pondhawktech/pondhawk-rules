// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Pondhawk.Rules.Evaluation;

/// <summary>
/// Stores facts with integer selectors for memory-efficient type-indexed retrieval during rule evaluation.
/// </summary>
public sealed class FactSpace
{

    private List<object> Facts { get; } = [];
    private HashSet<object> Guard { get; } = [];

    private Dictionary<Type, Schema> SchemaMap { get; } = new();
    internal List<Schema> Schema { get; } = [];

    private int NextPosition { get; set; } = 1;
    private Dictionary<int, int> SelectorMap { get; } = new();


    internal int TypeCount => Schema.Count;


    internal int[] GetIdentityFromSelector(int[] selectorIndices)
    {
        int len = selectorIndices.Length;

        var identityIndices = new int[len];
        for (int i = 0; i < len; i++)
            identityIndices[i] = SelectorMap[selectorIndices[i]];

        return identityIndices;
    }


    private void _InternalAdd(object fact, int identity)
    {
        if (SelectorMap.Count >= 65535)
            throw new InvalidOperationException("Evaluation is limited to 65535 facts at a time.");

        Type factType = fact.GetType();

        if (!SchemaMap.TryGetValue(factType, out var schema))
        {
            schema = new Schema { FactType = factType };

            SchemaMap[factType] = schema;
            Schema.Add(schema);
        }

        // Add the new facts identity to the reference map
        SelectorMap[NextPosition] = identity;

        // Add this reference to the schema for this type
        schema.Members.Add(NextPosition);

        NextPosition++;
    }



    internal Type[] GetFactTypes(byte[] signatureIndices)
    {
        int len = signatureIndices.Length;

        var types = new Type[len];

        for (int i = 0; i < len; i++)
            types[i] = Schema[signatureIndices[i]].FactType;

        return types;
    }



    internal object[] GetTuple(int[] selectorIndices)
    {
        int count = selectorIndices.Length;

        var tuple = new object[count];
        for (int i = 0; i < count; i++)
        {
            int selectorIndex = selectorIndices[i];

            if (SelectorMap.TryGetValue(selectorIndex, out var identityIndex))
                tuple[i] = Facts[identityIndex];
            else
                return null;
        }

        return tuple;
    }


    internal bool GetTuple(int[] selectorIndices, int length, object[] tuple)
    {
        for (int i = 0; i < length; i++)
        {
            if (SelectorMap.TryGetValue(selectorIndices[i], out var identityIndex))
                tuple[i] = Facts[identityIndex];
            else
                return false;
        }
        return true;
    }


    internal void GetIdentityFromSelector(int[] selectorIndices, int length, int[] identityIndices)
    {
        for (int i = 0; i < length; i++)
            identityIndices[i] = SelectorMap[selectorIndices[i]];
    }


    internal void InsertFact(object fact) => Add(fact);

    internal void ModifyFact(int selectorIndex)
    {
        var identityIndex = SelectorMap[selectorIndex];
        SelectorMap.Remove(selectorIndex);

        var fact = Facts[identityIndex];

        _InternalAdd(fact, identityIndex);

    }

    internal void RetractFact(int reference)
    {
        SelectorMap.Remove(reference);
    }


    /// <summary>Adds one or more facts to the fact space.</summary>
    /// <param name="facts">The facts to add.</param>
    public void Add(params object[] facts)
    {
        AddAll(facts);
    }

    /// <summary>Adds all facts from the collection to the fact space.</summary>
    /// <param name="facts">The facts to add.</param>
    public void AddAll(IEnumerable<object> facts)
    {

        foreach (var f in facts.Where(f => !Guard.Contains(f)))
        {

            Facts.Add(f);
            Guard.Add(f);

            _InternalAdd(f, (Facts.Count - 1));
        }

    }

}
