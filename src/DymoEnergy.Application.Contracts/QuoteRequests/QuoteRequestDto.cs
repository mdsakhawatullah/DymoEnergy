using System;
using Volo.Abp.Application.Dtos;

namespace DymoEnergy.QuoteRequests;

public class QuoteRequestDto : EntityDto<int>
{
    public string  Name     { get; set; } = string.Empty;
    public string? Phone    { get; set; }
    public string? Email    { get; set; }
    public string? Interest { get; set; }
    public string? Message  { get; set; }
    public QuoteRequestStatus Status { get; set; }
    public DateTime CreationTime { get; set; }
}
