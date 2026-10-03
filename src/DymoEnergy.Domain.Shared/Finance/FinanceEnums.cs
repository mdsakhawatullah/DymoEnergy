namespace DymoEnergy.Finance;

public static class FinanceConsts
{
    public const int MaxName  = 256;
    public const int MaxShort = 128;
    public const int MaxText  = 2000;
    public const int MaxColor = 16;
    public const int MaxUrl   = 1024;
}

public enum FinanceAccountKind
{
    Cash         = 1,
    MobileWallet = 2,
    Bank         = 3,
}

public enum FinanceDirection
{
    In  = 1,
    Out = 2,
}

/// <summary>Why a ledger row exists. Transfers move money between own accounts and are not income or cost.</summary>
public enum FinanceTxSource
{
    Manual       = 0,
    SupplierBill = 1,
    Expense      = 2,
    InTransit    = 3,
    Transfer     = 4,
}

public enum FinanceCostGroup
{
    /// <summary>Cost of what was sold: counted before gross profit.</summary>
    CostOfSales = 1,
    /// <summary>Running costs: counted after gross profit.</summary>
    Running     = 2,
}

/// <summary>Which editable list a <c>FinanceListItem</c> belongs to.</summary>
public enum FinanceItemKind
{
    InTransit      = 1,
    Advance        = 2,
    LetterOfCredit = 3,
    DealerCredit   = 4,
    Insight        = 5,
}
