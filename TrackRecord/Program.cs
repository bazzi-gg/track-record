﻿// See https://aka.ms/new-console-template for more information
using Bazzigg.Database.Context;
using Kartrider.Api.AspNetCore;
using Kartrider.Api.Endpoints.MatchEndpoint.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Sentry;
using TrackRecord.HostedServices;
using TrackRecord.Services;
using TrackRecord.Services.Impl;

CreateHostBuilder(args).Build().Run();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddDbContextFactory<AppDbContext>(
                dbContextOptions => dbContextOptions.UseMySql(hostContext.Configuration.GetSection("ConnectionStrings")["App"], ServerVersion.Parse("10.5.6-MariaDB", ServerType.MariaDb))
#if DEBUG
                .EnableSensitiveDataLogging() // <-- These two calls are optional but help
                .EnableDetailedErrors()       // <-- with debugging (remove for production).
#endif
        );
            services.AddLogging();
            services.AddSingleton<IQueue<MatchDetail>, MatchDetailQueueService>();
            services.AddHostedService<CollectHostedService>();
            services.AddHostedService<TrackRecordHostedService>();
            services.AddKartriderApi(hostContext.Configuration["KartriderApiKey"]);
        })
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddEnvironmentVariables("APP_");
        })
        .ConfigureLogging((hostingContext, logging) =>
        {
            logging.AddConfiguration(hostingContext.Configuration);
            logging.AddSentry();
        });
