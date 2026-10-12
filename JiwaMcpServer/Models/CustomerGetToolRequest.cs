using System.ComponentModel;

namespace JiwaMcpServer.Models;

public sealed class CustomerGetToolRequest
{
    [Description("The Jiwa DebtorID when it is already known. Leave empty when the user supplied an account number or customer name instead.")]
    public string? DebtorID { get; set; }

    [Description("A user-supplied customer reference such as AccountNo, AltAccountNo, debtor number, customer code, or customer name. Use this for prompts like 'show debtor 1149' or 'show debtor Armadale Shire Council'.")]
    public string? Identifier { get; set; }
}
