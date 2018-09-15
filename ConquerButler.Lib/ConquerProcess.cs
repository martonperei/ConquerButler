using log4net;
using System;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using WindowsInput;
using ConquerButler.Native;
using PropertyChanged;

namespace ConquerButler
{
    public class ConquerProcess : IDisposable
    {
        private static ILog log = LogManager.GetLogger(typeof(ConquerProcess));

        public int Id { get; protected set; }
        public ConquerScheduler Scheduler { get; protected set; }
        public Process InternalProcess { get; protected set; }

        public bool Disconnected { get; set; }

        public InputSimulator Simulator { get; protected set; }

        private readonly Random _random;

        public ConquerProcess(Process process, ConquerScheduler scheduler)
        {
            Id = process.Id;
            InternalProcess = process;
            Scheduler = scheduler;
            Simulator = new InputSimulator();

            _random = new Random();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Bitmap Screenshot()
        {
            return Helpers.PrintWindow(InternalProcess);
        }

        public Point GetCursorPosition()
        {
            return Helpers.GetCursorPosition(InternalProcess);
        }

        public bool HasUserFocus()
        {
            return Helpers.IsForegroundWindow(InternalProcess) &&
                Helpers.IsCursorInsideWindow(Helpers.GetCursorPosition(InternalProcess), InternalProcess);
        }

        private void TranslateToVirtualScreen(ref Point p, int variation = 5)
        {
            p.X = p.X + _random.Next(-variation, variation);
            p.Y = p.Y + _random.Next(-variation, variation);

            Helpers.ClientToVirtualScreen(InternalProcess, ref p);
        }

        public void RightClick(Point p, int variation = 5)
        {
            log.Debug($"Process {InternalProcess.Id} - RightClick {p.X}-{p.Y}");

            TranslateToVirtualScreen(ref p, variation);

            Simulator.Mouse.MoveMouseTo(p.X, p.Y);
            Simulator.Mouse.RightButtonClick();
        }

        public void LeftClick(Point p, int variation = 5)
        {
            log.Debug($"Process {InternalProcess.Id} - LeftClick {p.X}-{p.Y}");

            TranslateToVirtualScreen(ref p, variation);

            Simulator.Mouse.MoveMouseTo(p.X, p.Y);
            Simulator.Mouse.LeftButtonClick();
        }

        public void MoveTo(Point p, int variation = 5)
        {
            log.Debug($"Process {InternalProcess.Id} - MoveTo {p.X}-{p.Y}");

            TranslateToVirtualScreen(ref p, variation);

            Simulator.Mouse.MoveMouseTo(p.X, p.Y);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConquerProcess()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
        }
    }
}
