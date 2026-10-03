using System;
using System.Collections.Generic;

namespace NewAster.Core
{
    [Serializable]
    public sealed class TrialRunEvent
    {
        public int schemaVersion = 1;
        public string runId, sessionId, definitionVersion, buildHash, inputMethod, resourceHash, definitionHash;
        public string eventId, operationId, battleId, category, action, detail, utc;
        public int seed;
        public long sequence, timeTick;
        public double elapsedSeconds, activeSeconds, presentationSeconds;
    }

    // Observers only: this type has no game state, random generator or save writer.
    public sealed class TrialRunTelemetry
    {
        private readonly Func<double> clock;
        private readonly Func<DateTime> utc;
        private readonly Action<TrialRunEvent> sink;
        private readonly HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        private readonly string run, session, definition, build, input;
        private readonly string resources, definitions;
        private readonly double origin;
        private double previous, active;
        private bool inactive;
        private long sequence;
        public bool Incomplete { get; private set; }
        public long RecordedCount { get; private set; }
        public void MarkIncomplete() { Incomplete = true; }

        public TrialRunTelemetry(string runId, string sessionId, string definitionVersion, string buildHash,
            string inputMethod, Func<double> monotonicSeconds, Func<DateTime> utcNow, Action<TrialRunEvent> write,
            string resourceAssetsHash = "unrecorded", string definitionContentHash = "unrecorded")
        {
            if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(sessionId) ||
                string.IsNullOrWhiteSpace(definitionVersion) || string.IsNullOrWhiteSpace(buildHash) ||
                string.IsNullOrWhiteSpace(inputMethod)) throw new ArgumentException("Missing run identity.");
            clock = monotonicSeconds ?? throw new ArgumentNullException(nameof(monotonicSeconds));
            utc = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            sink = write ?? throw new ArgumentNullException(nameof(write));
            run = runId; session = sessionId; definition = definitionVersion; build = buildHash; input = inputMethod;
            resources=resourceAssetsHash;definitions=definitionContentHash;
            origin = previous = clock();
            if (double.IsNaN(origin) || double.IsInfinity(origin)) throw new ArgumentException("Invalid monotonic clock.");
        }

        private double Sample()
        {
            double now = clock();
            if (double.IsNaN(now) || double.IsInfinity(now) || now < previous) throw new InvalidOperationException("Invalid monotonic clock.");
            if (!inactive) active += now - previous;
            previous = now;
            return now;
        }

        public void SetInactive(bool value)
        {
            try { Sample(); inactive = value; }
            catch { Incomplete = true; }
        }

        public void Record(string eventId, string category, string action, string detail = "",
            string battleId = "", int seed = 0, string operationId = "", long timeTick = 0, double presentationSeconds = 0)
        {
            // Even serializer, clock and disk failures cannot escape into gameplay.
            try {
                if (string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(action) ||
                    presentationSeconds < 0 || double.IsNaN(presentationSeconds) || double.IsInfinity(presentationSeconds))
                    throw new ArgumentException("Invalid telemetry event.");
                if (!ids.Add(eventId)) return;
                double now = Sample();
                var row = new TrialRunEvent {
                    runId = run, sessionId = session, definitionVersion = definition, buildHash = build, inputMethod = input,
                    resourceHash=resources, definitionHash=definitions,
                    eventId = eventId, operationId = operationId, battleId = battleId, seed = seed,
                    category = category, action = action, detail = detail, utc = utc().ToUniversalTime().ToString("O"),
                    sequence = ++sequence, timeTick = timeTick, elapsedSeconds = now - origin,
                    activeSeconds = active, presentationSeconds = presentationSeconds
                };
                sink(row); RecordedCount++;
            } catch { Incomplete = true; }
        }
    }
}
