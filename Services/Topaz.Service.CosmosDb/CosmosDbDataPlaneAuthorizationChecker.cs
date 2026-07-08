using Topaz.EventPipeline;
using Topaz.Service.Authorization;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb;

internal sealed class CosmosDbDataPlaneAuthorizationChecker(Pipeline eventPipeline, ITopazLogger logger) : DataPlaneAuthorizationChecker(eventPipeline, logger);