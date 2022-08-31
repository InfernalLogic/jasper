using System.Threading.Tasks;
using TestingSupport.Compliance;
using TestMessages;

namespace Jasper.Testing.Runtime.Samples
{





    #region sample_sending_messages_for_static_routing
    public class SendingExample
    {
        public async Task SendPingsAndPongs(IMessageContext bus)
        {
            // Publish a message
            await bus.SendAsync(new PingMessage());
        }
    }
    #endregion


}
