namespace DymoEnergy.Compliance;

public static class ComplianceConsts
{
    public const int MaxName  = 256;
    public const int MaxShort = 128;
    public const int MaxText  = 2000;
    public const int MaxColor = 16;
    public const int MaxUrl   = 1024;
}

/// <summary>Which editable list a <c>ComplianceListItem</c> belongs to.</summary>
public enum ComplianceItemKind
{
    Badge        = 1,
    DocType      = 2,
    Template     = 3,
    Retention    = 4,
    AccessRule   = 5,
    Reminder     = 6,
    FilingRecord = 7,
    ImportPaper  = 8,
}

public enum ComplianceFilingStatus
{
    NotStarted = 0,
    InProgress = 1,
    Filed      = 2,
    UpToDate   = 3,
}

/// <summary>Cell state in the "document pack per project" grid.</summary>
public enum ComplianceDocStatus
{
    Pending    = 0,
    Done       = 1,
    InProgress = 2,
    Issue      = 3,
}

public enum ComplianceFileOwner
{
    Licence     = 1,
    Certificate = 2,
    Template    = 3,
    ImportPaper = 4,
    Filing      = 5,
}
