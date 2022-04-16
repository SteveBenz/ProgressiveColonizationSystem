namespace ProgressiveColonizationSystem
{
    public static class KerbalTime
    {
        private static int secondsPerDay => KSPUtil.dateTimeFormatter.Day;
        private static int secondsPerYear => KSPUtil.dateTimeFormatter.Year;

        public static double KerbalYearsToSeconds(double years) => years*secondsPerYear;
        public static double KerbalYearsToDays(double years) => years * secondsPerYear/secondsPerDay;
        public static double SecondsToKerbalDays(double seconds) => seconds / secondsPerDay;
        public static double KerbalDaysToSeconds(double days) => days * secondsPerDay;
        public static double KerbalSecondsToDays(double days) => days / secondsPerDay;
        public static double UnitsPerDayToUnitsPerSecond(double unitsPerDay) => unitsPerDay / secondsPerDay;
        public static double UnitsPerSecondToUnitsPerDay(double unitsPerSecond) => unitsPerSecond * secondsPerDay;
    }
}
