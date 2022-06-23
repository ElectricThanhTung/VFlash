using System.Diagnostics;
using System.Threading;

namespace VFlash.Flashing {
    internal class DelayAction : FlashAction {
        public DelayAction() {
            Name = "Delay Action";
            Time = 1000;
        }

        public string Name {
            get; set;
        }

        public int Time {
            get; set;
        }

        public override void Execute(FlashActionArgs actionArgs) {
            Stopwatch stopwatch = Stopwatch.StartNew();
            StartNewAction(Name, () => {
                while(stopwatch.ElapsedMilliseconds < Time) {
                    if((Time - stopwatch.ElapsedMilliseconds) > 10)
                        Thread.Sleep(1);
                }
                stopwatch.Stop();
            });
        }
    }
}
