using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;
using Grpc.Core;

namespace EventSourcedBookingSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting");

            //https://developers.eventstore.com/clients/grpc/getting-started/connecting.html#required-packages

            var settings = EventStoreClientSettings
                .Create("esdb://admin:changeit@localhost:2113");

            settings.ConnectivitySettings.Insecure = true;
            var client = new EventStoreClient(settings);

            Console.WriteLine("Connected. Appending Events");

            const string streamName = "newstream";
            const string eventType = "event-type";
            const string data = "{ \"a\":\"2\"}";
            const string metadata = "{}";

            var eventData = new EventData(
                eventId: Uuid.NewUuid(),
                type: eventType,
                data: Encoding.UTF8.GetBytes(data),
                metadata: Encoding.UTF8.GetBytes(metadata)
            );

            var appendResult = await client.AppendToStreamAsync(
                streamName: streamName,
                expectedState: StreamState.Any,
                eventData: new[] { eventData }
            );

            Console.WriteLine("Event added");

            var result = client.ReadStreamAsync(
                direction: Direction.Forwards,
                streamName: streamName,
                revision: StreamPosition.Start,
                maxCount: 1
            );

            var readState = await result.ReadState;
            Console.WriteLine("$ReadState: {readState}");
            
            foreach (var evt in await result.ToListAsync())
            {
                Console.WriteLine(Encoding.UTF8.GetString(evt.Event.Data.ToArray()));
            }

            Console.WriteLine("Done");
            Console.ReadKey();
        }
    }
}
