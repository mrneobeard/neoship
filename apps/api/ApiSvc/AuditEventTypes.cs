namespace NeoShip.ApiSvc;

public static class AuditEventTypes
{
    public const string SignupSuccess = "auth.signup.success";
    public const string SignupFailed = "auth.signup.failed";

    public const string LoginSuccess = "auth.login.success";
    public const string LoginFailed = "auth.login.failed";

    public const string Logout = "auth.logout";

    public const string PasswordReset = "auth.password_reset";

    public const string EmailVerified = "auth.email_verified";

    public const string ApiKeyCreated = "api_key.created";
    public const string ApiKeyRevoked = "api_key.revoked";

    public const string ServiceAccountCreated = "service_account.created";
    public const string ServiceAccountUpdated = "service_account.updated";
    public const string ServiceAccountDisabled = "service_account.disabled";

    public const string TokenExchange = "auth.token_exchange";

    public const string SessionRevoked = "session.revoked";
}