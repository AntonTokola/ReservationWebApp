using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VibrationMonitorReservation.Models;
using VibrationMonitorReservation.Services;
using VibrationMonitorReservation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VibrationMonitorReservation.Dtos.AccountControllerDtos;

namespace VibrationMonitorReservation.Controllers
{
    //AccountController-luokka, joka hakee ja lisää käyttäjiä, sekä heidän tietojaan "AspNetUsers"-tauluun
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtSettings _jwtSettings;
        private readonly IJwtService _jwtService;
        private readonly EmailService _emailService;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IOptions<JwtSettings> jwtSettings, IJwtService jwtService, EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtSettings = jwtSettings.Value;
            _jwtService = jwtService;
            _emailService = emailService;
        }


        /// <summary>
        /// Käyttäjän kirjautuminen palveluun - [PUBLIC]
        /// </summary>
        /// <param name="model">Kirjautumistiedot</param>
        /// /// <remarks>
        /// Vastaukset:
        /// - OK 200: Onnistunut kirjautuminen palauttaa jwt-tokenin.
        /// - BadRequest 400: Syötedata on väärässä muodossa.
        /// - Unauthorized 401: Käyttäjänimi tai salasana on väärin.
        /// </remarks>
        //Käyttäjän kirjautuminen palveluun.
        [AllowAnonymous] // Tämä on Swaggerin debugausta varten
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Tämä virheilmoitus kertoo, että syötetyssä mallissa on ongelma, mutta ei paljasta, mikä ongelma on.
                return BadRequest(new { error = "Invalid input data." });
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            // Tarkistaa kirjautumisen onnistumisen ja palauttaa JWT-tokenin onnistuneessa tapauksessa.
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                return Ok(new { token = _jwtService.GenerateJwtToken(user, model.RememberMe) });
            }

            // Yleinen virheilmoitus, joka näytetään, kun kirjautuminen epäonnistuu.
            return Unauthorized(new { error = "Check your username and/or password." });
        }

        /// <summary>
        /// Salasanan vaihtotoiminto kirjautumattomille - [PUBLIC]
        /// </summary>
        /// <param name="model">Käyttäjätilin nimi</param>
        /// <remarks>
        /// Tätä toimintoa varten lisätään linkki kirjautumissivulle. Käyttäjä syöttää käyttäjätunnuksensa, jonka sähköpostiin uusi salasana lähetetään.
        /// 
        /// Vastaukset:
        /// - OK 200: Mikäli tili löytyy, ja prosessi onnistuu, onnistuneesta salasanan vaihdosta ilmoitetaan.
        /// - BadRequest 400: Syötedata on väärässä muodossa, väärä käyttäjänimi, tai jos operaatio epäonnistuu muutoin.
        /// </remarks>
        // Julkinen salasanan vaihto -metodi, jos käyttäjä on unohtanut salasanansa.
        [HttpPost("changePasswordPublic")]
        [AllowAnonymous]
        public async Task<IActionResult> changePasswordPublic(ChangePasswordPublicViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    return BadRequest("Invalid username");
                }

                // Generoidaan uusi salasana ja lähetetään se käyttäjälle sähköpostitse.
                GenerateEmailPassword generateEmailPassword = new GenerateEmailPassword();
                string newGeneratedPassword = generateEmailPassword.GenerateNewEmailPassword();

                var passwordValidator = new PasswordValidator<ApplicationUser>();
                var passwordValidationResult = await passwordValidator.ValidateAsync(_userManager, user, newGeneratedPassword);

                if (!passwordValidationResult.Succeeded)
                {
                    foreach (var error in passwordValidationResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return BadRequest("Error in the password change process");
                }

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, resetToken, newGeneratedPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return BadRequest("Error in the password change process");
                }

                string testSubject = "Salasanasi on vaihdettu.";
                string testBody = "Olet pyytänyt salasanan vaihtoa.\nTässä käyttäjätunnuksesi palveluun:\n\n" + user.Email + "\nVäliaikainen salasana palveluun: " + newGeneratedPassword + "\n\nKäy kirjautumassa näillä tunnuksilla järjestelmään, ja käy vaihtamassa salasana asetuksistasi.";
                List<string> toEmail = new List<string>();
                toEmail.Add(user.Email);

                _emailService.SendEmail(toEmail, testSubject, testBody);

                user.EmailConfirmed = false;
                await _userManager.UpdateAsync(user);

                return Ok(new { Message = "Password changed successfully. Check your email: '" + user.Email + "'" });
            }
            catch (Exception)
            {

                return BadRequest(ModelState);
            }
           
        }

        /// <summary>
        /// Salasanan vaihtotoiminto kirjautuneille - [AUTHORIZED]
        /// </summary>
        /// <param name="model">Käyttäjätilin nimi ja uusi salasana</param>
        /// /// <remarks>
        /// Salasanan vaihtotoiminto kirjautuneille käyttäjille. 
        /// Salasanan pakotettu vaihto automatisoitu ensimmäistä kertaa palveluun kirjautuville.
        /// 
        /// Vastaukset:
        /// - OK 200: Mikäli tili löytyy, ja prosessi onnistuu, onnistuneesta salasanan vaihdosta ilmoitetaan.
        /// - BadRequest 400: Syötedata on väärässä muodossa, väärä käyttäjänimi, tai jos operaatio epäonnistuu muutoin.
        /// </remarks>
        //Salasanan vaihto. Toiminto vain kirjautuneille (mm. juuri rekisteröityneille käyttäjille, ensimmäinen kirjautuminen generoidulla/sähköpostitetulla salasanalla)
        [HttpPost("changePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest("Invalid username");
            }

            var passwordValidator = new PasswordValidator<ApplicationUser>();
            var passwordValidationResult = await passwordValidator.ValidateAsync(_userManager, user, model.NewPassword);

            if (!passwordValidationResult.Succeeded)
            {
                foreach (var error in passwordValidationResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return BadRequest(ModelState);
            }

            // Vaihtaa käyttäjän salasanan, jos syötetyt tiedot ovat oikein.
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return BadRequest(ModelState);
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            return Ok(new { Message = "Password changed successfully"});
        }

        /// <summary>
        /// Kirjautuneena olevan käyttäjän tietojen haku - [AUTHORIZED]
        /// </summary>
        /// <remarks>
        /// Tämä mm. käyttäjätasojen tunnistamiseen frontendissä
        /// 
        /// Vastaukset:
        /// - OK 200: Mikäli tilin tiedot löytyvät.
        /// - BadRequest 400: Käyttäjätiliä ei löydy, tai prosessi epäonnistuu.
        /// </remarks>
        //Omien käyttäjätietojen haku. Käytetään mm. käyttäjätasojen tunnistamiseen frontendissä.
        [Authorize]
        [HttpGet("userinfo")]
        public async Task<ActionResult<UserInfoViewModel>> GetUserInfo()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    var userViewModel = new UserInfoViewModel
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        UserName = user.UserName,
                        Email = user.Email,
                        EmailConfirmed = user.EmailConfirmed,
                        PhoneNumber = user.PhoneNumber,
                        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                        IsStoragehandler = user.IsStorageHandler,
                        IsAdmin = user.IsAdmin,
                        EmailsActivated = user.EmailsActivated
                        
                    };

                    return userViewModel;
                }

                return NotFound();

            }
            catch (Exception)
            {

                return NotFound();
            }
        }

        /// <summary>
        /// Omien käyttäjätietojen ja sähköpostiviestiasetuksen päivittäminen - [AUTHORIZED]
        /// </summary>
        /// <param name="model">Syötteenä muutettavat käyttäjätilin tiedot ja asetukset. Jos joku palautettava string-muuttuja sisältää nullin, 
        /// tyhjän, tai välilyöntejä, tietoa ei tallenneta. 
        /// Jos käyttäjä määritellään "Admin"-tasolle, hänet määritellään automaattisesti myös "StorageHandleriksi".</param>
        /// /// <remarks>
        /// Vastaukset:
        /// - OK 200: Käyttäjätietojen ja asetusten muutos on onnistunut.
        /// - BadRequest 400: Käyttäjätiliä ei ole, tai operaatio epäonnistuu muutoin.
        /// </remarks>
        //Omien käyttäjätietojen päivittäminen
        [Authorize]
        [HttpPatch("updateOwnUserInfo")]
        public async Task<IActionResult> UpdateOwnUserInfo([FromBody] UpdateUserViewModel model)
        {
            if (ModelState.IsValid) {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
                        user.PhoneNumber = model.PhoneNumber;

                    if (!string.IsNullOrWhiteSpace(model.FirstName))
                        user.FirstName = model.FirstName;

                    if (!string.IsNullOrWhiteSpace(model.LastName))
                        user.LastName = model.LastName;

                    //Jos joku näistä on arvoltaan 'null', kantaan tallennetaan 'false'
                    user.IsAdmin = model.IsAdmin ?? false;
                    user.IsStorageHandler = model.IsStoragehandler ?? false;
                    user.EmailsActivated = model.EmailsActivated ?? false;

                    //Jos käyttäjä on admin-tason käyttäjä -> hän on automaattisesti myös varastokäsittelijä-tason käyttäjä
                    if (user.IsAdmin == true)
                    {
                        user.IsStorageHandler = true;
                    }

                    var result = await _userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        return Ok("The settings has been changed successfully");
                    }
                    else
                    {
                        return BadRequest("Error to define the settings");
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            return BadRequest(ModelState);
        }



        /// <summary>
        /// Uuden käyttäjän rekisteröinti admin-käyttäjän toimesta. - [AUTHORIZED - ADMIN]
        /// </summary>
        /// <remarks>
        /// Käyttäjälle generoidaan salasana, joka lähetetään käyttäjän sähköpostiin.
        /// 
        /// Vastaukset:
        /// - OK 200: Uuden käyttäjän rekisteröinti onnistui ja väliaikainen salasana on lähetetty sähköpostilla.
        /// - BadRequest 400: Jos rekisteröinti epäonnistuu tai jos syötetyt tiedot ovat virheellisiä.
        /// </remarks>
        // Uuden käyttäjän rekisteröinti admin-käyttäjän toimesta. 
        //Salasana generoidaan, ja lähetetään sähköpostilla rekisteröidylle käyttäjälle.

        [HttpPost("admin/registerUser")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> Register(RegistrationViewModel model)
        {


            if (ModelState.IsValid)
            {
                if (model.IsAdmin)
                {
                    model.IsStorageHandler = true;
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    IsStorageHandler = model.IsStorageHandler,
                    IsAdmin = model.IsAdmin,
                    EmailsActivated = true
                    
                };

                GenerateEmailPassword generateEmailPassword = new GenerateEmailPassword();

                string userPassword = generateEmailPassword.GenerateNewEmailPassword();


                //var result = await _userManager.CreateAsync(user, randomizedEmailPassword); ;
                var result = await _userManager.CreateAsync(user, userPassword);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    string testSubject = "Tervetuloa varauspalveluun.";
                    string testBody = "Tässä käyttäjätunnuksesi palveluun:\n\n" + user.Email + "\nVäliaikainen salasana palveluun: " + userPassword + "\n\nKäy kirjautumassa näillä tunnuksilla järjestelmään, ja vaihda salasana ensimmäisen kirjautumisen yhteydessä.";
                    List<string> toEmail = new List<string>();
                    toEmail.Add(user.Email);

                    _emailService.SendEmail(toEmail, testSubject, testBody);


                    return Ok(new { Message = "New user created successfully. A temporary password has been sent to the email address: '" + user.UserName + "'" });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return BadRequest(ModelState);
        }

        /// <summary>
        /// Hakee kaikkien käyttäjien yksityiskohtaiset tiedot. Toiminto admineille - [AUTHORIZED - ADMIN]
        /// </summary>
        //Hakee kaikkien käyttäjien yksityiskohtaiset tiedot. Toiminto admineille.
        [HttpGet("admin/getAllUsers")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<ActionResult<IEnumerable<GetAllUsersDto>>> GetAllUsers()
        {
            try
            {
                var allUsersDb = _userManager.Users.ToList();
                List<GetAllUsersDto> allUsersList = new List<GetAllUsersDto>();

                foreach (var user in allUsersDb)
                {
                    GetAllUsersDto getAllUsersDto = new GetAllUsersDto();
                    getAllUsersDto.Id = user.Id;
                    getAllUsersDto.UserName = user.UserName;
                    getAllUsersDto.Email = user.Email;
                    getAllUsersDto.PhoneNumber = user.PhoneNumber;
                    getAllUsersDto.FirstName = user.FirstName;
                    getAllUsersDto.LastName = user.LastName;                    
                    getAllUsersDto.IsAdmin = user.IsAdmin;
                    getAllUsersDto.IsStorageHandler = user.IsStorageHandler;
                    allUsersList.Add(getAllUsersDto);
                }
                return Ok(allUsersList);
            }
            catch (Exception)
            {

                return BadRequest("User data search was not successful");
            }

        }

        /// <summary>
        /// Käyttäjätilin poistotoiminto admineille. [AUTHORIZED - ADMIN]
        /// </summary>
        /// /// <remarks>
        /// Parametrina annettava käyttäjä-id.
        /// </remarks>
        /// <param name="id">Käyttäjän ID, joka poistetaan.</param>
        //Käyttäjätilin poistotoiminto admineille. Parametrina annettava käyttäjä-id.
        [HttpDelete("admin/{id}")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.Users.Where(u => u.Id == id).Distinct().FirstOrDefaultAsync();
                if (user != null)
                { 
                    await _userManager.DeleteAsync(user);
                    return Ok(new { Message = "User '" + user.UserName + "' deleted successfully" });
                }
                return NotFound();
            }
            catch (Exception)
            {

                return BadRequest();
            }
            
        }

        /// <summary>
        /// Käyttäjätietojen päivitystoiminto admineille. [AUTHORIZED - ADMIN]
        /// </summary>
        /// <param name="updateUser">Päivitettävät käyttäjätiedot DTO-muodossa.</param>
        /// <remarks>
        /// Vastaukset:
        /// - OK 200: Käyttäjän tietojen päivitys onnistui.
        /// - NotFound 404: Käyttäjätiliä ei löydy annetulla ID:llä.
        /// - BadRequest 400: Jos päivitys epäonnistuu.
        /// </remarks>
        //Käyttäjätietojen päivitys admineille. Parametrina annettava käyttäjä-id.
        [HttpPatch("admin/updateUser")]
        [Authorize(Policy = "IsAdmin")]
        public async Task<ActionResult> UpdateUser([FromBody] UpdateUserDto updateUser)
        {
            try
            {
                var userProperties = await _userManager.Users.Where(u => u.Id == updateUser.Id).FirstOrDefaultAsync();
                if (userProperties != null)
                {
                    if (!string.IsNullOrWhiteSpace(updateUser.FirstName)) { userProperties.FirstName = updateUser.FirstName; }
                    if (!string.IsNullOrWhiteSpace(updateUser.LastName)) { userProperties.LastName = updateUser.LastName; }
                    if (!string.IsNullOrWhiteSpace(updateUser.PhoneNumber)) { userProperties.PhoneNumber = updateUser.PhoneNumber; }

                    //Jos jompikumpi on arvoltaan 'null', kantaan tallennetaan 'false'
                    userProperties.IsAdmin = updateUser.IsAdmin ?? false;
                    userProperties.IsStorageHandler = updateUser.IsStorageHandler ?? false;

                    //Jos käyttäjä on admin-tason käyttäjä -> hän on automaattisesti myös varastokäsittelijä-tason käyttäjä
                    if (userProperties.IsAdmin == true)
                    {
                        userProperties.IsStorageHandler = true;
                    }
                    await _userManager.UpdateAsync(userProperties);
                    

                    return Ok("User '" + userProperties.FirstName + " " + userProperties.LastName + "' updated");
                }
                return NotFound("User id: '" + updateUser.Id + "' not found");
            }
            catch (Exception)
            {

                return BadRequest();
            }
            
        }
    } 
}