using Microsoft.AspNetCore.Identity;
using Models.DTOs.Input;

namespace Services.Mappers.Extensions
{
    public static class MappingExtensions
    {
        public static IdentityUser<int> SignInDtoToIdentityUser(this SignInDto signInDto)
        {
            return new IdentityUser<int>()
            {
                Email = signInDto.Email
            };
        }

        public static IdentityUser<int> SignUpDtoToIdentityUser(this SignUpDto signUpDto)
        {
            return new IdentityUser<int>()
            {
                Email = signUpDto.Email,
                UserName = signUpDto.FullName
            };
        }
    }
}