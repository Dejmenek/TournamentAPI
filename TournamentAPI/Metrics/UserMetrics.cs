using System.Diagnostics.Metrics;

namespace TournamentAPI.Metrics;

public class UserMetrics
{
    private readonly Counter<int> _loginAttemptsTotal;
    private readonly Counter<int> _userRegistrationsTotal;
    private readonly Counter<int> _refreshTokenFailuresTotal;

    public UserMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricConstants.UserMeterName);
        _loginAttemptsTotal = meter.CreateCounter<int>("tournamentapi.users.login_attempts_total", description: "Number of login attempts");
        _userRegistrationsTotal = meter.CreateCounter<int>("tournamentapi.users.user_registrations_total", description: "Number of successful user registrations");
        _refreshTokenFailuresTotal = meter.CreateCounter<int>("tournamentapi.users.refresh_token_failures_total", description: "Number of refresh token failures");
    }

    public void LoginSucceeded() => _loginAttemptsTotal.Add(1, new KeyValuePair<string, object?>("result", "success"));

    public void LoginFailed() => _loginAttemptsTotal.Add(1, new KeyValuePair<string, object?>("result", "failure"));

    public void UserRegistered() => _userRegistrationsTotal.Add(1);

    public void RefreshTokenFailed(string reason) => _refreshTokenFailuresTotal.Add(1, new KeyValuePair<string, object?>("reason", reason));
}
