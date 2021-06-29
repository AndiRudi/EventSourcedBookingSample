using System;

namespace EventSourcedBookingSample
{
    public class Booking
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        public Guid SlotId { get; set; }

    }
}