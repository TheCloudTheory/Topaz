using System.Reflection;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;
using Topaz.Service.ServiceBus.Filtering;
using Topaz.Shared;

namespace Topaz.Host.AMQP;

public class OutgoingLinkEndpoint : LinkEndpoint
{
    private readonly ITopazLogger _logger;
    private readonly ServiceBusRuleLoader? _ruleLoader;

    /// <summary>
    /// Sentinel value used when a session-filter Attach was received with a null (wildcard) session ID.
    /// Distinguishes "receiver wants any session" from "receiver is not session-aware".
    /// </summary>
    internal const string WildcardSession = "\0wildcard";

    /// <summary>
    /// Session filter requested on this receiver link, or <c>null</c> when the link is not
    /// session-aware.  Equal to <see cref="WildcardSession"/> for wildcard receivers.
    /// Once a session is acquired this field holds the resolved session ID.
    /// </summary>
    private string? _sessionFilter;

    public OutgoingLinkEndpoint(string sourceAddress, ITopazLogger logger,
        ServiceBusRuleLoader? ruleLoader = null, string? sessionFilter = null)
    {
        _ruleLoader = ruleLoader;
        _sessionFilter = sessionFilter;
        // AMQP source addresses arrive with a leading '/' (e.g. "/filter-topic/Subscriptions/sub-true").
        // Strip it so the name matches what IncomingLinkEndpoint enqueues under.
        EntityAddress = sourceAddress.TrimStart('/');

        // Event Hub consumer links use the address "{hubName}/ConsumerGroups/{cg}/Partitions/{id}".
        // The producer enqueues messages under just "{hubName}", so we must look up messages using
        // only the hub name portion.
        MessageStoreAddress = ResolveMessageStoreAddress(EntityAddress);
        _logger = logger;
    }

    /// <summary>
    /// Returns the key under which messages are stored in <see cref="SubscriptionMessageStore"/>
    /// for this entity.  For Event Hub consumer links (address contains "/ConsumerGroups/") this
    /// is "{hubName}/Partitions/{partitionId}" to match how the producer enqueues; for all other
    /// entities it is the full entity address.
    /// </summary>
    private static string ResolveMessageStoreAddress(string entityAddress)
    {
        // Pattern: "{hubName}/ConsumerGroups/{consumerGroup}/Partitions/{partitionId}"
        // Producer sends to:  "{hubName}/Partitions/{partitionId}"
        // We must return "{hubName}/Partitions/{partitionId}" so TryDequeue finds the messages.
        var consumerGroupsIndex = entityAddress.IndexOf("/ConsumerGroups/", StringComparison.OrdinalIgnoreCase);
        if (consumerGroupsIndex > 0)
        {
            var hubName = entityAddress[..consumerGroupsIndex];
            var partitionsIndex = entityAddress.IndexOf("/Partitions/", consumerGroupsIndex, StringComparison.OrdinalIgnoreCase);
            if (partitionsIndex > 0)
            {
                var partitionId = entityAddress[(partitionsIndex + "/Partitions/".Length)..];
                return $"{hubName}/Partitions/{partitionId}";
            }
            return hubName;
        }
        return entityAddress;
    }

    // The Azure SDK (AmqpMessageConverter) reads LockTokenGuid from the AMQP Transfer delivery tag,
    // but only if it is exactly 16 bytes (GuidUtilities.TryParseGuidBytes requires length == 16).
    // AMQPNetLite's ListenerLink.SendMessage generates a 4-byte counter delivery tag, which the SDK
    // ignores, leaving LockTokenGuid as Guid.Empty and causing ThrowIfLockTokenIsEmpty to throw.
    // The fix bypasses the built-in SendMessage to supply a 16-byte GUID delivery tag via Session.SendDelivery.
    private static readonly Type DeliveryType =
        typeof(ListenerLink).Assembly.GetTypes().First(t => t.Name == "Delivery");

    private static readonly PropertyInfo DeliveryTagProperty =
        DeliveryType.GetProperty("Tag")!;

    private static readonly PropertyInfo DeliveryMessageProperty =
        DeliveryType.GetProperty("Message")!;

    private static readonly FieldInfo DeliveryBufferField =
        DeliveryType.GetField("Buffer")!;

    private static readonly FieldInfo DeliveryHandleField =
        DeliveryType.GetField("Handle")!;

    private static readonly FieldInfo DeliveryLinkField =
        DeliveryType.GetField("Link")!;

    private static readonly PropertyInfo DeliverySettledProperty =
        DeliveryType.GetProperty("Settled")!;

    private static readonly MethodInfo SessionSendDeliveryMethod =
        typeof(ListenerLink).Assembly.GetTypes().First(t => t.Name == "Session")
            .GetMethod("SendDelivery", BindingFlags.NonPublic | BindingFlags.Instance)!;

    // Protects _queueEndpoints, per-instance _pendingLink/_pendingCredit, and SubscriptionMessageStore.
    internal static readonly object DeliveryLock = new();

