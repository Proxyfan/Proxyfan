namespace Proxyfan.Domain.Traffic;

/// <summary>
///     Decoded HTTP Basic Authentication credentials per RFC 7617.
/// </summary>
public sealed class BasicAuthenticationCredentials
{
    /// <summary>
    ///     Gets the password (the portion after the first colon).
    /// </summary>
    public string Password { get; }

    /// <summary>
    ///     Gets the user name (the portion before the first colon).
    /// </summary>
    public string UserName { get; }

    /// <summary>
    ///     Initializes a new <see cref="BasicAuthenticationCredentials" />.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The password.</param>
    public BasicAuthenticationCredentials(string userName, string password)
    {
        UserName = userName;
        Password = password;
    }
}
