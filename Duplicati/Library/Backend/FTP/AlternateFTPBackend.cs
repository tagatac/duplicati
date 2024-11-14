using System;
using System.Collections.Generic;
using System.Security.Authentication;
using Duplicati.Library.Interface;
using FluentFTP;

namespace Duplicati.Library.Backend;


/// <summary>
/// This classes exists to provide a way to use the FTP backend with the alternate name "AlternateFTPBackend",
/// and supporting all existing configured backups that were created using the "aftp" prefix.
///
/// The functionality is identical to the FTP backend
/// </summary>
public class AlternateFTPBackend: FTP
{
    /* 
        These constants are overriden to provide transparent access to the alternate configuration keys
    */
    protected override string CONFIG_KEY_FTP_ENCRYPTION_MODE => "aftp-encryption-mode";
    protected override string CONFIG_KEY_FTP_DATA_CONNECTION_TYPE => "aftp-data-connection-type";
    protected override string CONFIG_KEY_FTP_SSL_PROTOCOLS => "aftp-ssl-protocols";
    protected override string CONFIG_KEY_FTP_UPLOAD_DELAY => "aftp-upload-delay";
    protected override string CONFIG_KEY_FTP_LOGTOCONSOLE => "aftp-log-to-console";
    protected override string CONFIG_KEY_FTP_LOGPRIVATEINFOTOCONSOLE => "aftp-log-privateinfo-to-console";
    
    public override string Description => Strings.DescriptionAlternate;
    
    public override string DisplayName => Strings.DisplayNameAlternate;

    /*
     * Likewise, this constant is overriden to provide transparent access to the alternate protocol key
    */
    public override string ProtocolKey { get; } = "aftp";

    public AlternateFTPBackend()
    {
        
    }
    public AlternateFTPBackend(string url, Dictionary<string, string> options): base(url, options)
    {
    }
    
    /// <summary>
    /// Overriden so that for the alternate FTP backend, we need to support the alternate config keys starting with "aftp"
    /// </summary>
    public override IList<ICommandLineArgument> SupportedCommands =>
    new List<ICommandLineArgument>([
        new CommandLineArgument("auth-password", CommandLineArgument.ArgumentType.Password, Strings.DescriptionAuthPasswordShort, Strings.DescriptionAuthPasswordLong),
        new CommandLineArgument("auth-username", CommandLineArgument.ArgumentType.String, Strings.DescriptionAuthUsernameShort, Strings.DescriptionAuthUsernameLong),
        new CommandLineArgument("disable-upload-verify", CommandLineArgument.ArgumentType.Boolean, Strings.DescriptionDisableUploadVerifyShort, Strings.DescriptionDisableUploadVerifyLong),
        new CommandLineArgument(CONFIG_KEY_FTP_DATA_CONNECTION_TYPE, CommandLineArgument.ArgumentType.Enumeration, Strings.DescriptionFtpDataConnectionTypeShort, Strings.DescriptionFtpDataConnectionTypeLong, DEFAULT_DATA_CONNECTION_TYPE_STRING, null, Enum.GetNames(typeof(FtpDataConnectionType))),
        new CommandLineArgument(CONFIG_KEY_FTP_ENCRYPTION_MODE, CommandLineArgument.ArgumentType.Enumeration, Strings.DescriptionFtpEncryptionModeShort, Strings.DescriptionFtpEncryptionModeLong, DEFAULT_ENCRYPTION_MODE_STRING, null, Enum.GetNames(typeof(FtpEncryptionMode))),
        new CommandLineArgument(CONFIG_KEY_FTP_SSL_PROTOCOLS, CommandLineArgument.ArgumentType.Flags, Strings.DescriptionSslProtocolsShort, Strings.DescriptionSslProtocolsLong, DEFAULT_SSL_PROTOCOLS_STRING, null, Enum.GetNames(typeof(SslProtocols))),
        new CommandLineArgument(CONFIG_KEY_FTP_UPLOAD_DELAY, CommandLineArgument.ArgumentType.Timespan, Strings.DescriptionUploadDelayShort, Strings.DescriptionUploadDelayLong, DEFAULT_UPLOAD_DELAY_STRING),
        new CommandLineArgument(CONFIG_KEY_FTP_LOGTOCONSOLE, CommandLineArgument.ArgumentType.Boolean, Strings.DescriptionLogToConsoleShort, Strings.DescriptionLogToConsoleLong),
        new CommandLineArgument(CONFIG_KEY_FTP_LOGPRIVATEINFOTOCONSOLE, CommandLineArgument.ArgumentType.Boolean, Strings.DescriptionLogPrivateInfoToConsoleShort, Strings.DescriptionLogPrivateInfoToConsoleLong, "false"),
    ]);
}