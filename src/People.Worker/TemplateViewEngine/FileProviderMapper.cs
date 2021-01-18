using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace People.Worker.TemplateViewEngine
{
    public class FileProviderMapper : IFileProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly string _partialsFolder;

        public FileProviderMapper(IFileProvider fileProvider)
            : this(fileProvider, "Partials") =>
            _fileProvider = fileProvider;

        public FileProviderMapper(IFileProvider fileProvider, string partialsFolder)
        {
            _fileProvider = fileProvider;
            _partialsFolder = partialsFolder;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            var path = Path.Combine(_partialsFolder, subpath);
            return _fileProvider.GetDirectoryContents(path);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            var path = Path.Combine(_partialsFolder, subpath);
            return _fileProvider.GetFileInfo(path);
        }

        public IChangeToken Watch(string filter) =>
            _fileProvider.Watch(filter);
    }
}