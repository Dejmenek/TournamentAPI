namespace TournamentAPI.LoadTests;

internal sealed record StepBudget(
    string Step,
    double P95Ms,
    double P99Ms,
    double MaxMs,
    double? MinMeanBytes = null,
    double? MaxMeanBytes = null);

internal sealed record MeasuredBaseline(double P95Ms, double P99Ms, double MaxMs, double MeanKilobytes);

internal static class StepNames
{
    public const string ListTournaments = "list_tournaments";
    public const string ViewTournament = "view_tournament";
    public const string SearchTournamentsByName = "search_tournaments_by_name";
    public const string SortTournaments = "sort_tournaments";
    public const string ViewOwnerHistory = "view_owner_history";
    public const string ViewParticipantsWonTournaments = "view_participants_won_tournaments";
    public const string ViewParticipantsPlayedTournaments = "view_participants_played_tournaments";
    public const string ViewParticipantsWonMatches = "view_participants_won_matches";
    public const string ViewBracketStandings = "view_bracket_standings";
    public const string ViewOwnTournamentHistory = "view_own_tournament_history";
    public const string ListTournamentsWithOversizedPage = "list_tournaments_with_oversized_page";
    public const string Login = "login";
    public const string ViewProfile = "view_profile";
    public const string RefreshToken = "refresh_token";
    public const string Logout = "logout";
}

internal static class LoadTestBudgets
{
    public static class Rates
    {
        public const int ListTournamentsPerSecond = 15;
        public const int ViewTournamentPerSecond = 6;
        public const int DiscoverTournamentsPerSecond = 4;
        public const int InsightPerSecond = 5;
        public const int AuthFlowPerSecond = 5;
    }

    public static class Steps
    {
        private const double P95Headroom = 3.5;
        private const double P99Headroom = 3.5;
        private const double MaxHeadroom = 5;
        private const double PayloadFloorRatio = 0.6;
        private const double PayloadCeilingRatio = 1.6;
        private const double BytesPerKilobyte = 1_000;

        private const double P95FloorMs = 100;
        private const double P99FloorMs = 250;
        private const double MaxFloorMs = 1_000;

        private static readonly Dictionary<(LoadTestDataSize Size, string Step), MeasuredBaseline> Measured = new()
        {
            [(LoadTestDataSize.Default, StepNames.ListTournaments)] = new(7.8, 13.46, 48.05, 1.388),
            [(LoadTestDataSize.Default, StepNames.ViewTournament)] = new(18.96, 23.97, 30.28, 0.592),
            [(LoadTestDataSize.Default, StepNames.SearchTournamentsByName)] = new(8.26, 11.89, 12.15, 0.314),
            [(LoadTestDataSize.Default, StepNames.SortTournaments)] = new(19.54, 21.01, 22.43, 2.678),

            [(LoadTestDataSize.Medium, StepNames.ListTournaments)] = new(8.56, 13.29, 35.31, 1.410),
            [(LoadTestDataSize.Medium, StepNames.ViewTournament)] = new(23.52, 47.84, 60.71, 1.425),
            [(LoadTestDataSize.Medium, StepNames.SearchTournamentsByName)] = new(8.49, 14.45, 35.30, 1.410),
            [(LoadTestDataSize.Medium, StepNames.SortTournaments)] = new(25.81, 48.42, 61.24, 6.173),
            [(LoadTestDataSize.Medium, StepNames.ViewOwnerHistory)] = new(124.61, 215.81, 241.48, 0.415),
            [(LoadTestDataSize.Medium, StepNames.ViewParticipantsWonTournaments)] = new(220.67, 260.99, 263.62, 0.614),
            [(LoadTestDataSize.Medium, StepNames.ViewParticipantsPlayedTournaments)] = new(193.15, 265.73, 324.24, 0.638),
            [(LoadTestDataSize.Medium, StepNames.ViewParticipantsWonMatches)] = new(236.42, 321.02, 321.11, 0.583),
            [(LoadTestDataSize.Medium, StepNames.ViewBracketStandings)] = new(72.58, 211.46, 249.80, 1.007),
            [(LoadTestDataSize.Medium, StepNames.ViewOwnTournamentHistory)] = new(94.66, 208.77, 208.91, 0.487),
            [(LoadTestDataSize.Medium, StepNames.ListTournamentsWithOversizedPage)] = new(14.54, 48.19, 147.34, 0.213),
            [(LoadTestDataSize.Medium, StepNames.Login)] = new(537.60, 824.83, 970.28, 0.653),
            [(LoadTestDataSize.Medium, StepNames.ViewProfile)] = new(18.18, 39.23, 47.57, 0.131),
            [(LoadTestDataSize.Medium, StepNames.RefreshToken)] = new(84.42, 170.62, 215.82, 0.656),
            [(LoadTestDataSize.Medium, StepNames.Logout)] = new(46.69, 83.07, 83.73, 0.039),

            [(LoadTestDataSize.Large, StepNames.ListTournaments)] = new(9.35, 12.01, 41.25, 1.416),
            [(LoadTestDataSize.Large, StepNames.ViewTournament)] = new(30.34, 45.41, 51.63, 1.770),
            [(LoadTestDataSize.Large, StepNames.SearchTournamentsByName)] = new(10.14, 22.27, 41.25, 1.416),
            [(LoadTestDataSize.Large, StepNames.SortTournaments)] = new(42.40, 54.37, 74.04, 8.631),
            [(LoadTestDataSize.Large, StepNames.ViewOwnerHistory)] = new(144.77, 237.18, 288.05, 0.571),
            [(LoadTestDataSize.Large, StepNames.ViewParticipantsWonTournaments)] = new(244.74, 369.92, 486.91, 1.162),
            [(LoadTestDataSize.Large, StepNames.ViewParticipantsPlayedTournaments)] = new(240.26, 408.32, 430.80, 1.216),
            [(LoadTestDataSize.Large, StepNames.ViewParticipantsWonMatches)] = new(253.18, 411.39, 472.05, 1.110),
            [(LoadTestDataSize.Large, StepNames.ViewBracketStandings)] = new(117.38, 237.82, 262.43, 1.910),
            [(LoadTestDataSize.Large, StepNames.ViewOwnTournamentHistory)] = new(130.69, 231.04, 233.95, 0.940),
            [(LoadTestDataSize.Large, StepNames.ListTournamentsWithOversizedPage)] = new(8.34, 29.39, 43.05, 0.213)
        };

        public static StepBudget For(LoadTestDataSize size, string step)
        {
            if (!Measured.TryGetValue((size, step), out var baseline))
            {
                throw new InvalidOperationException($"No measured baseline for step '{step}' on the {size} dataset.");
            }

            return new StepBudget(
                step,
                Math.Max(P95FloorMs, baseline.P95Ms * P95Headroom),
                Math.Max(P99FloorMs, baseline.P99Ms * P99Headroom),
                Math.Max(MaxFloorMs, baseline.MaxMs * MaxHeadroom),
                baseline.MeanKilobytes * BytesPerKilobyte * PayloadFloorRatio,
                baseline.MeanKilobytes * BytesPerKilobyte * PayloadCeilingRatio);
        }
    }

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
