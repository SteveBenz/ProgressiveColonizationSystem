using System;

namespace ProgressiveColonizationSystem
{
    public class PksRetrainingEntry
    {
        public PksRetrainingEntry(string kerbalName, double remainingTrainingTime, string futureTrait)
        {
            this.KerbalName = kerbalName;
            this.RemainingTrainingTime = remainingTrainingTime;
            this.FutureTrait = futureTrait;
        }

        public void ChangeFutureTrait(double remainingTrainingTime, string futureTrait)
        {
            this.RemainingTrainingTime = remainingTrainingTime;
            this.FutureTrait = futureTrait;
        }

        public string KerbalName { get; }

        public double RemainingTrainingTime { get; private set; }

        public string FutureTrait { get; private set; }

        public bool IsComplete => RemainingTrainingTime <= 0;

        public static bool TryCreateFromNode(ConfigNode node, out PksRetrainingEntry entry)
        {
            string kerbalName = "";
            string futureTrait = "";
            double remainingTrainingTime = 0;

            if (node.TryGetValue("kerbal", ref kerbalName)
                && node.TryGetValue("trait", ref futureTrait)
                && node.TryGetValue("time", ref remainingTrainingTime))
            {
                entry = new PksRetrainingEntry(kerbalName, remainingTrainingTime, futureTrait);
                return true;
            }
            else
            {
                entry = null;
                return false;
            }
        }

        public ConfigNode CreateNode()
        {
            ConfigNode node = new ConfigNode();
            node.AddValue("kerbal", this.KerbalName);
            node.AddValue("trait", this.FutureTrait);
            node.AddValue("time", this.RemainingTrainingTime);
            return node;
        }

        internal void AddStudyTime(double elapsed)
        {
            this.RemainingTrainingTime -= elapsed;
        }
    }
}
