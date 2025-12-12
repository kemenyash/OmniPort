namespace OmniPort.Core.Models
{
    public class LazyStream
    {
        private readonly Func<Task<Stream>> factory;
        public LazyStream(Func<Task<Stream>> factory) => this.factory = factory;
        public async Task<Stream> OpenAsync() => await factory();
    }
}
