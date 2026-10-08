namespace BloodBankSystem.Services;

public class StockOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static StockOperationResult Ok(string message = "Operation completed successfully.") 
        => new() { Success = true, Message = message };

    public static StockOperationResult Fail(string message) 
        => new() { Success = false, Message = message };
}

public interface IStockService
{
    Task<StockOperationResult> AddDonationAsync(
        int donorId, 
        int bloodGroupId, 
        int units, 
        DateTime donationDate, 
        string? remarks = null, 
        int? scheduledDonationId = null);

    Task<StockOperationResult> IssueRequestAsync(int requestId, string? remarks = null);

    Task<StockOperationResult> RejectRequestAsync(int requestId, string adminRemarks);

    Task<StockOperationResult> AdjustAsync(int bloodGroupId, int unitsDelta, string reason);
}
