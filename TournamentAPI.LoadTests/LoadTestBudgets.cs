namespace TournamentAPI.LoadTests;

internal static class LoadTestBudgets
{
    public static class ListTournaments
    {
        public const string Step = "list_tournaments";

        public const double MaxErrorPercent = 0;
        public const double MinOkStatusPercent = 100;

        public const double P95Ms = 150;
        public const double P99Ms = 300;
        public const double MaxMs = 1000;

        public const double MinMeanPayloadBytes = 2_000;
        public const double MaxMeanPayloadBytes = 5_000;
    }

    public static class Load
    {
        public const int NormalRatePerSecond = 50;
    }

    public static class Ramp
    {
        public const int BaselineRatePerSecond = 25;
        public const int MidRatePerSecond = 75;
        public const int PeakRatePerSecond = 100;

        public const double P95PerLevelMs = 800;
        public const double P99Ms = 1_000;
        public const double MaxMs = 3_000;

        public const double MaxTopToBottomP95Ratio = 6;
        public const double MaxRecoveryP95Ratio = 2;
    }
}
