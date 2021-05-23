using Accord.Extensions.Imaging.Algorithms.LINE2D;
using log4net;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using TemplatePyramid = Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplatePyramid<Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplate>;
using ConquerButler.Native;
using WindowsInput.Native;

namespace ConquerButler.Tasks
{
    public class XPSkillTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(XPSkillTask));

        public static string TASK_TYPE_NAME = "XPSkill";

        private TemplatePyramid _xpSkillTemplate;

        private string _xpSkillName;

        public string XPSkillName
        {
            set
            {
                if (value != _xpSkillName)
                {
                    _xpSkillName = value;

                    _xpSkillTemplate = LoadTemplate("images/" + _xpSkillName + ".png");
                }
            }
            get { return _xpSkillName; }
        }

        public XPSkillTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            Interval = 5000;
        }

        protected internal async override Task Tick()
        {
            if (_xpSkillTemplate != null)
            {
                List<Match> isXpSkill = FindMatches(0.9f, ConquerControlConstants.XP_SKILLS, _xpSkillTemplate);

                if (isXpSkill.Count > 0)
                {
                    await EnqueueInputAction(async () =>
                    {
                        await Delay(250);
                        Process.Simulator.Keyboard.KeyPress(VirtualKeyCode.F6);
                        await Delay(250);
                        Process.Simulator.Keyboard.KeyPress(VirtualKeyCode.F6);
                        //Point p = Process.GetCursorPosition();

                        //Point skillP = isXpSkill[0].Center();

                        //Process.TranslateToVirtualScreen(ref skillP, 5);

                        //Process.Simulator.Mouse.MoveMouseTo(skillP.X, skillP.Y);

                        //await Delay(500);

                        //Process.Simulator.Mouse.LeftButtonClick();

                        //await Delay(500);

                        //Helpers.ClientToVirtualScreen(Process.InternalProcess, ref p);

                        //Process.Simulator.Mouse.MoveMouseTo(p.X, p.Y);
                    }, 0);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
