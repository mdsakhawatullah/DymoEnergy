namespace DymoEnergy.Shared;

/// <remarks>Use <see cref="DymoPagedResultDto{T}"/> instead.</remarks>
public class NextXPagedResultDto<T> : DymoPagedResultDto<T>
{
    public NextXPagedResultDto() { }
    public NextXPagedResultDto(long totalCount, System.Collections.Generic.List<T> items)
        : base(totalCount, items) { }
}
