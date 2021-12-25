using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core.Test
{
    public class SimulationRequest
    {
        public string RequestId { get; set; }

        public string RequestResource { get; set; }

        public Dictionary<string, string> Parameters { get; set; }
    }
}