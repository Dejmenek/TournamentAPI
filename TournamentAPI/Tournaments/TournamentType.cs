using GreenDonut.Data;
using HotChocolate.Data.Projections;
using TournamentAPI.Brackets;
using TournamentAPI.Data.Models;
using TournamentAPI.Participants;
using TournamentAPI.Users;

namespace TournamentAPI.Tournaments;

public class TournamentType : ObjectType<Tournament>
{
    protected override void Configure(IObjectTypeDescriptor<Tournament> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.StartDate);
        descriptor.Field(t => t.Status);
        descriptor.Field(t => t.OwnerId);
        descriptor.Field(t => t.Bracket)
            .Type<BracketType>();
        descriptor.Field(t => t.Owner)
            .Type<ApplicationUserType>()
            .ResolveWith<TournamentResolvers>(t => t.GetOwnerAsync(default!, default!, default))
            .UseFiltering<UserFilterInputType>()
            .UseSorting<UserSortInputType>();
        descriptor.Field(t => t.Participants)
            .ResolveWith<TournamentResolvers>(t => t.GetParticipantsAsync(default!, default!, default!, default))
            .Type<ListType<TournamentParticipantType>>();
    }

    private class TournamentResolvers
    {
        public async Task<IEnumerable<TournamentParticipant>> GetParticipantsAsync(
            [Parent] Tournament tournament,
            IProjectionSelection projectionSelection,
            ParticipantsByTournamentIdDataLoader participantsByTournamentId,
            CancellationToken cancellationToken)
            => await participantsByTournamentId
                .Select(projectionSelection)
                .LoadAsync(tournament.Id, cancellationToken) ?? [];

        public async Task<ApplicationUser?> GetOwnerAsync(
            [Parent] Tournament tournament,
            OwnerByTournamentIdDataLoader ownerById,
            CancellationToken cancellationToken)
        {
            if (tournament.Owner != null)
                return tournament.Owner;

            return await ownerById
                .LoadAsync(tournament.OwnerId, cancellationToken);
        }
    }
}
