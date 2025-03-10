﻿using CanSharp;

namespace VFlash.Flashing {
    public class HardReset : FlashAction {
        public HardReset() {
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
            StartNewAction("Hard Reset", () => {
                byte[] request = (byte[])UDSMessage.HardReset.Clone();
                request[1] |= (byte)(SuppressBit ? 0x80 : 0x00);
                UDS uds = IsFunctional ? actionArgs.FunUDS : actionArgs.EcuUDS;
                UDSResponse response = uds.SendRequest(request, 0);
                if(response != UDSResponseType.SendRequestSuccessful)
                    throw new FlashException("Hard Reset Failed");
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
