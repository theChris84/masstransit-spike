using System.Text.RegularExpressions;

namespace MassTransit.SharedTypes
{
    public record SomeValue
    {

        public string Payload { get; init; }
        public string RoutingKey => 
            Regex
                .Match(
                Payload, 
                @"^\w*.(?<payload>\w*.\w*)",
                RegexOptions.Compiled)
                .Groups["payload"]
                .Value;
    };

}