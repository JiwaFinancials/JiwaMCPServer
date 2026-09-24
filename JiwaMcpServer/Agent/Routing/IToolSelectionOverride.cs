using JiwaMcpServer.Agent.Catalog;
using JiwaMcpServer.Agent.Models;

namespace JiwaMcpServer.Agent.Routing;

public interface IToolSelectionOverride
{
    void Apply(ToolSelectionContext context);
}

public sealed class ToolSelectionContext
{
    private readonly IReadOnlyDictionary<string, ToolDefinition> _availableToolsByName;
    private readonly List<ToolSelectionCandidate> _selectedTools;
    private readonly List<string> _adjustments = [];

    public ToolSelectionContext(
        string conversationId,
        string prompt,
        string conversationSummary,
        string conversationContext,
        IntentContext intent,
        WorkingContext workingContext,
        IReadOnlyList<string> resolvedDomains,
        IReadOnlyList<ToolDefinition> candidateTools,
        IReadOnlyList<ToolDefinition> availableTools,
        IEnumerable<ToolSelectionCandidate> selectedTools,
        int maxTools)
    {
        ConversationId = conversationId ?? string.Empty;
        Prompt = prompt ?? string.Empty;
        ConversationSummary = conversationSummary ?? string.Empty;
        ConversationContext = conversationContext ?? string.Empty;
        Intent = intent ?? new IntentContext();
        WorkingContext = workingContext ?? new WorkingContext();
        ResolvedDomains = resolvedDomains?.ToList() ?? [];
        CandidateTools = candidateTools?.ToList() ?? [];
        AvailableTools = availableTools?.ToList() ?? [];
        _availableToolsByName = AvailableTools.ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);
        _selectedTools = selectedTools?.ToList() ?? [];
        MaxTools = maxTools <= 0 ? 20 : maxTools;
    }

    public string ConversationId { get; }

    public string Prompt { get; }

    public string ConversationSummary { get; }

    public string ConversationContext { get; }

    public IntentContext Intent { get; }

    public WorkingContext WorkingContext { get; }

    public IReadOnlyList<string> ResolvedDomains { get; }

    public IReadOnlyList<ToolDefinition> CandidateTools { get; }

    public IReadOnlyList<ToolDefinition> AvailableTools { get; }

    public IReadOnlyList<ToolSelectionCandidate> SelectedTools => _selectedTools;

    public IReadOnlyList<string> Adjustments => _adjustments;

    public int MaxTools { get; }

    public bool ContainsTool(string toolName)
        => IndexOfSelectedTool(toolName) >= 0;

    public bool RemoveTool(string toolName)
    {
        var index = IndexOfSelectedTool(toolName);
        if (index < 0)
            return false;

        _selectedTools.RemoveAt(index);
        return true;
    }

    public void PreferTool(string toolName, double? relevanceScore = null)
    {
        var candidate = CreateCandidate(toolName, relevanceScore ?? GetNextTopScore());
        var existingIndex = IndexOfSelectedTool(toolName);
        if (existingIndex >= 0)
            _selectedTools.RemoveAt(existingIndex);

        _selectedTools.Insert(0, candidate);
    }

    public void AppendTool(string toolName, double? relevanceScore = null)
    {
        var existingIndex = IndexOfSelectedTool(toolName);
        if (existingIndex >= 0)
            return;

        _selectedTools.Add(CreateCandidate(toolName, relevanceScore ?? GetNextBottomScore()));
    }

    public void ReplaceSelection(IEnumerable<string> toolNames, double? relevanceScore = null)
    {
        ArgumentNullException.ThrowIfNull(toolNames);

        var distinctNames = toolNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _selectedTools.Clear();

        for (var i = 0; i < distinctNames.Count; i++)
        {
            var score = relevanceScore ?? (distinctNames.Count - i);
            _selectedTools.Add(CreateCandidate(distinctNames[i], score));
        }
    }

    public void AddAdjustment(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Adjustment description cannot be blank.", nameof(description));

        _adjustments.Add(description.Trim());
    }

    internal IReadOnlyList<ToolSelectionCandidate> BuildSelection()
        => _selectedTools.Take(MaxTools).ToList();

    private ToolSelectionCandidate CreateCandidate(string toolName, double relevanceScore)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        if (!_availableToolsByName.TryGetValue(toolName, out var tool))
            throw new InvalidOperationException($"Tool '{toolName}' is not available for selection override.");

        return new ToolSelectionCandidate
        {
            Tool = tool,
            RelevanceScore = relevanceScore
        };
    }

    private int IndexOfSelectedTool(string toolName)
        => _selectedTools.FindIndex(item => item.Tool.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));

    private double GetNextTopScore()
        => _selectedTools.Count == 0 ? 1d : _selectedTools.Max(item => item.RelevanceScore) + 1d;

    private double GetNextBottomScore()
        => _selectedTools.Count == 0 ? 0d : _selectedTools.Min(item => item.RelevanceScore) - 1d;
}

public sealed class ToolSelectionCandidate
{
    public required ToolDefinition Tool { get; init; }

    public double RelevanceScore { get; init; }
}
