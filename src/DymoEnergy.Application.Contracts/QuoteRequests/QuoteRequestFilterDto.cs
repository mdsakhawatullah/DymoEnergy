using Volo.Abp.Application.Dtos;

namespace DymoEnergy.QuoteRequests;

public class QuoteRequestFilterDto : PagedAndSortedResultRequestDto
{
    public string?             Filter { get; set; }
    public QuoteRequestStatus? Status { get; set; }
}
