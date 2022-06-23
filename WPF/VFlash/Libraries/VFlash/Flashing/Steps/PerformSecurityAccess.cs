using CanSharp;
using System;

namespace VFlash.Flashing {
    public class PerformSecurityAccess : FlashAction {
        private FlashActionArgs actionArgs;

        private byte[] ResquestSeedKey() {
            byte[] seed = null;
            StartNewAction("Resquest Seed Key", () => {
                byte[] requestSeed = { 0x27, (byte)actionArgs.SecurityLevel };
                UDSResponse response = actionArgs.EcuUDS.SendRequest(requestSeed, actionArgs.Timeout);
                if(!response)
                    throw new FlashException("Resquest Seed Key Failed");
                seed = new byte[response.Count - 2];
                Array.Copy(response.ToArray(), 2, seed, 0, seed.Length);
            });
            return seed;
        }

        private void SecurityAccess(byte[] seed) {
            StartNewAction("Perform Security Access", () => {
                SeedKeyLoader seedKeyLoader = new SeedKeyLoader(actionArgs.SeedKeyDll);
                byte[] OEMKey = seedKeyLoader.GenKey(seed, (uint)actionArgs.SecurityLevel);
                byte[] securityResquest = new byte[OEMKey.Length + 2];
                securityResquest[0] = 0x27;
                securityResquest[1] = (byte)(actionArgs.SecurityLevel + 1);
                Array.Copy(OEMKey, 0, securityResquest, 2, OEMKey.Length);
                UDSResponse response = actionArgs.EcuUDS.SendRequest(securityResquest, actionArgs.Timeout);
                if(!response)
                    throw new FlashException("Resquest Send Key Failed");
            });
        }

        public override void Execute(FlashActionArgs actionArgs) {
            this.actionArgs = actionArgs;
            SecurityAccess(ResquestSeedKey());
        }
    }
}
