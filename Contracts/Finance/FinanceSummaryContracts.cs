namespace EduBridge.Contracts.Finance;

public sealed record DashboardFinanceSummaryResponse(
    decimal TotalRevenue, 
    decimal TotalDebt,    
    int TotalInvoicesCreated, 
    int TotalInvoicesPaid     
);

public sealed record ClassDebtSummaryResponse(
    int ClassId,
    string ClassName,
    decimal TotalExpected, 
    decimal TotalCollected, 
    decimal TotalDebt,      
    int UnpaidStudentsCount 
);
