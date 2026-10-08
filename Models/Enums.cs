namespace BloodBankSystem.Models;

public enum DonationStatus
{
    Scheduled,
    Completed,
    Rejected,
    Cancelled
}

public enum RequestUrgency
{
    Normal,
    Urgent,
    Critical
}

public enum RequestStatus
{
    Pending,
    Approved,
    Rejected,
    Issued,
    Cancelled
}

public enum TransactionType
{
    DonationIn,
    IssueOut,
    Adjustment
}
