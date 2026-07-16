// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Pondhawk.Rules.Evaluation;

namespace Pondhawk.Rules.Validators;

/// <summary>
/// Base class for validators, providing condition collection and <c>Otherwise</c> violation message builders.
/// </summary>
public abstract class BaseValidator<TFact>
{


    /// <summary>
    /// Initializes a new instance of the <see cref="BaseValidator{TFact}"/> class.
    /// </summary>
    /// <param name="rule">The validation rule that owns this validator.</param>
    /// <param name="group">The group name used to categorize violation messages.</param>
    protected BaseValidator(ValidationRule<TFact> rule, string group)
    {

        Rule = rule;
        Group = group;

        Conditions = [];
        Consequence = f => { };

    }


    private string Group { get; }

    private ValidationRule<TFact> Rule { get; }


    /// <summary>
    /// Gets the list of condition functions that must all return <c>true</c> for the validation to pass.
    /// </summary>
    public IList<Func<TFact, bool>> Conditions { get; }

    /// <summary>
    /// Gets the action to execute when the validation fails, which emits a violation event.
    /// </summary>
    public Action<TFact> Consequence { get; private set; }


    /// <summary>
    /// Specifies the violation message to emit when the validation condition fails.
    /// </summary>
    /// <param name="template">A message template, optionally with format placeholders.</param>
    /// <param name="parameters">Functions that extract values from the fact to fill the template placeholders.</param>
    /// <returns>The validation rule for further configuration.</returns>
    public IValidationRule<TFact> Otherwise(string template, params Func<TFact, object>[] parameters)
    {
        Consequence = f => _BuildMessage(f, RuleEvent.EventCategory.Violation, Group, template, parameters);
        return Rule;
    }

    /// <summary>
    /// Specifies the violation message and group name to emit when the validation condition fails.
    /// </summary>
    /// <param name="group">The group name for the violation event.</param>
    /// <param name="template">A message template, optionally with format placeholders.</param>
    /// <param name="parameters">Functions that extract values from the fact to fill the template placeholders.</param>
    /// <returns>The validation rule for further configuration.</returns>
    public IValidationRule<TFact> Otherwise(string group, string template, params Func<TFact, object>[] parameters)
    {
        Consequence = f => _BuildMessage(f, RuleEvent.EventCategory.Violation, group, template, parameters);
        return Rule;
    }

    /// <summary>
    /// Specifies the event category, group name, and violation message to emit when the validation condition fails.
    /// </summary>
    /// <param name="category">The event category (e.g. Violation, Warning, Info).</param>
    /// <param name="group">The group name for the violation event.</param>
    /// <param name="template">A message template, optionally with format placeholders.</param>
    /// <param name="parameters">Functions that extract values from the fact to fill the template placeholders.</param>
    /// <returns>The validation rule for further configuration.</returns>
    public IValidationRule<TFact> Otherwise(RuleEvent.EventCategory category, string group, string template, params Func<TFact, object>[] parameters)
    {
        Consequence = f => _BuildMessage(f, category, group, template, parameters);
        return Rule;
    }


    private static void _BuildMessage(TFact fact, RuleEvent.EventCategory category, string group, string template, Func<TFact, object>[] parameters)
    {
        int len = parameters.Length;

        var markers = new object[len];
        for (int i = 0; i < len; i++)
        {
            object o = parameters[i](fact) ?? "null";
            markers[i] = o;
        }

        string desc = len == 0 ? template : string.Format(System.Globalization.CultureInfo.InvariantCulture, template, markers);
        RuleThreadLocalStorage.CurrentContext.Event(category, group, desc);
    }
}
