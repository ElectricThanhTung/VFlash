using CanSharp;

namespace VFlash.Flashing {
    public class CheckProgrammingPreconditions : FlashAction {
        public override void Execute(FlashActionArgs actionArgs) {
            StartNewAction("Check Programming Preconditions", () => {
                byte[] request = UDSMessage.CheckProgrammingPreconditions;
                UDSResponse response = actionArgs.EcuUDS.SendRequest(request, actionArgs.Timeout);
                if(!response)
                    throw new FlashException("Check Programming Preconditions Failed");
            });
        }
    }
}
