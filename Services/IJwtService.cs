using VibrationMonitorReservation.Models;

namespace VibrationMonitorReservation.Services
{
    //  An interface that defines a contract for generating JWT tokens for an ApplicationUser. 
    //  This allows for easy implementation swapping and mocking during testing.
    public interface IJwtService
    {
        string GenerateJwtToken(ApplicationUser user, bool rememberMe);
    }
}
