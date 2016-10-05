using System.Diagnostics;

namespace ConquerButler
{
    public class Clock
    {
        private readonly long frequency;
        private long lastFrame;
        private long initialTick;

        private long tickCount;

        private long fixedTickCount;

        public long Tick => tickCount;
        public long FixedTick => fixedTickCount;

        public Clock()
        {
            frequency = Stopwatch.Frequency;
        }

        public void Start()
        {
            initialTick = Stopwatch.GetTimestamp();
        }

        public void FixedUpdate()
        {
            fixedTickCount++;
        }

        public float Frame()
        {
            long tick = Stopwatch.GetTimestamp();

            float elapsed = ((float)(tick - lastFrame)) / frequency;
            lastFrame = tick;

            tickCount++;

            return elapsed;
        }

        public float TotalTime()
        {
            long tick = Stopwatch.GetTimestamp();
            return ((float)(tick - initialTick)) / frequency;
        }
    }
}