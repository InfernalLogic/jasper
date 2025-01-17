﻿using System.Threading.Tasks;
using Jasper.Attributes;

namespace Jasper.Persistence.Testing.SqlServer.Persistence
{
    public class SendItemEndpoint
    {
        [Transactional]
        public ValueTask post_send_item(ItemCreated created, IMessageContext context)
        {
            return context.SendAsync(created);
        }
    }
}
