using KeyInventory.Domain.Parties;

namespace KeyInventory.Application.Workforce;

public interface ICreatePartyUseCase
{
    Task ExecuteAsync(string partyCode, string firstName, string lastName, string uin, CancellationToken cancellationToken);
}

public interface IListPartiesUseCase
{
    Task<IReadOnlyList<PartyListItem>> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class CreatePartyUseCase : ICreatePartyUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public CreatePartyUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public async Task ExecuteAsync(
        string partyCode,
        string firstName,
        string lastName,
        string uin,
        CancellationToken cancellationToken)
    {
        if (await _workforce.PartyExistsAsync(partyCode, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A party with this party code already exists.");
        }

        Party party = new(partyCode, firstName, lastName, uin);
        if (await _workforce.PartyUinExistsAsync(party.Uin, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("A party with this UIN already exists.");
        }

        await _workforce.AddPartyAsync(party, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ListPartiesUseCase : IListPartiesUseCase
{
    private readonly IWorkforcePersistencePort _workforce;

    public ListPartiesUseCase(IWorkforcePersistencePort workforce)
    {
        _workforce = workforce ?? throw new ArgumentNullException(nameof(workforce));
    }

    public Task<IReadOnlyList<PartyListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _workforce.ListPartiesAsync(cancellationToken);
    }
}
