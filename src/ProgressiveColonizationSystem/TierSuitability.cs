namespace ProgressiveColonizationSystem
{
    public enum TierSuitability
    {
        /// <summary>
        ///   The highest tier for the given research level
        /// </summary>
        Ideal,

        /// <summary>
        ///   A higher tier could be produced instead
        /// </summary>
        UnderTier,

        /// <summary>
        ///   Would ultimately fail if the scanner technology is not researched
        /// </summary>
        LacksScanner,

        /// <summary>
        ///   Would certainly fail because some underlying research has not been done.
        /// </summary>
        LacksSubordinateResearch,

        /// <summary>
        ///   Isn't available at all because the research hasn't been done
        /// </summary>
        NotResearched,

        /// <summary>
        ///   No target body has been selected
        /// </summary>
        BodyNotSelected,

        /// <summary>
        ///   The part does not support the tech tier selected
        /// </summary>
        PartDoesntSupportTier,
    }
}
