using Microsoft.AspNetCore.Authorization;

namespace VibrationMonitorReservation.Controllers    
{
    public class AuthorizeAdminAttribute : AuthorizeAttribute
    {
        public AuthorizeAdminAttribute()
        {
            Policy = "IsAdmin";
        }
    }
    public class AuthorizeIsStorageHandlerAttribute : AuthorizeAttribute
    {
        public AuthorizeIsStorageHandlerAttribute()
        {
            Policy = "IsStorageHandler";
        }
    }
}
