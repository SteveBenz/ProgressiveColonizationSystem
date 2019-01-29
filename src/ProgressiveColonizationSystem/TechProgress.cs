using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public class TechProgress
    {
        /// <summary>
        ///   The current progress tech level
        /// </summary>
        public TechTier Tier;

        /// <summary>
        ///   The current progress towards advancing to the next level
        /// </summary>
        public double ProgressInKerbalSeconds;
    }
}
