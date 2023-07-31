using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Models.DTOs.Input;
using Models.DTOs.Output;
using Services.Helpers;
using Services.Interfaces;
using Services.Mappers.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Services.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly SignInManager<IdentityUser<int>> _signInManager;
        private readonly AuthOptions _authOptions;

        public UserService(UserManager<IdentityUser<int>> userManager, SignInManager<IdentityUser<int>> signInManager, AuthOptions authOptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _authOptions = authOptions;
        }

        public async Task SignUpUserAsync(SignUpDto userModel, CancellationToken cancellationToken)
        {
            var identityUser = userModel.SignUpDtoToIdentityUser();
            var identityResult = await _userManager.CreateAsync(identityUser, userModel.Password);
            if(!identityResult.Succeeded) {
                throw new Exception(string.Join(", ", identityResult.Errors.Select(er => er.Description)));
            }
        }

        public async Task<TokenDto> SignInUserAsync(SignInDto userModel, CancellationToken cancellationToken)
        {
            var identityUser = await _userManager.FindByEmailAsync(userModel.Email);
            if (identityUser == null) 
                throw new UnauthorizedAccessException("Account does not exist");

            var isPasswordCorrect = await _signInManager.CheckPasswordSignInAsync(identityUser, userModel.Password, false);
            if (!isPasswordCorrect.Succeeded)
                throw new UnauthorizedAccessException("Passwords doesn't match");

            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, identityUser.UserName),
            };

            var roles = await _userManager.GetRolesAsync(identityUser);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var personClaims = await _userManager.GetClaimsAsync(identityUser);
            claims.AddRange(personClaims);

            var jwt = new JwtSecurityToken(
                issuer: _authOptions.Issuer,
                audience: _authOptions.Audience,
                claims: claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.Add(TimeSpan.FromMinutes(_authOptions.Lifetime)),
                signingCredentials: new SigningCredentials(_authOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            return new TokenDto()
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt)
            };
        }
    }
}