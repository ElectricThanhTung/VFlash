using CanSharp;

namespace VFlash.Flashing {
    public class EnableNormalCommunication : FlashAction {
        public EnableNormalCommunication() {
            IsFunctional = true;
            SuppressBit = false;
        }

        public bool IsFunctional {
            get; set;
        }

        public bool SuppressBit {
            get; set;
        }

        public override void Execute(FlashActionArgs actionArgs) {
            StartNewAction("Enable Normal Communication", () => {
                byte[] request = (byte[])UDSMessage.EnableNormalCommunication.Clone();
                request[1] |= (byte)(SuppressBit ? 0x80 : 0x00);
                UDS uds = IsFunctional ? actionArgs.FunUDS : actionArgs.EcuUDS;
                UDSResponse response = uds.SendRequest(request, 0);
                if(response != UDSResponseType.SendRequestSuccessful)
                    throw new FlashException("Enable Normal Communication Failed");
                if(!SuppressBit) {
                    response = uds.WaitResoponse(request, actionArgs.Timeout);
                    if(!IsFunctional && !response)
                        throw new FlashException("Hard Reset Failed");
                    else if(response == UDSResponseType.NegativeResponse)
                        throw new FlashException("Hard Reset Failed");
                }
            });
        }
    }
}
