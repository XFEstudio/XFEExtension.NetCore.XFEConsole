using XFEExtension.NetCore.FormatExtension;

namespace XFEExtension.NetCore.XFEConsole;

internal static class XFEConsoleProtocol
{
    public const string MessageTypeKey = "MessageType";
    public const string AuthenticationMessage = "Authentication";
    public const string AuthenticatedKey = "Authenticated";
    public const string ReasonKey = "Reason";
    public const string ProgramNameKey = "ProgramName";
    public const string ProgramIdKey = "ProgramID";
    public const string TerminalNameHeader = "TerminalName";
    public const string TerminalIdHeader = "TerminalID";
    public const string PasswordHeader = "Password";

    public static string CreateAuthenticationMessage(bool authenticated, string reason, string programName, string programId) =>
        new XFEDictionary(
            MessageTypeKey, AuthenticationMessage,
            AuthenticatedKey, authenticated ? "true" : "false",
            ReasonKey, reason,
            ProgramNameKey, programName,
            ProgramIdKey, programId);

    public static string CreateOutputMessage(string message, bool isLine) =>
        new XFEDictionary("IsLine", isLine ? "true" : "false", "Text", message);
}
