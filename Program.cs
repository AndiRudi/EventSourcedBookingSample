using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Grpc.Core;

namespace EventSourcedBookingSample
{
    class Program
    {
        const string slotId = "fe1c31ed-b3e0-46d0-936f-b7c6757df592";

        static List<Booking> bookings = new List<Booking>();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting");

            //https://developers.eventstore.com/clients/grpc/getting-started/connecting.html#required-packages

            var settings = EventStoreClientSettings
                .Create("esdb://admin:changeit@localhost:2113");

            settings.ConnectivitySettings.Insecure = true;
            var client = new EventStoreClient(settings);

            Console.WriteLine("Connected. Appending Events");

            await StartProjection(client);

            char selection = '.';
            while (selection != 'q')
            {
                Console.WriteLine("1 = Get Bookings");
                Console.WriteLine("2 = Add Bookings");
                Console.WriteLine("q = Quit");

                selection = Console.ReadKey().KeyChar;
                switch (selection)
                {
                    case '1': await GetBookings(client); break;
                    case '2': await CreateBooking(client); break;
                }
            }

            Console.WriteLine("End");
        }

        static async Task StartProjection(EventStoreClient client)
        {
            await client.SubscribeToAllAsync(
                start: Position.Start,
                eventAppeared: BookingProcessorAsync
            );
        }

        static async Task BookingProcessorAsync(StreamSubscription _, ResolvedEvent resolvedEvent, CancellationToken c)
        {
            if (resolvedEvent.Event.EventType == "Booking.Created")
            {
                Console.WriteLine("Projection Booking.Created");

                var bookingCreated = JsonSerializer.Deserialize<BookingCreated>(resolvedEvent.Event.Data.ToArray());
                bookings.Add(new Booking
                {
                    UserId = bookingCreated.UserId,
                    SlotId = bookingCreated.SlotId
                });
            }

            /*  if (resolvedEvent.Event.EventType == "Booking.Cancelled")
             {

             } */
        }


        static async Task CreateBooking(EventStoreClient client)
        {
            var booking = new BookingCreated
            {
                UserId = Guid.NewGuid(),
                SlotId = Guid.Parse(slotId)
            };
            const string metadata = "{}";

            var eventData = new EventData(
                eventId: Uuid.NewUuid(),
                type: "Booking.Created",
                data: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(booking)),
                metadata: Encoding.UTF8.GetBytes(metadata)
            );

            var appendResult = await client.AppendToStreamAsync(
                streamName: "Booking",
                expectedState: StreamState.Any,
                eventData: new[] { eventData }
            );

            Console.WriteLine("Booking added");
        }

        static async Task GetBookings(EventStoreClient client)
        {
            foreach (var booking in bookings)
            {
                Console.WriteLine($"Booking {booking.Id} {booking.UserId} {booking.SlotId}");
            }
        }


        static async Task GetBookingsWithDirectStreamRead(EventStoreClient client)
        {
            var result = client.ReadStreamAsync(
                direction: Direction.Forwards,
                streamName: "Booking",
                revision: StreamPosition.Start
            );

            var readState = await result.ReadState;
            Console.WriteLine("$ReadState: {readState}");

            foreach (var evt in await result.ToListAsync())
            {
                var booking = JsonSerializer.Deserialize<BookingCreated>(evt.Event.Data.ToArray());
                Console.WriteLine($"{evt.Event.EventType} Booking {booking.Id} {booking.UserId} {booking.SlotId}");
            }
        }

    }
}
