using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nerm.Colonization
{
    public class IntervesselResourceTransfer
    {
        private double restartTime = double.MinValue;

        public bool IsTransferUnderway { get; private set; }

        public float TransferPercent { get; private set; }

        public Vessel TargetVessel { get; private set; }

        public void StartTransfer()
        {
            IsTransferUnderway = true;
            TransferPercent = 0;
        }

        public void OnFixedUpdate()
        {
            if (restartTime == double.MinValue)
            {
                restartTime = Planetarium.GetUniversalTime();
            }
            else if (IsTransferUnderway)
            {
                this.TransferPercent += .001f;
                if (this.TransferPercent > 1)
                {
                    this.TransferPercent = 1;
                    IsTransferUnderway = false;
                    this.TargetVessel = null;
                    restartTime = Planetarium.GetUniversalTime();
                }
            }
            else if (Planetarium.GetUniversalTime() > restartTime + 7)
            {
                // FlightGlobals.VesselsLoaded
                this.TargetVessel = FlightGlobals.ActiveVessel;
            }
        }
    }
}
