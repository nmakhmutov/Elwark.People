using System;

namespace Elwark.People.Api.Requests
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