using Fido2NetLib;

namespace NeoShip.ApiSvc.Tests;

internal static class TestFido2
{
    public static Fido2 Create()
    {
        return new Fido2(new Fido2Configuration
        {
            ServerDomain = "localhost",
            ServerName = "NeoShip Tests",
            Origins = new HashSet<string> { "https://localhost" },
        }, metadataService: null);
    }
}