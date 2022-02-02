using Kartrider.Api;
using Kartrider.Api.Endpoints.MatchEndpoint.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrackRecord.Services;

namespace TrackRecord.HostedServices
{
    internal class CollectHostedService : BackgroundService
    {
        private readonly ILogger<CollectHostedService> _logger;
        private readonly IKartriderApi _kartriderApi;
        private readonly IQueue<MatchDetail> _queue;
        private readonly static string[] _matchTypes = new string[]
        {
            // 스피드 개인전
            "7b9f0fd5377c38514dbb78ebe63ac6c3b81009d5a31dd569d1cff8f005aa881a",
            // 스피드 클럽 레이싱
            "826ecdb309f3a2b80a790902d1b133499866d6b933c7deb0916979d1232f968c",
            // 스피드 팀전
            "effd66758144a29868663aa50e85d3d95c5bc0147d7fdb9802691c2087f3416e"
        };

        public CollectHostedService(ILogger<CollectHostedService> logger,
                                 IKartriderApi kartriderApi,
                                 IQueue<MatchDetail> queue)
        {
            _logger = logger;
            _kartriderApi = kartriderApi;
            _queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                AllMatches allMatches = await _kartriderApi.Match.GetAllMatchesAsync(null, null, 0, 200, _matchTypes).ConfigureAwait(false);
                IEnumerable<string> matchIds = allMatches.Matches.SelectMany(p => p.Value);
                foreach (string matchId in matchIds)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    MatchDetail matchDetail;
                    try
                    {
                        matchDetail = await _kartriderApi.Match.GetMatchDetailAsync(matchId).ConfigureAwait(false);
                    }
                    catch (KartriderApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(matchDetail.Channel) ||
                        string.IsNullOrEmpty(matchDetail.TrackId) ||
                        matchDetail.Channel.Contains("Newbie"))
                    {
                        continue;
                    }
                    _queue.Enqueue(matchDetail);
                }
                await Task.Delay(1000, stoppingToken);
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
