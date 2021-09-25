using System;

namespace People.Account.Domain.Seed
{
    public interface IHasHistory
    {
        DateTime CreatedAt { get; }
        
        DateTime UpdatedAt { get; }
        
        void SetAsCreated(DateTime date);
        
        void SetAsUpdated(DateTime date);
    }
}
