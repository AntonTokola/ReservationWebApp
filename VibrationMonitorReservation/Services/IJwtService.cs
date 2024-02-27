using VibrationMonitorReservation.Models;

namespace VibrationMonitorReservation.Services
{
//Interface JWT-token palvelulle
    public interface IJwtService
    {
        string GenerateJwtToken(ApplicationUser user, bool rememberMe);
    }
}
