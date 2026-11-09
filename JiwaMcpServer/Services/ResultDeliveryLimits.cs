namespace JiwaMcpServer.Services;

public sealed class ResultDeliveryLimits
{
    public int MaxQueryRows { get; set; } = 10_000;
    public int MaxResultBytes { get; set; } = 16 * 1024 * 1024;
    public int QueryTimeoutSeconds { get; set; } = 30;
    public int MaxChatRows { get; set; } = 500;
    public int MaxChatBytes { get; set; } = 48_000;
    public int PageRows { get; set; } = 100;
    public int PageBytes { get; set; } = 8_000;
    public int MaxStoredResults { get; set; } = 50;
    public long MaxStoredBytes { get; set; } = 64 * 1024 * 1024;
    public int RetentionMinutes { get; set; } = 15;

    public void Validate()
    {
        if (MaxQueryRows <= 0 || MaxResultBytes <= 0 || QueryTimeoutSeconds <= 0
            || MaxChatRows <= 0 || MaxChatBytes <= 0 || PageRows <= 0 || PageBytes < 2_048
            || MaxStoredResults <= 0 || MaxStoredBytes < MaxResultBytes || RetentionMinutes <= 0
            || PageRows > MaxChatRows || MaxChatRows > MaxQueryRows || PageBytes > MaxChatBytes
            || MaxChatBytes > MaxResultBytes)
            throw new InvalidOperationException("ResultDelivery limits must be positive and ordered: page <= chat <= query; storage must fit a result. PageBytes must be at least 2048.");
    }
}
