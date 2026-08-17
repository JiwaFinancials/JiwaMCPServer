namespace JiwaMcpServer.ToolMetadata;

public sealed class ToolDescriptionComposer
{
    public ToolDescriptionComposer(BusinessTerminologyRegistry terminologyRegistry)
    {
        ArgumentNullException.ThrowIfNull(terminologyRegistry);
    }

    public string Compose(BusinessToolDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var description = FirstSentence(descriptor.Description);
        if (string.IsNullOrWhiteSpace(description))
        {
            description = BuildFallbackDescription(descriptor);
        }

        description = AppendEntityDisambiguation(descriptor, description);
        description = AppendWorkflowGuidance(descriptor, description);

        if (ToolAliasRegistry.TryGetCanonicalName(descriptor.ToolName, out var canonicalToolName))
        {
            return $"{description} Use {canonicalToolName}.";
        }

        return description;
    }

    private static string BuildFallbackDescription(BusinessToolDescriptor descriptor)
    {
        var action = string.IsNullOrWhiteSpace(descriptor.Metadata.ActionType)
            ? "Use"
            : descriptor.Metadata.ActionType.Trim();
        var entity = string.IsNullOrWhiteSpace(descriptor.Metadata.EntityType)
            ? descriptor.ToolName
            : descriptor.Metadata.EntityType.Trim();

        return $"{action} {SplitWords(entity)}.";
    }

    private static string AppendEntityDisambiguation(BusinessToolDescriptor descriptor, string description)
    {
        var entityType = descriptor.Metadata.EntityType;
        var actionType = descriptor.Metadata.ActionType;

        if (!string.Equals(actionType, "Create", StringComparison.OrdinalIgnoreCase))
        {
            return description;
        }

        var hint = entityType switch
        {
            "PurchaseOrder" => "This is a purchase order (PO), not a supplier invoice or creditor purchase",
            "CreditorPurchase" => "This is a supplier invoice (creditor purchase), not a purchase order (PO)",
            _ => null
        };

        if (string.IsNullOrWhiteSpace(hint))
        {
            return description;
        }

        var trimmed = description.Trim();
        if (trimmed.EndsWith('.'))
        {
            trimmed = trimmed[..^1];
        }

        return $"{trimmed}. {hint}.";
    }

    private static string AppendWorkflowGuidance(BusinessToolDescriptor descriptor, string description)
    {
        var guidance = descriptor.ToolName switch
        {
            "DocumentIngest" => "Use this first for uploaded PDFs, Word documents, or images to obtain a documentId for extraction",
            "DocumentExtractInvoice" => "Use this after DocumentIngest with the returned documentId to extract invoice fields",
            _ => null
        };

        if (string.IsNullOrWhiteSpace(guidance))
        {
            return description;
        }

        var trimmed = description.Trim();
        if (trimmed.EndsWith('.'))
        {
            trimmed = trimmed[..^1];
        }

        return $"{trimmed}. {guidance}.";
    }

    private static string FirstSentence(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        var trimmed = description.Trim();
        var index = trimmed.IndexOf('.');
        return index >= 0 ? trimmed[..(index + 1)] : trimmed;
    }

    private static string SplitWords(string value)
        => System.Text.RegularExpressions.Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");
}
