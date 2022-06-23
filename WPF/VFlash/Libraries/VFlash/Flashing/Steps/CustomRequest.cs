using CanSharp;

namespace VFlash.Flashing {
    public class CustomRequest : FlashAction {
        public CustomRequest() {
            Name = "Custom Request Action";
            IsFunctional = false;
            DataRequest = null;
            ExpectedResponse = null;
        }

        public string Name {
            get; set;
        }

        public bool IsFunctional {
            get; set;
        }

        public byte[] DataRequest {
            get; set;
        }

        public byte[] ExpectedResponse {
            get; set;
        }

        private bool CompareResponse(UDSResponse response) {
            if(response.Count != ExpectedResponse.Length)
                return false;
            for(int i = 0; i < response.Count; i++) {
                if(response[i] != ExpectedResponse[i])
                    return false;
            }
            return true;
        }

        public override void Execute(FlashActionArgs actionArgs) {
            StartNewAction(Name, () => {
                UDS uds = IsFunctional ? actionArgs.FunUDS : actionArgs.EcuUDS;
                UDSResponse response = uds.SendRequest(DataRequest, 0);
                if(response != UDSResponseType.SendRequestSuccessful)
                    throw new FlashException("Hard Reset Failed");
                response = uds.WaitResoponse(DataRequest, actionArgs.Timeout);
                if(response == UDSResponseType.NegativeResponse)
                    throw new FlashException(Name + " Failed");
                else if(!IsFunctional || ExpectedResponse != null) {
                    if(ExpectedResponse != null) {
                        if(!CompareResponse(response))
                            throw new FlashException("Response Value does not match ExpectedResponse Value");
                    }
                    else if(!response)
                        throw new FlashException(Name + " Failed");
                }
            });
        }
    }
}
