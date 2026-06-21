using System.Collections.Concurrent;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;
using Topaz.Service.ServiceBus.Filtering;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

internal sealed class LinkProcessor(ITopazLogger logger, ServiceBusRuleLoader? ruleLoader = null) : ILinkProcessor
{
    // Maps AMQP session (by reference) → CBS response link (server-side sender).
    // Populated when a client opens a ReceiverLink at "$cbs"; consumed when the
    // matching SenderLink's CbsRequestEndpoint processes a put-token request.
    private readonly ConcurrentDictionary<Session, ListenerLink> _cbsResponseLinks
        = new(ReferenceEqualityComparer.Instance);

    // Maps AMQP session (by reference) → queue $management response link (server-side sender).
    // Populated when a client opens a ReceiverLink at "{queue}/$management"; consumed when the
    // matching SenderLink's QueueManagementRequestEndpoint processes a management request
    // (e.g. com.microsoft:complete, com.microsoft:renew-lock).
    private readonly ConcurrentDictionary<Session, ListenerLink> _queueManagementResponseLinks
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

        if (address != null && address.EndsWith("/$management", StringComparison.OrdinalIgnoreCase))
        {
            HandleQueueManagementLink(attachContext);
            return;
        }

        if (attachContext.Attach.Role)
        {
            // Parse com.microsoft:session-filter from the Attach Source filter-set (if present).
            // A null value means "any session" (wildcard); a string value means a specific session.
            // The sentinel SessionFilterRequested.Any is used to distinguish "wildcard requested"
            // from "no session filter at all".
            string? sessionFilter = null;
            bool hasSessionFilter = false;
            if (attachContext.Attach.Source is Source source && source.FilterSet != null)
            {
                foreach (var key in source.FilterSet.Keys)
                {
                    var keyStr = key?.ToString() ?? string.Empty;
                    if (keyStr.Contains("session-filter", StringComparison.OrdinalIgnoreCase))
                    {
                        hasSessionFilter = true;
                        sessionFilter = source.FilterSet[key]?.ToString(); // null = wildcard
                        break;
                    }
                }
            }

            attachContext.Complete(
                new OutgoingLinkEndpoint(address ?? string.Empty, logger, ruleLoader,
                    hasSessionFilter ? (sessionFilter ?? OutgoingLinkEndpoint.WildcardSession) : null),
                300);
        }
        else
        {
            attachContext.Complete(new IncomingLinkEndpoint(address ?? string.Empty, logger, ruleLoader), 300);
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

    private void HandleQueueManagementLink(AttachContext attachContext)
    {
        var address = attachContext.Attach.Role
            ? ((Source?)attachContext.Attach.Source)?.Address
            : ((Target?)attachContext.Attach.Target)?.Address;

        if (attachContext.Attach.Role)
        {
            // Client opens a ReceiverLink to "{queue}/$management" → server is the sender →
            // this is the management RESPONSE link.  Register it by session so that
            // QueueManagementRequestEndpoint can look it up and send 200 OK replies.
            logger.LogInformation($"[{nameof(LinkProcessor)}.{nameof(HandleQueueManagementLink)}] Registering queue management response link for address '{address}'.");

            var session = attachContext.Link.Session;
            _queueManagementResponseLinks[session] = attachContext.Link;
            attachContext.Link.AddClosedCallback((_, _) => _queueManagementResponseLinks.TryRemove(session, out _));
            attachContext.Complete(new QueueManagementResponseEndpoint(), 300);
        }
        else
        {
            // Client opens a SenderLink to "{queue}/$management" → server is the receiver →
            // this is the management REQUEST link.  Pass the session registry so OnMessage
            // can send responses (200 OK) for com.microsoft:complete, renew-lock, etc.
            logger.LogInformation($"[{nameof(LinkProcessor)}.{nameof(HandleQueueManagementLink)}] Registering queue management request link for address '{address}'.");

            // Strip the /$management suffix to get the bare entity address for session operations.
            var entityAddress = address != null && address.EndsWith("/$management", StringComparison.OrdinalIgnoreCase)
                ? address[..^"/$management".Length].TrimStart('/')
                : address ?? string.Empty;

            attachContext.Complete(new QueueManagementRequestEndpoint(_queueManagementResponseLinks, logger, ruleLoader, entityAddress), 300);
        }
    }
}