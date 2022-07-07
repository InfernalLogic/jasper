using System;
using System.Threading.Tasks;

namespace Jasper;

/// <summary>
/// Interface for cascading messages that require some customization of how
/// the resulting inner message is sent out
/// </summary>
public interface ISendMyself
{
    ValueTask ApplyAsync(IExecutionContext context);
}

/// <summary>
/// Base class that will add a scheduled delay to any messages
/// of this type that are used as a cascaded message returned from
/// a message handler
/// </summary>
public abstract record DelayedMessage(TimeSpan DelayTime) : ISendMyself
{
    public virtual ValueTask ApplyAsync(IExecutionContext context)
    {
        return context.SchedulePublishAsync(this, DelayTime);
    }
}

/// <summary>
/// DelayedMessage specifically for saga timeouts. Inheriting from TimeoutMessage
/// tells Jasper that this message is to enforce saga timeouts and can be ignored
/// if the underlying saga does not exist
/// </summary>
public abstract record TimeoutMessage(TimeSpan DelayTime) : DelayedMessage(DelayTime);
