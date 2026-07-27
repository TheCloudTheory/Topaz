using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.Redis.Commands;

public sealed class GenericRedisCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("redis", redis =>
        {
            redis.AddCommand<CreateRedisCommand>("create");
            redis.AddCommand<GetRedisCommand>("show");
            redis.AddCommand<DeleteRedisCommand>("delete");
            redis.AddCommand<UpdateRedisCommand>("update");
            redis.AddCommand<ListRedisCommand>("list");
            redis.AddCommand<ListRedisKeysCommand>("list-keys");
            redis.AddCommand<RegenerateRedisKeyCommand>("regenerate-key");
        });
    }
}
