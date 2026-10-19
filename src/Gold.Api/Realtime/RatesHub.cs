using Gold.Application.Abstractions;
using Gold.Application.Rates;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Gold.Api.Realtime;

public interface IRatesClient
{
    Task RateChanged(RateDto rate);
}

/// <summary>Public, read-only hub: clients only listen for live 18K rate changes.</summary>
public sealed class RatesHub : Hub<IRatesClient>;

public sealed class SignalRRateNotifier(IHubContext<RatesHub, IRatesClient> hub, HybridCache cache) : IRateNotifier
{
    public const string RateTag = "rates";

    public async Task RateChangedAsync(RateDto rate, CancellationToken ct)
    {
        await cache.RemoveByTagAsync(RateTag, ct); // every price derived from the rate is now stale
        await hub.Clients.All.RateChanged(rate);
    }
}
