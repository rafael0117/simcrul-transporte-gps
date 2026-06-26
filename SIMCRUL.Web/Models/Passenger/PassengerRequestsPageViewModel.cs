using SIMCRUL.Common.DTOs.Passenger;

namespace SIMCRUL.Web.Models.Passenger;

public class PassengerRequestsPageViewModel
{
    public PassengerRequestCreateDto Form { get; set; } = new();
    public List<PassengerRequestDto> Requests { get; set; } = new();
    public List<PassengerRouteOptionViewModel> Routes { get; set; } = new();
}
