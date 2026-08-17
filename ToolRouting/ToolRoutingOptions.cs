using Microsoft.Extensions.Options;

namespace JiwaMcpServer.ToolRouting;

public sealed class ToolRoutingOptions
{
    public const string SectionName = "ToolRouting";

    public bool EnableRouting { get; set; } = true;

    public bool EnableDomainClassification { get; set; } = true;

    public int RetrievalTopK { get; set; } = 20;

    public double SimilarityThreshold { get; set; } = 0.70;

    public int MaxSelectedTools { get; set; } = 5;

    public string EmbeddingProvider { get; set; } = "LocalHash";

    public string VectorStoreProvider { get; set; } = "InMemory";

    public string[] Domains { get; set; } =
    [
        "CRM",
        "ERP",
        "Procurement",
        "Finance",
        "Reporting",
        "HR",
        "Projects",
        "Documents",
        "Knowledge Base",
        "Email",
        "Calendar",
        "Customer Support"
    ];

    public int EmbeddingDimensions { get; set; } = 768;
}

public sealed class ToolRoutingOptionsValidator : IValidateOptions<ToolRoutingOptions>
{
    public ValidateOptionsResult Validate(string? name, ToolRoutingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.RetrievalTopK <= 0)
        {
            return ValidateOptionsResult.Fail("ToolRouting:RetrievalTopK must be greater than zero.");
        }

        if (options.MaxSelectedTools <= 0)
        {
            return ValidateOptionsResult.Fail("ToolRouting:MaxSelectedTools must be greater than zero.");
        }

        if (options.MaxSelectedTools > options.RetrievalTopK)
        {
            return ValidateOptionsResult.Fail("ToolRouting:MaxSelectedTools cannot exceed RetrievalTopK.");
        }

        if (options.SimilarityThreshold is < 0 or > 1)
        {
            return ValidateOptionsResult.Fail("ToolRouting:SimilarityThreshold must be between 0 and 1.");
        }

        if (options.EmbeddingDimensions < 64)
        {
            return ValidateOptionsResult.Fail("ToolRouting:EmbeddingDimensions must be at least 64.");
        }

        return ValidateOptionsResult.Success;
    }
}
