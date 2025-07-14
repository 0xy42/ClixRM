using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Services.Authentication
{
    internal class WwwAuthenticateParameters
    {
        private readonly Dictionary<string, string> _parameters;

        public WwwAuthenticateParameters(string headerValue)
        {
            _parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var value = headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? headerValue.Substring("Bearer ".Length)
                : headerValue;

            var pairs = value.Split([','], StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                var parts = pair.Split(['='], 2);

                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var val = parts[1].Trim().Trim('"');
                    _parameters[key] = val;
                }
            }
        }

        public string? AuthorizationUri => _parameters.GetValueOrDefault("authorization_uri");
        public string? ResourceId => _parameters.GetValueOrDefault("resource_id");

        public Guid? GetTenantId()
        {
            if (string.IsNullOrEmpty(AuthorizationUri))
            {
                return null;
            }

            var uri = new Uri(AuthorizationUri);
            var segments = uri.AbsolutePath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                if (Guid.TryParse(segment, out var tenantId))
                {
                    return tenantId;
                }
            }

            return null;
        }
    }
}
