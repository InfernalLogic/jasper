using System;
using System.Threading.Tasks;

namespace Jasper.Runtime;

#region sample_IContinuation

/// <summary>
///     Represents an action to take after processing a message
/// </summary>
public interface IContinuation
{
    /// <summary>
    ///     Post-message handling action
    /// </summary>
    /// <param name="context"></param>
    /// <param name="runtime"></param>
    /// <param name="now"></param>
    /// <returns></returns>
    ValueTask ExecuteAsync(IMessageContext context, IJasperRuntime runtime, DateTimeOffset now);
}

#endregion
