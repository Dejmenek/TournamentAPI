using NBomber.Contracts;
using NBomber.CSharp;

namespace TournamentAPI.LoadTests;

internal static class GraphQLLoadScenario
{
    /// <summary>
    /// Creates a scenario whose own result only reports success or failure of the last step.
    /// Returning the step response directly makes NBomber count its status code and payload a second time at scenario level.
    /// </summary>
    public static ScenarioProps Create(string name, Func<IScenarioContext, Task<Response<object>>> run)
    {
        return Scenario.Create(name, async context =>
        {
            var last = await run(context);

            return last.IsError ? Response.Fail() : Response.Ok();
        });
    }
}
