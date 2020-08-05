using System;

namespace Elwark.People.Api.Application.Models.Requests
{
    public class UpdatePictureRequest
    {
        public UpdatePictureRequest(Uri? picture)
        {
            Picture = picture;
        }

        public Uri? Picture { get; }
    }
}