using System;

namespace People.Domain.AggregateModels.Account
{
    public sealed record Profile(
        Language Language,
        Gender Gender,
        Uri Picture,
        string? Bio = null,
        DateTime? DateOfBirth = null
    )
    {
        public static Uri DefaultPicture => 
            new("https://res.cloudinary.com/elwark/image/upload/v1610430646/People/default_j21xml.png");
    }
}