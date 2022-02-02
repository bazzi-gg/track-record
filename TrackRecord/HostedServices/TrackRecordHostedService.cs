using Bazzigg.Database.Context;
using Kartrider.Api.Endpoints.MatchEndpoint.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrackRecord.Services;

namespace TrackRecord.HostedServices
{
    internal class TrackRecordHostedService : BackgroundService
    {
        private readonly ILogger<TrackRecordHostedService> _logger;
        private readonly IQueue<MatchDetail> _queue;
        private readonly IDbContextFactory<AppDbContext> _appDbContext;

        public TrackRecordHostedService(ILogger<TrackRecordHostedService> logger,
                                 IQueue<MatchDetail> queue,
                                 IDbContextFactory<AppDbContext> appDbContext)
        {
            _logger = logger;
            _queue = queue;
            _appDbContext = appDbContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_queue.IsEmpty)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }
                MatchDetail matchDetail = _queue.Dequeue();
                using AppDbContext context = _appDbContext.CreateDbContext();
                var trackRecord = context.TrackRecord.FirstOrDefault(p => p.Channel == matchDetail.Channel && p.TrackId == matchDetail.TrackId);
                EntityState entityState = trackRecord == null ? EntityState.Added : EntityState.Modified;
                if (trackRecord == null)
                {
                    trackRecord = new Bazzigg.Database.Entity.TrackRecord()
                    {
                        Channel = matchDetail.Channel,
                        TrackId = matchDetail.TrackId,
                        Records = new List<double>()
                    };
                }
                foreach (Player player in matchDetail.Players)
                {
                    if (player.Retired || player.Rank < 0 || player.Record == TimeSpan.Zero)
                    {
                        continue;
                    }
                    trackRecord.Records.Add(player.Record.TotalSeconds);
                }
                if (trackRecord.Records.Count == 0)
                {
                    continue;
                }
                // 중복 삭제
                trackRecord.Records = trackRecord.Records.Distinct().ToList();
                trackRecord.Records.Sort();
                trackRecord.LastUpdated = DateTime.Now;
                context.Entry(trackRecord).State = entityState;
                await context.SaveChangesAsync(stoppingToken);
            }
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting up");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping");
            return base.StopAsync(cancellationToken);
        }
    }
}
