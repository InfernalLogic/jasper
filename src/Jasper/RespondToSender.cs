using System.Threading.Tasks;

namespace Jasper;

/// <summary>
/// Declares that the inner message should be sent
/// to the original sender of a processed message
/// </summary>
public class RespondToSender : ISendMyself
{
    private readonly object _message;

    public RespondToSender(object message)
    {
        _message = message;
    }

    public ValueTask ApplyAsync(IExecutionContext context)
    {
        return context.RespondToSenderAsync(_message);
    }

    protected bool Equals(RespondToSender other)
    {
        return _message.Equals(other._message);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((RespondToSender)obj);
    }

    public override string ToString()
    {
        return $"Respond to sender with : {_message}";
    }

    public override int GetHashCode()
    {
        return _message.GetHashCode();
    }
}
