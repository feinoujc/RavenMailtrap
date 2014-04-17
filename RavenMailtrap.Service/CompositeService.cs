using Raven.Abstractions.Extensions;

namespace RavenMailtrap.Service
{
    public class CompositeService : IStartAndStop
    {
        private readonly IStartAndStop[] _services;

        public CompositeService(params IStartAndStop[] services)
        {
            _services = services;
        }

        public void Start()
        {
            _services.ForEach(x=>x.Start());
        }

        public void Stop()
        {
            _services.ForEach(x => x.Stop());
        }
    }
}