namespace KRAFT.Results.Contracts.Users;

public sealed record class ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);