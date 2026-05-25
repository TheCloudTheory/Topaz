using System.Collections.Concurrent;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

internal sealed class LinkProcessor(ITopazLogger logger) : ILinkProcessor
{
    // Maps AMQP session (by reference) → CBS response link (server-side sender).
    // Populated when a client opens a ReceiverLink at "$cbs"; consumed when the
    // matching SenderLink's CbsRequestEndpoint processes a put-token request.
    private readonly ConcurrentDictionary<Session, ListenerLink> _cbsResponseLinks
        = new(ReferenceEqualityComparer.Instance);

    public void Process(AttachContext attachContext)
    {
        // Setting MaxMessageSize is required for the Azure SDK: the default value
        // (long.MaxValue) overflows in the SDK, resulting in -1 as the max size,
        // which breaks batch communication.
        attachContext.Attach.MaxMessageSize = 262144;

        // Resolve the node address from the Attach frame.
        //   attach.Role = true  → client is receiver → address is in Source
        //   attach.Role = false → client is sender   → address is in Target
        var address = attachContext.Attach.Role
            ? ((Source?)attachContext.Attach.Source)?.Address
            : ((Target?)attachContext.Attach.Target)?.Address;

        if (address == "$cbs")
        {
            HandleCbsLink(attachContext);
            return;
        }

        if (attachContext.Attach.Role)
        {
            attachContext.Complete(new OutgoingLinkEndpoint(address ?? string.Empty, logger), 300);
        }
        else
        {
            attachContext.Complete(new IncomingLinkEndpoint(address ?? string.Empty, logger), 300);
        }
    }

    private void HandleCbsLink(AttachContext attachContext)
    {
        if (attachContext.Attach.Role)
        {
            // Client opens a ReceiverLink → server is the sender → this is the
            // CBS response link.  Register it by session so CbsRequestEndpoint
            // can look it up without needing reply_to on the request message.
            var session = attachContext.Link.Session;
            _cbsResponseLinks[session] = attachContext.Link;
            attachContext.Link.AddClosedCallback((_, _) => _cbsResponseLinks.TryRemove(session, out _));
            attachContext.Complete(new CbsResponseEndpoint(), 300);
        }
        else
        {
            // Client opens a SenderLink → server is the receiver → this is the
            // CBS request link.  Pass the session registry so OnMessage can send
            // the response directly.
            attachContext.Complete(new CbsRequestEndpoint(_cbsResponseLinks), 300);
        }
    }
}