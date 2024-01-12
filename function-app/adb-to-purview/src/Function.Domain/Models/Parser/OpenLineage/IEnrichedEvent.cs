namespace Function.Domain.Models.OL
{
    public interface IEnrichedEvent
    {
        public Event? OlEvent { get; }
    }
}