namespace TicketBooking.Application.Common.Dtos;

public record CreateConcertDto(
    string Name, 
    DateTime Date, 
    string PosterImageUrl, 
    decimal VipPrice, 
    decimal GaPrice
);