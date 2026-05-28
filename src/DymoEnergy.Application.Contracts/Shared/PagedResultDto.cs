namespace DymoEnergy.Shared;

/// <remarks>Use <see cref="DymoPagedResultDto{T}"/> instead.</remarks>
public class PagedResultDto<T> : DymoPagedResultDto<T>
{
    public PagedResultDto() { }
    public PagedResultDto(long totalCount, System.Collections.Generic.List<T> items)
        : base(totalCount, items) { }
}
