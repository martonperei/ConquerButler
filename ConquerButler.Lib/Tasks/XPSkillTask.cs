using Accord.Extensions.Imaging.Algorithms.LINE2D;
using log4net;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using TemplatePyramid = Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplatePyramid<Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplate>;
using ConquerButler.Native;

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

        protected async override Task DoTick()
        {
            if (_xpSkillTemplate != null)
            {
                List<Match> isXpSkill = FindMatches(0.9f, ConquerControlConstants.XP_SKILLS, _xpSkillTemplate);

                if (isXpSkill.Count > 0)
                {
                    await EnqueueInputAction(async () =>
                    {
                        Point p = Process.GetCursorPosition();

                        Process.LeftClick(isXpSkill[0].Center());

                        await Delay(500);

                        Helpers.ClientToVirtualScreen(Process.InternalProcess, ref p);

                        Process.Simulator.Mouse.MoveMouseTo(p.X, p.Y);
                    });
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
