using Microsoft.AspNetCore.Mvc;
using Models.DTOs.Input;
using Models.DTOs.Output;
using Services.Interfaces;

namespace Transactions.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorizeController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthorizeController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("sign-up")]
        public async Task<ActionResult> SignUpAsync([FromBody] SignUpDto userModel, CancellationToken cancellationToken)
        {
            await _userService.SignUpUserAsync(userModel, cancellationToken);
            return Ok();
        }

        [HttpPost("sign-in")]
        public async Task<ActionResult<TokenDto>> SignInAsync([FromBody] SignInDto userModel, CancellationToken cancellationToken)
        {
            var jwt = await _userService.SignInUserAsync(userModel, cancellationToken);
            return Ok(jwt);
        }
    }
}