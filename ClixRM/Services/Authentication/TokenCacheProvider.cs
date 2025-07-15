using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace ClixRM.Services.Authentication
{
    public static class TokenCacheProvider
    {
        private const string CacheFileName = "clixrm_msal.cache";
        private static readonly string CacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClixRM");

        private static readonly MsalCacheHelper _cacheHelper;

        static TokenCacheProvider()
        {
            Directory.CreateDirectory(CacheDir);

            var storageProperties =
                new StorageCreationPropertiesBuilder(CacheFileName, CacheDir)
                    .Build();

            _cacheHelper = MsalCacheHelper.CreateAsync(storageProperties).GetAwaiter().GetResult();
        }

        public static IPublicClientApplication CreateClientApplication(PublicClientApplicationBuilder builder)
        {
            var app = builder.Build();

            _cacheHelper.RegisterCache(app.UserTokenCache);

            return app;
        }
    }
}
