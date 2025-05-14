using System.Text;
using Amqp;
using Amqp.Sasl;
using Amqp.Types;

namespace Topaz.Host;

public class TopazSaslProfile(Symbol mechanism) : SaslProfile(mechanism)
{
    protected override ITransport UpgradeTransport(ITransport transport)
    {
        return transport;
    }

    protected override DescribedList GetStartCommand(string hostname)
    {
        return new SaslInit()
        {
            Mechanism = this.Mechanism,
            InitialResponse = Encoding.UTF8.GetBytes("hello"),
        };
    }

    protected override DescribedList OnCommand(DescribedList command)
    {
        if (command.Descriptor.Code == 0x0000000000000041)
        {
            return new SaslOutcome() { Code = SaslCode.Ok };
        }
        else if (command.Descriptor.Code == 0x0000000000000040)
        {
            return null;
        }

        throw new AmqpException(ErrorCode.NotAllowed, command.ToString());
    }
}