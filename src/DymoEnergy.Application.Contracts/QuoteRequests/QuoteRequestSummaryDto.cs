using System.Collections.Generic;

namespace DymoEnergy.QuoteRequests;

/// <summary>Tab counts + system-type options for the admin list.</summary>
public class QuoteRequestSummaryDto
{
    public int AllCount       { get; set; }
    public int NewCount       { get; set; }
    public int ContactedCount { get; set; }
    public int QuotedCount    { get; set; }
    public int ClosedCount    { get; set; }
    public List<string> SystemTypes { get; set; } = new();
}
