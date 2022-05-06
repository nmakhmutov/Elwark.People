using System.Collections.Generic;

namespace People.Domain.Aggregates.AccountAggregate;

public interface IPasswordHasher
{
    byte[] CreateSalt();

    byte[] CreateHash(string password, ICollection<byte> salt);
}
