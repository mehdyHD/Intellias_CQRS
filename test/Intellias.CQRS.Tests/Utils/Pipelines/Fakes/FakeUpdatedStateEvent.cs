using Intellias.CQRS.Core.Events;

namespace Intellias.CQRS.Tests.Utils.Pipelines.Fakes
{
    public class FakeUpdatedStateEvent : Event
    {
        public string Data { get; set; }
    }
}