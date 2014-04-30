using NLog;
using NLog.Config;
using NLog.Targets;
using Quartz;
using RavenMailtrap.Service;
using Topshelf;
using Topshelf.Quartz;

namespace RavenMailtrap
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            LogManager.Configuration = GetDefaultLoggingConfiguration();
            HostFactory.Run(host =>
            {
                host.Service<CompositeService>(service =>
                {
                    service.ConstructUsing(name => new CompositeService(new MessagesApi(), new SmtpService()));
                    service.WhenStarted(tc => tc.Start());
                    service.WhenStopped(tc => tc.Stop());
                    service.WhenPaused(tc => tc.Stop());
                    service.WhenContinued(tc => tc.Start());
                    service.ScheduleQuartzJob(q =>
                        q.WithJob(() =>
                            JobBuilder.Create<PurgeOldMessagesJob>().Build())
                            .AddTrigger(() =>
                                TriggerBuilder.Create()
                                    .WithSimpleSchedule(builder => builder
                                        .WithIntervalInHours(4)
                                        .RepeatForever())
                                    .Build())
                        );
                });

                host.RunAsLocalSystem();
                host.SetDescription("Raven Mailtrap Smtp Server and API");
                host.SetDisplayName("ravenmailtrap");
                host.SetServiceName("ravenmailtrap");
#if TOPSHELF3
                host.EnableServiceRecovery(recovery => recovery.RestartService(1));
                host.StartAutomaticallyDelayed();
#else
                    host.StartAutomatically();
#endif
            });
        }


        private static LoggingConfiguration GetDefaultLoggingConfiguration()
        {
            // Step 1. Create configuration object 
            var config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration 
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);

            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            const string layout = @"${date:format=HH\:MM\:ss} ${logger} ${message}";
            // Step 3. Set target properties 
            consoleTarget.Layout = layout;
            fileTarget.FileName = "${basedir}/mailtrap.log";
            fileTarget.Layout = layout;
            fileTarget.ArchiveFileName = "${basedir}/archives/log.{#####}.txt";
            fileTarget.MaxArchiveFiles = 3;
            fileTarget.ArchiveAboveSize = 10240;
            fileTarget.ArchiveNumbering = ArchiveNumberingMode.Sequence;

            // Step 4. Define rules
            var rule1 = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(rule1);

            var rule2 = new LoggingRule("*", LogLevel.Info, fileTarget);
            config.LoggingRules.Add(rule2);
            return config;
        }
    }
}