using Microsoft.Identity.Client.Extensions.Msal;

namespace CommandConfiguration;

public static class AppConfiguration
{
    // App settings
    public const string AUTHORITY = "https://login.microsoftonline.com/common";
    public const string CLIENT_ID = "14d82eec-204b-4c2f-b7e8-296a70dab67e";
    public const string REDIRECT_URI = "http://localhost";
    public const string GRAPH_URL = "https://graph.microsoft.com/v1.0/";
    public static readonly string[] GRAPH_INTERACTIVE_SCOPE = new string[] {"https://graph.microsoft.com//.default" };

    // Cache settings
    public const string CACHE_FILE_NAME = "msal-cache.plaintext";
    public const string CACHE_DIR_NAME = ".intuneassistant";
    public static readonly string CacheDir = Path.Combine(MsalCacheHelper.UserRootDirectory, CACHE_DIR_NAME);

    public const string KEY_CHAIN_SERVICE_NAME = "intuneassistant_msal_service";
    public const string KEY_CHAIN_ACCOUNT_NAME = "intuneassistant_msal_account";

    public const string LINUX_KEY_RING_SCHEMA = "com.srozemuller.intuneassistant.tokencache";
    public const string LINUX_KEY_RING_COLLECTION = MsalCacheHelper.LinuxKeyRingDefaultCollection;
    public const string LINUX_KEY_RING_LABEL = "MSAL token cache for Intune Assistant CLI.";
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new("Version", "1");
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new("ProductGroup", "MyApps");
}