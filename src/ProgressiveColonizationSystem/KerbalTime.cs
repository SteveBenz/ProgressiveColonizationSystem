namespace ProgressiveColonizationSystem
{
    public static class KerbalTime
    {
        public static double KerbalYearsToSeconds(double years) => KerbalDaysToSeconds(years * 426.0);
        public static double KerbalYearsToDays(double years) => years * 426.0;
        public static double SecondsToKerbalDays(double seconds) => seconds / (6.0 * 60.0 * 60.0);
        public static double KerbalDaysToSeconds(double days) => days * (6.0 * 60.0 * 60.0);
        public static double KerbalSecondsToDays(double days) => days / (6.0 * 60.0 * 60.0);
    }
}
