using System.Text.Json;
using EventStore.Client;

string connectionString = "esdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false";
EventStoreClientSettings settings = EventStoreClientSettings.Create(connectionString);
EventStoreClient client = new EventStoreClient(settings);

OrderPlacedEvent orderPlacedEvent = new()
{
    OrderId = 1,
    TotalAmount = 300
};

EventData eventData = new EventData(
    eventId: Uuid.NewUuid(),
    type: orderPlacedEvent.GetType().Name,
    data: JsonSerializer.SerializeToUtf8Bytes(orderPlacedEvent)
    );

string streamName = "order-stream";

//Eventi db ye yazıyoruz
await client.AppendToStreamAsync(
    streamName: streamName,
    expectedState: StreamState.Any,
    eventData: new[] { eventData }
    );

//Evemtleri db den çekiyoruz
var events = client.ReadStreamAsync(streamName: streamName, direction: Direction.Forwards, revision: StreamPosition.Start);
var datas = events.ToListAsync();

//Eventlere Subscribe oluyoruz
await client.SubscribeToStreamAsync(
    streamName: streamName,
    start: FromStream.Start,
    //Eventler dinleniyor
    eventAppeared: async (stremSubscription, resolvedEvent, cancellationToken) =>
    {
        OrderPlacedEvent @event = JsonSerializer.Deserialize<OrderPlacedEvent>(resolvedEvent.Event.Data.ToArray());
        await Console.Out.WriteAsync(JsonSerializer.Serialize(@event));
    },
    //Hata alınıyor ise
    subscriptionDropped : async (stremSubscription, subscriptionDroppedReason, exception) =>
    {

    });

Console.Read();

class OrderPlacedEvent
{
    public int OrderId { get; set; }
    public int TotalAmount { get; set; }
}
