namespace Ais.Net.Models.Json.Nats.Specs;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Ais.Net.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NATS.Client.Core;

using Shouldly;

using Testcontainers.Nats;

/// <summary>
/// End-to-end tests that publish and subscribe through a real NATS server running in a container,
/// proving the serializer works over the actual wire (connection framing, buffer handling and the
/// polymorphic round-trip) rather than only against hand-built buffers.
/// </summary>
/// <remarks>
/// Requires a Docker daemon. When Docker is unavailable the tests report inconclusive (skipped)
/// rather than failing, so the suite still passes on agents without Docker.
/// </remarks>
[TestClass]
[TestCategory("Integration")]
public class NatsContainerIntegrationTests
{
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(15);

    private static NatsContainer? container;
    private static string? skipReason;

    // GitHub Actions (and most CI systems) set CI=true. In CI the container must start; on a dev
    // machine without Docker the tests skip instead.
    private static bool IsContinuousIntegration =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        try
        {
            container = new NatsBuilder("nats:2.10").Build();
            await container.StartAsync();
        }
        catch (Exception ex) // TEMP: always catch so Diagnostics_TEMP can report the reason.
        {
            skipReason = $"{ex.GetType().Name}: {ex.Message}";
            container = null;
        }
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    [TestMethod]
    public void Diagnostics_TEMP() => Assert.Fail(
        $"IsCI={IsContinuousIntegration} " +
        $"CI='{Environment.GetEnvironmentVariable("CI")}' " +
        $"GITHUB_ACTIONS='{Environment.GetEnvironmentVariable("GITHUB_ACTIONS")}' " +
        $"DOCKER_HOST='{Environment.GetEnvironmentVariable("DOCKER_HOST")}' " +
        $"container={(container is null ? "NULL" : container.GetConnectionString())} " +
        $"skipReason='{skipReason}'");

    [TestMethod]
    public async Task AllSevenMessageTypesRoundTripOverNatsViaTheRegistry()
    {
        // The registry is configured on the connection, so no serializer is passed per call.
        await using NatsConnection nats = await ConnectAsync(useRegistry: true);
        const string subject = "ais.integration.registry";

        await using INatsSub<AisMessageBase> subscription =
            await nats.SubscribeCoreAsync<AisMessageBase>(subject);

        AisMessageBase[] sent = TestMessages.All;
        foreach (AisMessageBase message in sent)
        {
            await nats.PublishAsync(subject, message);
        }

        // Round-trip a PING to force all queued PUBs onto the wire before we wait for delivery.
        await nats.PingAsync();

        IReadOnlyList<AisMessageBase> received = await ReceiveAsync(subscription, sent.Length);

        // A single subject from a single connection preserves publish order.
        received.Count.ShouldBe(sent.Length);
        for (int i = 0; i < sent.Length; i++)
        {
            received[i].ShouldBe(sent[i]);
            received[i].GetType().ShouldBe(sent[i].GetType());
        }
    }

    [TestMethod]
    public async Task RoundTripsViaAnExplicitPerCallSerializer()
    {
        // No registry on the connection; the serializer is supplied on each publish/subscribe.
        await using NatsConnection nats = await ConnectAsync(useRegistry: false);
        const string subject = "ais.integration.percall";

        await using INatsSub<AisMessageBase> subscription = await nats.SubscribeCoreAsync<AisMessageBase>(
            subject, serializer: AisMessageNatsSerializer.Default);

        AisMessageBase sent = TestMessages.CreateType24Part1();
        await nats.PublishAsync(subject, sent, serializer: AisMessageNatsSerializer.Default);
        // Round-trip a PING to force all queued PUBs onto the wire before we wait for delivery.
        await nats.PingAsync();

        IReadOnlyList<AisMessageBase> received = await ReceiveAsync(subscription, 1);

        received.ShouldHaveSingleItem().ShouldBe(sent);
        received[0].ShouldBeOfType<AisMessageType24Part1>();
    }

    private static async Task<NatsConnection> ConnectAsync(bool useRegistry)
    {
        if (container is null)
        {
            Assert.Inconclusive(skipReason ?? "NATS container is not available.");
        }

        NatsOpts opts = useRegistry
            ? new NatsOpts
            {
                Url = container!.GetConnectionString(),
                SerializerRegistry = AisMessageNatsSerializerRegistry.Default,
            }
            : new NatsOpts { Url = container!.GetConnectionString() };

        NatsConnection nats = new(opts);
        await nats.ConnectAsync();
        return nats;
    }

    private static async Task<IReadOnlyList<AisMessageBase>> ReceiveAsync(
        INatsSub<AisMessageBase> subscription, int count)
    {
        List<AisMessageBase> received = new(count);
        using CancellationTokenSource cts = new(ReceiveTimeout);

        try
        {
            await foreach (NatsMsg<AisMessageBase> msg in subscription.Msgs.ReadAllAsync(cts.Token))
            {
                received.Add(msg.Data.ShouldNotBeNull());
                if (received.Count == count)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Expected {count} message(s) within {ReceiveTimeout.TotalSeconds:0}s but received {received.Count}.");
        }

        return received;
    }
}
