namespace OmniPort.Core.Models
{
    public class LazyStream
    {
        private readonly Func<Task<Stream>> _factory;
        public LazyStream(Func<Task<Stream>> factory) => _factory = factory;
        public async Task<Stream> OpenAsync() => await _factory();
    }
}
