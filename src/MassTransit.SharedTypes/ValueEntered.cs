namespace MassTransit.SharedTypes
{
    public record ValueEntered
    {
        public readonly string Value;

        public ValueEntered(string value)
        {
            this.Value = value;
        }
    }
}