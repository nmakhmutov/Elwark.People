using System;

namespace Elwark.People.Shared.Primitives
{
    public class UrlTemplate
    {
        public UrlTemplate(string url, string marker)
        {
            Url = url;
            Marker = marker;
        }

        public string Url { get; }

        public string Marker { get; }
        
        public Uri Build(string value) =>
            new Uri(Url.Replace(Marker, value));

        public override string ToString() => Url;
    }
}