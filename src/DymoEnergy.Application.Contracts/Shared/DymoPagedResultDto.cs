using System.Collections.Generic;

namespace DymoEnergy.Shared;

/// <summary>Standard paged response wrapper used across all list endpoints.</summary>
public class DymoPagedResultDto<T>
{
    public long    TotalCount { get; set; }
    public List<T> Items      { get; set; } = new();

    public DymoPagedResultDto() { }

    public DymoPagedResultDto(long totalCount, List<T> items)
    {
        TotalCount = totalCount;
        Items      = items;
    }
}
