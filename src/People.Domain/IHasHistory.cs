using System;

namespace People.Domain
{
    public interface IHasHistory
    {
        DateTime CreatedAt { get; }
        
        DateTime UpdatedAt { get; }
        
        void SetAsCreated(DateTime date);
        
        void SetAsUpdated(DateTime date);
    }
}
