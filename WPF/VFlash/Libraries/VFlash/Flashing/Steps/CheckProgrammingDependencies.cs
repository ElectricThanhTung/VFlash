using CanSharp;

namespace VFlash.Flashing {
    public class CheckProgrammingDependencies : FlashAction {
        public override void Execute(FlashActionArgs actionArgs) {
            StartNewAction("Check Programming Dependencies", () => {
                UDSResponse response = actionArgs.EcuUDS.SendRequest(UDSMessage.CheckDependencies, actionArgs.Timeout);
                if(!response)
                    throw new FlashException("Check Programming Dependencies Failed");
            });
        }
    }
}
