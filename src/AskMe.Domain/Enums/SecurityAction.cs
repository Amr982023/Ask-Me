namespace AskMe.Domain.Enums;

public enum SecurityAction
{
    ViewSecurityMetadata = 0,
    HideQuestion = 1,
    UnhideQuestion = 2,
    DeleteQuestion = 3,
    RestoreQuestion = 4,
    DeleteUser = 5,
    BlockIp = 6,
    UnblockIp = 7
}
