using Microsoft.Extensions.Localization;

namespace SAT242516026.Models.Services
{
}  public class LocalizerService<TResource>
    {
        private readonly IStringLocalizer _localizer;

        public LocalizerService(IStringLocalizerFactory factory)
        {
            var assemblyName = typeof(TResource).Assembly.GetName().Name!;
            var resourceName = typeof(TResource).Name;
            _localizer = factory.Create(resourceName, assemblyName);
        }

        public LocalizedString this[string key] => _localizer[key];
        public string Get(string key) => _localizer[key];
    }

