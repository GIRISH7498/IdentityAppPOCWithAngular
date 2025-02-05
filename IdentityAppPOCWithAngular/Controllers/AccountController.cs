using IdentityAppPOCWithAngular.DTOs.Account;
using IdentityAppPOCWithAngular.Models;
using IdentityAppPOCWithAngular.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace IdentityAppPOCWithAngular.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly JWTService _jWTService;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public AccountController(JWTService jWTService, 
            SignInManager<User> signInManager, 
            UserManager<User> userManager,
            EmailService emailService,
            IConfiguration configuration)
        {
            _jWTService = jWTService;
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
        }

        [Authorize]
        [HttpGet("refresh-user-token")]
        public async Task<ActionResult<UserDto>> RefreshUserToken()
        {
            var user = await _userManager.FindByNameAsync(User.FindFirst(ClaimTypes.Email)?.Value);
            return CreateApplicationUserDto(user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            if(user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            if(user.EmailConfirmed == false)
            {
                return Unauthorized("Please confirm your email.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
            {
                return Unauthorized("Invalid username and password.");
            }

            return CreateApplicationUserDto(user);
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegisterDto>> Register(RegisterDto registerDto)
        {
            if(await CheckEmailExistAsync(registerDto.Email))
            {
                return BadRequest($"An existing account is using {registerDto.Email}, email address. Please try with another email address");
            }

            var userToAdd = new User
            {
                FirstName = registerDto.FirstName.ToLower(),
                LastName = registerDto.LastName.ToLower(),
                UserName = registerDto.Email.ToLower(),
                Email = registerDto.Email.ToLower()
            };

            var result = await _userManager.CreateAsync(userToAdd, registerDto.Password);
            
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            try
            {
                if( await SendConfirmEMailAsync(userToAdd))
                {
                    return Ok(new JsonResult(new { title = "Account Created", message = "Your account has been created, you can login." }));
                }
                return BadRequest("Failed to send email. Please contact admin.");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to send email. Please contact admin.");
            }
        }

        [HttpPut("confirm-email")]
        public async Task<ActionResult> ConfirmEmail(ConfirmEmailDto confirmEmailDto)
        {
            var user = await _userManager.FindByEmailAsync(confirmEmailDto.Email);  
            if (user == null)
            {
                return BadRequest("This email address has not been registered yet.");
            }
            if(user.EmailConfirmed == true)
            {
                return BadRequest("This email address already confirmed please login.");
            }

            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(confirmEmailDto.Token);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if(result.Succeeded)
                {
                    return Ok(new JsonResult(new { title = "Email Confirmed", message = "Your email address is confirmed, you can login." }));
                }

                return BadRequest("Invalid toke, Please try again") ;
            }
            catch (Exception ex)
            {
                return BadRequest("Invalid toke, Please try again");
            }
        }

        [HttpPut("resend-email-confirmation-link/{email}")]
        public async Task<ActionResult> ResendEmailLinkForConfirmation(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Please enter Email address.");
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return BadRequest("This email address is not exist while registration.");
            }

            if(user.EmailConfirmed == true)
            {
                return BadRequest("Email is already confirmed. You can login");
            }

            try
            {
                if(await SendConfirmEMailAsync(user))
                {
                    return Ok(new JsonResult(new { title = "Confirmation link sent", message = "Please confirm your email address" }));
                }

                return BadRequest("Failed to send email.");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to send email.");
            }
        }

        [HttpPost("reset-username-password/{email}")]
        public async Task<IActionResult> ResetUsernameAndPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Please enter email address");
            }

            var user = await _userManager.FindByEmailAsync (email);

            if (user == null)
            {
                return BadRequest("Email is not registered while registration");
            }

            if (user.EmailConfirmed == false)
            {
                return BadRequest("Please confirm your email address first.");
            }

            try
            {
                if(await ResetUsernameAndPasswordEmailAsync(user))
                {
                    return Ok(new JsonResult(new { title = "Forgot username and password email", message = "Please check your email." }));
                }

                return BadRequest("Failed to send email.");
            }
            catch(Exception ex)
            {
                return BadRequest("Failed to send email.");
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);

            if (user == null)
            {
                return BadRequest("Email address is not found.");
            }

            if(user.EmailConfirmed == false)
            {
                return BadRequest("Please confirm the email address first.");
            }

            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(resetPasswordDto.Token);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

                var result = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.NewPassword);
                if (result.Succeeded)
                {
                    return Ok(new JsonResult(new { title = "Password Reset successfully", message = "Your Password Reset successfully, you can login with new password." }));
                }

                return BadRequest("Invalid token, Please try again");

            }
            catch (Exception ex)
            {
                return BadRequest("Invalid token, Please try again");
            }
        }

        #region Private Helper Methods
        private UserDto CreateApplicationUserDto(User user)
        {
            return new UserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                JWT = _jWTService.CreateJWt(user),
            };
        }

        private async Task<bool> CheckEmailExistAsync(string email)
        {
            return await _userManager.Users.AnyAsync(x => x.Email == email.ToLower());
        }

        private async Task<bool> SendConfirmEMailAsync(User user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = $"{_configuration["JWT:ClientUrl"]}/{_configuration["Email:ConfirmEmailPath"]}?token={token}&email={user.Email}";

            var body = $"<p>Hello: {user.FirstName} {user.LastName}</p>" +
                       "<p>Please confirm your email address by clicking on the following link.</p>" +
                       $"<p><a href=\"{url}\">Click here</a></p>" +
                       "<p>Thank you,</p>" +
                       $"<br>{_configuration["Email:ApplicationName"]}";

            var emailSend = new EmailSendDto(user.Email, "Confirm your email", body);

            return await _emailService.SendEmailAsync(emailSend);
        }

        private async Task<bool> ResetUsernameAndPasswordEmailAsync(User user)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = $"{_configuration["JWT:ClientUrl"]}/{_configuration["Email:ResetPasswordPath"]}?token={token}&email={user.Email}";

            var body = $"<p>Hello: {user.FirstName} {user.LastName}</p>" +
                       $"<p>Username: {user.UserName}</p>" +
                       "<p>In order to reset your password. Please click on the given below link.</p>" +
                       $"<p><a href=\"{url}\">Click here</a></p>" +
                       "<p>Thank you,</p>" +
                       $"<br>{_configuration["Email:ApplicationName"]}";

            var emailSend = new EmailSendDto(user.Email, "Reset your password", body);

            return await _emailService.SendEmailAsync(emailSend);
        }

        #endregion
    }
}
