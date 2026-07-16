// Copyright (c) Pond Hawk Technologies Inc. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pondhawk.Rules.Listeners;

/// <summary>
/// Creates <see cref="WatchEvaluationListener"/> instances using a configured <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/>.
/// </summary>
public sealed class WatchEvaluationListenerFactory : IEvaluationListenerFactory
{

    /// <summary>
    /// Gets or sets the logger factory used to create loggers for evaluation listeners. Defaults to <see cref="NullLoggerFactory.Instance"/>.
    /// </summary>
    public ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

    /// <summary>
    /// Gets or sets the logging category name passed to the logger factory.
    /// </summary>
    public string Category { get; set; } = string.Empty;


    /// <inheritdoc />
    public IEvaluationListener CreateListener()
    {
        var logger = LoggerFactory.CreateLogger(Category);
        return new WatchEvaluationListener(logger);
    }

}
