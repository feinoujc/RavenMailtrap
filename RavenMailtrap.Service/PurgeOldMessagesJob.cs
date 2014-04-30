using System;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Quartz;
using Raven.Abstractions.Data;
using Raven.Client.Indexes;

namespace RavenMailtrap
{
    public class PurgeOldMessagesJob : IJob
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private async Task PurgeAllOldEmailAsync()
        {
            try
            {
                const string indexName = "WeekOldEmail";


                RavenDB.DocumentStore.DatabaseCommands.PutIndex(indexName,
                    new IndexDefinitionBuilder<Message>
                    {
                        Map = documents => documents.Where(m => m.ReceivedDate.AddDays(7) <= DateTime.Today)
                            .Select(entity => new {})
                    }, overwrite: true);

                //let the index (re)build
                await Task.Delay(TimeSpan.FromSeconds(30));
                RavenDB.DocumentStore.DatabaseCommands.DeleteByIndex(indexName,
                    new IndexQuery());
            }
            catch (Exception e)
            {
                Log.LogException(LogLevel.Error, "Could not delete week old email.", e);
            }
        }

        public async void Execute(IJobExecutionContext context)
        {
            await PurgeAllOldEmailAsync();
        }
    }
}