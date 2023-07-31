using Models.DTOs.Input;
using Models.DTOs.Output;

namespace Services.Interfaces
{
    public interface IUserService
    {
        Task SignUpUserAsync(SignUpDto userModel, CancellationToken cancellationToken);
        Task<TokenDto> SignInUserAsync(SignInDto userModel, CancellationToken cancellationToken);
    }
}