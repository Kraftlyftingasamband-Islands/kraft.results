using KRAFT.Results.Contracts.Users;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class ChangePasswordCommandBuilder
{
    private string _currentPassword = Constants.TestUser.Password;
    private string _newPassword = "NewSecurePassword123";
    private string _confirmNewPassword = "NewSecurePassword123";

    public ChangePasswordCommandBuilder WithCurrentPassword(string currentPassword)
    {
        _currentPassword = currentPassword;
        return this;
    }

    public ChangePasswordCommandBuilder WithNewPassword(string newPassword)
    {
        _newPassword = newPassword;
        return this;
    }

    public ChangePasswordCommandBuilder WithConfirmNewPassword(string confirmNewPassword)
    {
        _confirmNewPassword = confirmNewPassword;
        return this;
    }

    public ChangePasswordCommand Build() =>
        new(_currentPassword, _newPassword, _confirmNewPassword);
}