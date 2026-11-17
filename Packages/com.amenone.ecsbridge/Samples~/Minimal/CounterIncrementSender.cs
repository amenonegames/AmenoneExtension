namespace Amenone.EcsBridge.Samples.Minimal
{
    public sealed class CounterIncrementSender
    {
        private readonly DefaultWorldSingleton<CounterIncrementRequest> _request = new();

        public void Send(int amount)
        {
            var request = _request.Read();
            request.Amount += amount;
            _request.Write(request);
        }
    }
}
