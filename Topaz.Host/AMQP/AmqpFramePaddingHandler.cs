using Amqp.Framing;
using Amqp.Handler;
using Amqp.Types;

namespace Topaz.Host.AMQP;

/// <summary>
/// AMQPNetLite 2.5.1 omits trailing null fields from AMQP performative frames.
/// Python's azure-eventhub uses positional tuple access (e.g. frame[9], frame[11], frame[13])
/// which causes IndexError when the list is shorter than expected.
///
/// This handler intercepts outgoing Open, Begin, and Attach frames and ensures they are padded
/// to include all required fields (even as empty/null values), making them compatible
/// with the Python azure-eventhub pyamqp transport decoder.
/// </summary>
internal sealed class AmqpFramePaddingHandler : IHandler
{
    public static readonly AmqpFramePaddingHandler Instance = new();

    public bool CanHandle(EventId id) =>
        id == EventId.ConnectionLocalOpen || id == EventId.SessionLocalOpen || id == EventId.LinkLocalOpen;

    public void Handle(Event protocolEvent)
    {
        try
        {
            switch (protocolEvent.Id)
            {
                case EventId.ConnectionLocalOpen:
                    // Ensure the Open frame has all 10 fields so Python's frame[4] and frame[9] succeed.
                    if (protocolEvent.Context is Open open)
                    {
                        Console.Error.WriteLine($"[PADDING] Open frame intercepted");
                        if (open.Properties == null)
                        {
                            open.Properties = new Fields();
                            Console.Error.WriteLine($"[PADDING] Open frame Properties set to empty Fields");
                        }
                    }
                    break;

                case EventId.SessionLocalOpen:
                    // Ensure the Begin frame has all 8 fields so Python's frame[7] (properties) succeeds.
                    if (protocolEvent.Context is Begin begin)
                    {
                        Console.Error.WriteLine($"[PADDING] Begin frame intercepted");
                        if (begin.Properties == null)
                        {
                            begin.Properties = new Fields();
                            Console.Error.WriteLine($"[PADDING] Begin frame Properties set to empty Fields");
                        }
                    }
                    break;

                case EventId.LinkLocalOpen:
                    // Ensure the Attach frame has all 14 fields so Python's frame[11] and frame[13] succeed.
                    if (protocolEvent.Context is Attach attach)
                    {
                        Console.Error.WriteLine($"[PADDING] Attach frame intercepted");
                        if (attach.Properties == null)
                        {
                            attach.Properties = new Fields();
                            Console.Error.WriteLine($"[PADDING] Attach frame Properties set to empty Fields");
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[PADDING] Exception in handler: {ex.Message}");
        }
    }
}
