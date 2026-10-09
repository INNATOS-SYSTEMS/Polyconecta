using System;

namespace Contpaq.Bridge.Infrastructure.Sdk
{
    public class CircuitBreakerPolicy
    {
        private readonly int _threshold;
        private readonly TimeSpan _cooldown;
        private int _consecutiveFailures;
        private DateTime? _circuitOpenedAt;

        public string State { get; private set; } = "CLOSED"; // CLOSED, OPEN, HALF_OPEN

        public CircuitBreakerPolicy(int threshold = 3, int cooldownSeconds = 15)
        {
            _threshold = threshold;
            _cooldown = TimeSpan.FromSeconds(cooldownSeconds);
        }

        public bool AllowExecution()
        {
            if (State == "CLOSED") return true;

            if (State == "OPEN")
            {
                if (_circuitOpenedAt.HasValue && DateTime.UtcNow - _circuitOpenedAt.Value > _cooldown)
                {
                    State = "HALF_OPEN";
                    return true;
                }
                return false;
            }

            if (State == "HALF_OPEN")
            {
                return true;
            }

            return true;
        }

        public void RecordSuccess()
        {
            _consecutiveFailures = 0;
            _circuitOpenedAt = null;
            State = "CLOSED";
        }

        public void RecordFailure()
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= _threshold)
            {
                State = "OPEN";
                _circuitOpenedAt = DateTime.UtcNow;
            }
        }
    }
}
