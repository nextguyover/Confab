using System.Text;
using System.Text.Json;

namespace Confab.Tests.Helpers
{
    public class HttpHelpers
    {
        public static StringContent JsonSerialize<a>(a value)
        {
            return new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
        }
    }
}
