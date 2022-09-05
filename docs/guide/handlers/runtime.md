# Message Handling Runtime



Next, even though I said that Jasper does not require an adapter interface in *your* code, Jasper itself does actually need that for its own internal runtime pipeline. To that end
Jasper uses

TODO -- link to new documentation on pre-generated adapter code


2. Create a new instance of that handler class for a new message
3. Execute the `Handle(MyMessage)` method against the `MyMessage` object passed in up above to `ICommandBus.EnqueueAsync()`



## How Jasper Consumes Your Message Handlers

If you're worried about the performance implications of Jasper calling into your code without any interfaces or base classes, nothing to worry about because Jasper **does not use Reflection at runtime** to call your actions. Instead, Jasper uses [runtime
code generation with Roslyn](https://jeremydmiller.com/2015/11/11/using-roslyn-for-runtime-code-generation-in-marten/) to write the "glue" code around your actions. Internally, Jasper is generating a subclass of `MessageHandler` for each known message type:

<!-- snippet: sample_MessageHandler -->
<a id='snippet-sample_messagehandler'></a>
```cs
public interface IMessageHandler
{
    Task HandleAsync(IMessageContext context, CancellationToken cancellation);
}

public abstract class MessageHandler : IMessageHandler
{
    public HandlerChain? Chain { get; set; }

    public abstract Task HandleAsync(IMessageContext context, CancellationToken cancellation);
}
```
<sup><a href='https://github.com/JasperFx/alba/blob/master/src/Jasper/Runtime/Handlers/MessageHandler.cs#L6-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_messagehandler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## IoC Container Integration


## Code Generation



