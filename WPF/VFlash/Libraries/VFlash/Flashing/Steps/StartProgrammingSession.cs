using CanSharp;

namespace VFlash.Flashing {
    public class StartProgrammingSession : FlashAction {
        public override void Execute(FlashActionArgs actionArgs) {
            StartNewAction("Start Programming Session", () => {
                byte[] request = UDSMessage.ProgrammingSession;
                UDSResponse response = actionArgs.EcuUDS.SendRequest(request, actionArgs.Timeout);
                if(!response)
                    throw new FlashException("Start Programming Session Failed");
            });
        }
    }
}
