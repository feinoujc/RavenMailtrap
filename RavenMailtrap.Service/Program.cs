﻿using NLog;
using NLog.Config;
using NLog.Targets;
using Topshelf;

namespace RavenMailtrap.Service
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
                    host.Service<SmtpService>(service =>
                        {
                            service.ConstructUsing(name => new SmtpService());
                            service.WhenStarted(tc => tc.Start());
                            service.WhenStopped(tc => tc.Stop());
                            service.WhenPaused(tc => tc.Stop());
                            service.WhenContinued(tc => tc.Start());
                        });
                    host.RunAsLocalSystem();
                    host.SetDescription("Raven Mailtrap Smtp Server");
                    host.SetDisplayName("MailtrapSmtpServer");
                    host.SetServiceName("MailtrapSmtpServer");
                    host.EnableServiceRecovery(recovery => recovery.RestartService(1));
                    host.StartAutomaticallyDelayed();
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
            fileTarget.FileName = "${basedir}/log.txt";
            fileTarget.Layout = layout;

            // Step 4. Define rules
            var rule1 = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(rule1);

            var rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
            config.LoggingRules.Add(rule2);
            return config;
        }
    }
}