    // Registry of active OutgoingLinkEndpoint instances that serve real queue/topic addresses
    // (source address starts with "/"). Management links (e.g. "sbqueue/$management") are
    // excluded so their FLOW frames cannot hijack the shared delivery state.
    private static readonly List<OutgoingLinkEndpoint> _queueEndpoints = [];

    // Expose the AMQP entity address so DeliverMessages() can look up the per-entity queue.
    // Stored explicitly to satisfy the C# restriction that a primary constructor parameter
    // cannot both be captured in member bodies and used to initialize a field.
    internal readonly string EntityAddress;

    // The key used for SubscriptionMessageStore lookups.  For Event Hub consumer links this is
    // the hub name only (the producer enqueues under the hub name, not the full consumer path).
    internal readonly string MessageStoreAddress;

    // Per-instance state — was static, which caused FLOW from any receiver link (including
    // AMQP management response links) to overwrite the single shared _pendingLink and steal
    // all subsequent deliveries away from the real queue consumer.
    private ListenerLink? _pendingLink;
    private int _pendingCredit;

    // Called by IncomingLinkEndpoint after a message is added to the queue.
    internal static void NotifyMessageEnqueued()
    {
        DeliverMessages();
    }

    private static void DeliverMessages()
    {
        List<(ListenerLink Link, Message Message, Guid LockToken)> deliveries = [];

        lock (DeliveryLock)
        {
            foreach (var ep in _queueEndpoints)
            {
                while (ep._pendingCredit > 0 && ep._pendingLink != null)
                {
                    Message? message = null;

                    if (ep._sessionFilter != null)
                    {
                        // Session receiver: dequeue from the session-specific sub-queue.
                        if (!SessionMessageStore.TryDequeue(ep.MessageStoreAddress, ep._sessionFilter, out message) || message == null)
                            break;
                    }
                    else
                    {
                        if (!SubscriptionMessageStore.TryDequeue(ep.MessageStoreAddress, out message) || message == null)
                            break;
                    }

                    message.Header ??= new Header();
                    
                    // AMQPNetLite only writes a field into the encoded list when the backing store has been
                    // explicitly set via the property setter — it does NOT encode fields whose backing store
                    // is null even when the getter returns the default (0). Re-assigning DeliveryCount
                    // through the setter forces the value into the backing store so it appears as
                    // delivery-count=0 (0x43) in the binary. Microsoft.Azure.Amqp decodes that as
                    // Nullable<uint> with HasValue=true, which AmqpMessageToSBReceivedMessage requires 
                    // to populate ServiceBusReceivedMessage.DeliveryCount without throwing.
                    message.Header.DeliveryCount = message.Header.DeliveryCount;
                    message.MessageAnnotations[new Symbol("x-opt-offset")] = SubscriptionMessageStore.NextOffset().ToString();
                    message.MessageAnnotations[new Symbol("x-opt-locked-until")] = DateTime.UtcNow.AddMinutes(5);
                    ep._pendingCredit--;
                    var lockToken = Guid.NewGuid();
                    InFlightMessageStore.Track(lockToken, ep.MessageStoreAddress, message);
                    deliveries.Add((ep._pendingLink, message, lockToken));
                }
            }
        }

        // Send outside the lock to avoid holding it during network I/O.
        foreach (var (link, message, lockToken) in deliveries)
        {
            SendMessageWithGuidTag(link, message, lockToken);
        }
    }

