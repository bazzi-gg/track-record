using Kartrider.Api.Endpoints.MatchEndpoint.Models;

namespace TrackRecord.Services.Impl
{
    internal class MatchDetailQueueService : IQueue<MatchDetail>
    {
        private readonly Queue<MatchDetail> _queue = new();
        public void Enqueue(MatchDetail item) => _queue.Enqueue(item);
        public MatchDetail Dequeue() => _queue.Dequeue();
        public bool IsEmpty => _queue.Count == 0;
    }
}
