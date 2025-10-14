#if NET
#nullable enable
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

// ReSharper disable once CheckNamespace

namespace Unify.Web.Theme.Pages.Components.Identity;

 public static class IdentityExtensions
    {
        private const string AzureProfilePhotoDir = "App_Data/UserContent/azure-profile-photos";

        private const string MockAvatar =
            "data:image/png;base64,/9j/4QAYRXhpZgAASUkqAAgAAAAAAAAAAAAAAP/sABFEdWNreQABAAQAAAA8AAD/4QMvaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wLwA8P3hwYWNrZXQgYmVnaW49Iu+7vyIgaWQ9Ilc1TTBNcENlaGlIenJlU3pOVGN6a2M5ZCI/PiA8eDp4bXBtZXRhIHhtbG5zOng9ImFkb2JlOm5zOm1ldGEvIiB4OnhtcHRrPSJBZG9iZSBYTVAgQ29yZSA5LjEtYzAwMiA3OS5mMzU0ZWZjNzAsIDIwMjMvMTEvMDktMTI6MDU6NTMgICAgICAgICI+IDxyZGY6UkRGIHhtbG5zOnJkZj0iaHR0cDovL3d3dy53My5vcmcvMTk5OS8wMi8yMi1yZGYtc3ludGF4LW5zIyI+IDxyZGY6RGVzY3JpcHRpb24gcmRmOmFib3V0PSIiIHhtbG5zOnhtcD0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wLyIgeG1sbnM6eG1wTU09Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8iIHhtbG5zOnN0UmVmPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvc1R5cGUvUmVzb3VyY2VSZWYjIiB4bXA6Q3JlYXRvclRvb2w9IkFkb2JlIFBob3Rvc2hvcCAyNS40IChXaW5kb3dzKSIgeG1wTU06SW5zdGFuY2VJRD0ieG1wLmlpZDpBQzA5OTlGOEQwOUExMUVFOEUxQ0U2MzZBRUI1MUVEMSIgeG1wTU06RG9jdW1lbnRJRD0ieG1wLmRpZDpBQzA5OTlGOUQwOUExMUVFOEUxQ0U2MzZBRUI1MUVEMSI+IDx4bXBNTTpEZXJpdmVkRnJvbSBzdFJlZjppbnN0YW5jZUlEPSJ4bXAuaWlkOkFDMDk5OUY2RDA5QTExRUU4RTFDRTYzNkFFQjUxRUQxIiBzdFJlZjpkb2N1bWVudElEPSJ4bXAuZGlkOkFDMDk5OUY3RDA5QTExRUU4RTFDRTYzNkFFQjUxRUQxIi8+IDwvcmRmOkRlc2NyaXB0aW9uPiA8L3JkZjpSREY+IDwveDp4bXBtZXRhPiA8P3hwYWNrZXQgZW5kPSJyIj8+/+4ADkFkb2JlAGTAAAAAAf/bAIQABgQEBAUEBgUFBgkGBQYJ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";
        
        public static string DisplayName(this IIdentity identity) => GetClaim(identity, "DisplayName");
        public static string DisplayName(this ClaimsPrincipal identity) => GetClaim(identity, "DisplayName");
        public static string GivenName(this ClaimsPrincipal identity) => GetClaim(identity, ClaimTypes.GivenName);
        public static string Email(this IIdentity identity) => GetClaim(identity, ClaimTypes.Email);
        public static string Email(this ClaimsPrincipal identity) => GetClaim(identity, ClaimTypes.Email);
        public static string Cn(this ClaimsPrincipal identity) => GetClaim(identity, "Cn");
        public static string Cn(this IIdentity identity) => GetClaim(identity, "Cn");
        
        public static string Photo(this ClaimsPrincipal identity)
        {
            var claim = identity.FindFirst("photo");

            if (claim == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(claim.Value))
                return string.Empty;

            if (claim.Value == "mock")
            {
                return MockAvatar;
            }

            var profilePhotoPath = Path.Combine(Directory.GetCurrentDirectory(), AzureProfilePhotoDir,
                claim.Value);

            return !File.Exists(profilePhotoPath) ? string.Empty : File.ReadAllText(profilePhotoPath);
        }

        public static void DeletePhoto(this ClaimsPrincipal identity)
        {
            var claim = identity.FindFirst("photo");
            if (claim == null) return;

            if (string.IsNullOrWhiteSpace(claim.Value)) return;

            var profilePhotoB64Path = Path.Combine(Directory.GetCurrentDirectory(), AzureProfilePhotoDir,
                claim.Value);

            if (File.Exists(profilePhotoB64Path)) File.Delete(profilePhotoB64Path);
        }

        private static string GetClaim(IIdentity? identity, string type)
        {
            var claim = (identity as ClaimsIdentity)?.FindFirst(type);
            return claim != null ? claim.Value : string.Empty;
        }

        private static string GetClaim(ClaimsPrincipal? identity, string type)
        {
            var claim = identity?.Claims.FirstOrDefault(x => x.Type == type) ?? identity?.FindFirst(x => x.Type == type);
            return claim != null ? claim.Value : string.Empty;
        }
    }
    #endif