    public override void OnFlow(FlowContext flowContext)
    {
        _logger.LogDebug(nameof(OutgoingLinkEndpoint), nameof(OnFlow), "There will be a maximum of {0} to process with {1} messages available.", flowContext.Messages, SubscriptionMessageStore.Count(MessageStoreAddress));
        if (flowContext.Link.Role) return;

        lock (DeliveryLock)
        {
            // Session receiver: enforce requiresSession and acquire the session lock.
            if (_sessionFilter != null)
            {
                // Enforce: non-session receiver on a requiresSession=true entity is rejected.
                // (This branch is for session receivers, so nothing to enforce here.)

                // Resolve wildcard session filter to the next available session.
                if (_sessionFilter == WildcardSession)
                {
                    var nextSession = SessionMessageStore.GetNextAvailableSession(MessageStoreAddress);
                    if (nextSession == null)
                    {
                        // No sessions available — close the link with SessionCannotBeLocked.
                        flowContext.Link.Close(TimeSpan.FromSeconds(5),
                            new Amqp.Framing.Error(new Symbol("com.microsoft:session-cannot-be-locked"))
                            {
                                Description = "No sessions available for entity."
                            });
                        return;
                    }
                    _sessionFilter = nextSession;
                }

                // Acquire the session lock.
                if (!SessionMessageStore.TryAcquireSessionLock(MessageStoreAddress, _sessionFilter, flowContext.Link))
                {
                    flowContext.Link.Close(TimeSpan.FromSeconds(5),
                        new Amqp.Framing.Error(new Symbol("com.microsoft:session-cannot-be-locked"))
                        {
                            Description = $"Session '{_sessionFilter}' is already locked by another receiver."
                        });
                    return;
                }

                // Release the session lock when this link closes.
                var capturedSession = _sessionFilter;
                flowContext.Link.Closed += (_, _) =>
                {
                    lock (DeliveryLock)
                    {
                        SessionMessageStore.ReleaseSessionLock(MessageStoreAddress, capturedSession);
                        _queueEndpoints.Remove(this);
                    }
                };
            }
            else
            {
                // Non-session receiver: enforce requiresSession flag.
                if (_ruleLoader != null && _ruleLoader.GetRequiresSession(MessageStoreAddress))
                {
                    flowContext.Link.Close(TimeSpan.FromSeconds(5),
                        new Amqp.Framing.Error(new Symbol("amqp:not-allowed"))
                        {
                            Description = "Entity requires a session-aware receiver (requiresSession = true)."
                        });
                    return;
                }
            }

            _pendingLink = flowContext.Link;
            _pendingCredit = flowContext.Messages;

            // Register this endpoint in the queue-delivery registry on first FLOW,
            // but only for real queue/topic addresses (not management links).
            // AMQP management links (e.g. "sbqueue/$management") send FLOW with
            // link-credit:50 at startup. Before this fix those flows overwrote the
            // single shared static _pendingLink, redirecting all subsequent deliveries
            // to the management response link instead of the queue consumer.
            // Note: regular queue addresses are plain names like "py-queue-test" (no
            // leading slash), so $management is the correct discriminator — NOT "/".
            if (!EntityAddress.Contains("$management", StringComparison.OrdinalIgnoreCase) && !_queueEndpoints.Contains(this))
            {
                // Ensure the per-entity queue slot exists so pre-consumer messages
                // accumulate here until the consumer attaches.
                SubscriptionMessageStore.EnsureQueue(MessageStoreAddress);
                _queueEndpoints.Add(this);
                if (_sessionFilter == null)
                {
                    // Non-session: remove from registry on close (session receivers handle this above).
                    flowContext.Link.Closed += (_, _) =>
                    {
                        lock (DeliveryLock) { _queueEndpoints.Remove(this); }
                    };
                }
            }
        }

        DeliverMessages();

        if (flowContext.Link.IsDraining)
        {
            _logger.LogDebug(nameof(OutgoingLinkEndpoint), nameof(OnFlow), "Completing draining.");
            flowContext.Link.CompleteDrain();
            _logger.LogDebug(nameof(OutgoingLinkEndpoint), nameof(OnFlow), "Draining complete.");
        }

        _logger.LogDebug(nameof(OutgoingLinkEndpoint), nameof(OnFlow), "Finished processing messages.");
    }

    public override void OnDisposition(DispositionContext dispositionContext)
    {
        var message = dispositionContext.Message;
        _logger.LogDebug(nameof(OutgoingLinkEndpoint), nameof(OnDisposition),
            "Disposition received: state={0}, messageNull={1}, entity={2}",
            dispositionContext.DeliveryState?.GetType().Name ?? "<null>", message == null, MessageStoreAddress);

        if (message != null)
        {
            switch (dispositionContext.DeliveryState)
            {
                case Accepted:
                    lock (DeliveryLock) { InFlightMessageStore.TryCompleteByMessage(message); }
                    break;

                case Released:
                    lock (DeliveryLock)
                    {
                        var maxDeliveryCount = _ruleLoader?.GetMaxDeliveryCount(MessageStoreAddress) ?? 10;
                        InFlightMessageStore.HandleAbandonByMessage(message, maxDeliveryCount);
                    }
                    break;

                // ponytail: Azure SDK sends Modified{DeliveryFailed=true} for AbandonMessageAsync, treat as Released
                case Modified:
                    lock (DeliveryLock)
                    {
                        var maxDeliveryCount = _ruleLoader?.GetMaxDeliveryCount(MessageStoreAddress) ?? 10;
                        InFlightMessageStore.HandleAbandonByMessage(message, maxDeliveryCount);
                    }
                    break;

                case Rejected rejected:
                    lock (DeliveryLock)
                    {
                        var reason = rejected.Error?.Condition?.ToString();
                        var description = rejected.Error?.Description;
                        InFlightMessageStore.HandleDeadLetterByMessage(message, reason, description);
                    }
                    break;
            }
        }

        dispositionContext.Complete();
    }

    private static void SendMessageWithGuidTag(ListenerLink link, Message message, Guid lockToken)
    {
        var buffer = message.Encode();
        var delivery = Activator.CreateInstance(DeliveryType)!;
        DeliveryTagProperty.SetValue(delivery, lockToken.ToByteArray());
        DeliveryMessageProperty.SetValue(delivery, message);
        DeliveryBufferField.SetValue(delivery, buffer);
        DeliveryHandleField.SetValue(delivery, link.Handle);
        DeliveryLinkField.SetValue(delivery, link);
        
        // Service Bus queue receivers operate in PeekLock mode and expect normal unsettled
        // transfers. Sender-settled deliveries consume the initial credit but do not appear to
        // trigger credit replenishment in the MassTransit/Azure Service Bus receive path, which
        // leaves the consumer stuck after the first message. Use unsettled transfers and let the
        // receiver settle explicitly.
        DeliverySettledProperty.SetValue(delivery, false);
        SessionSendDeliveryMethod.Invoke(link.Session, [delivery]);
    }
}