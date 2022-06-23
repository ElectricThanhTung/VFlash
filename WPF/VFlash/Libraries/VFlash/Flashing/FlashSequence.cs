using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace VFlash.Flashing {
    internal class FlashSequence : FlashAction {
        private FlashActionArgs actionArgs;
        private List<FlashAction> actions;
        private Thread sendTesterPresentThread;

        public FlashSequence(List<FlashAction> actions) {
            this.actions = actions;
            TesterPresent = true;
        }

        public bool TesterPresent {
            get; set;
        }

        public static List<FlashAction> DefaultActions {
            get {
                List<FlashAction> defaultActions = new List<FlashAction>() {
                    new StartExtendedSession(),
                    new CheckProgrammingPreconditions(),
                    new DisableDTCSetting(),
                    new DisableNormalCommunication(),
                    new StartProgrammingSession(),
                    new PerformSecurityAccess(),
                    new WriteFingerPrint(),
                    new FlashBinaries(),
                    new CheckProgrammingDependencies(),
                    new HardReset() { AlwaysExecuted = true },
                    new EnableNormalCommunication() { AlwaysExecuted = true },
                    new EnableDTCSetting() { AlwaysExecuted = true },
                    new StartDefaultSession() { AlwaysExecuted = true },
                };

                return defaultActions;
            }
        }

        private void StartCommunication() {
            StartNewAction("Start Communication", () => {
                if(!(actionArgs.EcuUDS.Connect() && actionArgs.FunUDS.Connect()))
                    throw new FlashException("Start Communication Failed");
            });
        }

        private void StartSendTesterPresent() {
            sendTesterPresentThread = new Thread(() => {
                Stopwatch stopwatch = Stopwatch.StartNew();
                while(true) {
                    if(stopwatch.ElapsedMilliseconds >= 2500) {
                        stopwatch.Restart();
                        Thread.Sleep(50);
                        actionArgs.FunUDS.SendRequest(UDSMessage.TesterPresent, 0);
                        Thread.Sleep(50);
                    }
                    Thread.Sleep(10);
                }
            }) {
                IsBackground = true
            };
            sendTesterPresentThread.Start();
        }

        private void StopSendTesterPresent() {
            if(sendTesterPresentThread != null && sendTesterPresentThread.IsAlive)
                sendTesterPresentThread.Abort();
        }

        private void StopCommunication() {
            StartNewAction("Stop Communication", () => {
                actionArgs.EcuUDS.Disconnect();
                actionArgs.FunUDS.Disconnect();
            });
        }

        private void StepNewActionHander(object sender, InfomationEventArgs args) {
            OnNewAction(args.Message);
        }

        private void StepActionFinishHander(object sender, InfomationEventArgs args) {
            OnActionFinish(args.Message);
        }

        private void StepActionErrorHander(object sender, InfomationEventArgs args) {
            OnActionError(args.Message);
        }

        private void StepUnknowErrorHander(object sender, InfomationEventArgs args) {
            OnUnknowError(args.Message);
        }

        private void StepProgressChangedHander(object sender, ProgressChangedArgs args) {
            OnProgressChanged(args.FileIndex, args.Value, args.Total);
        }

        public override void Execute(FlashActionArgs actionArgs) {
            this.actionArgs = actionArgs;
            StartCommunication();
            if(TesterPresent)
                StartSendTesterPresent();

            bool stepIsSuccessful = true;

            foreach(FlashAction step in actions) {
                if(stepIsSuccessful || step.AlwaysExecuted) {
                    Thread.Sleep(100);
                    step.NewActionEvent += StepNewActionHander;
                    step.ActionFinishEvent += StepActionFinishHander;
                    step.ActionErrorEvent += StepActionErrorHander;
                    step.UnknowErrorEvent += StepUnknowErrorHander;
                    step.ProgressChanged += StepProgressChangedHander;
                    step.Execute(actionArgs);
                    step.NewActionEvent -= StepNewActionHander;
                    step.ActionFinishEvent -= StepActionFinishHander;
                    step.ActionErrorEvent -= StepActionErrorHander;
                    step.UnknowErrorEvent -= StepUnknowErrorHander;
                    step.ProgressChanged -= StepProgressChangedHander;
                }
                stepIsSuccessful &= step.IsSuccessful == true;
            }

            StopSendTesterPresent();
            StopCommunication();
        }
    }
}
