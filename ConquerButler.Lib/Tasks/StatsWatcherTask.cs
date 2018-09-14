using log4net;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Linq;

namespace ConquerButler.Tasks
{
    public class StatsWatcherTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(StatsWatcherTask));

        public static string TASK_TYPE_NAME = "StatsWatcher";

        public int Health { get; protected set; }
        public int Mana { get; protected set; }

        public override string ResultDisplayInfo { get { return $"{Health}|{Mana}"; } }

        public StatsWatcherTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {

            Health = -1;
            Mana = -1;

            Interval = 1000;
        }

        private const int healthX = 39;
        private const int manaX = 52;
        private const int minY = 689;
        private const int maxY = 756;

        public static bool IsRed(Color color)
        {
            return Math.Abs(136 - color.R) <= 110 &&
                Math.Abs(12 - color.G) <= 50 &&
                Math.Abs(12 - color.B) <= 50;
        }

        public static bool IsBlue(Color color)
        {
            return Math.Abs(5 - color.R) <= 50 &&
                Math.Abs(5 - color.G) <= 50 &&
                Math.Abs(111 - color.B) <= 110;
        }

        public int GetHealth(Bitmap screenshot)
        {
            var pixel = screenshot.GetPixel(healthX, minY - 1);

            if (IsRed(pixel))
            {
                return 100;
            }
            else
            {
                for (int y = minY; y < maxY; y++)
                {
                    pixel = screenshot.GetPixel(healthX, y);

                    if (IsRed(pixel))
                    {
                        return (int)((float)(maxY - y) / (float)(maxY - minY) * 90);
                    }
                }

                return -1;
            }
        }

        public int GetMana(Bitmap screenshot)
        {
            var pixel = screenshot.GetPixel(manaX, minY - 1);

            if (IsBlue(pixel))
            {
                return 100;
            }
            else
            {
                for (int y = minY; y < maxY; y++)
                {
                    pixel = screenshot.GetPixel(manaX, y);

                    if (IsBlue(pixel))
                    {
                        return (int)((float)(maxY - y) / (float)(maxY - minY) * 90);
                    }
                }

                return -1;
            }
        }

        protected override Task DoTick()
        {
            int newHealth = 0;
            int newMana = 0;

            using (var screenshot = Process.Screenshot())
            {
                newHealth = GetHealth(screenshot);
                newMana = GetMana(screenshot);
            }

            if (newHealth != Health)
            {
                foreach (ConquerTask task in Scheduler.Tasks.Where(t => t.Process.Equals(Process)))
                {
                    task.OnHealthChanged(Health, newHealth);
                }
            }

            if (newMana != Mana)
            {
                foreach (ConquerTask task in Scheduler.Tasks.Where(t => t.Process.Equals(Process)))
                {
                    task.OnManaChanged(Mana, newMana);
                }
            }

            Health = newHealth;
            Mana = newMana;

            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
