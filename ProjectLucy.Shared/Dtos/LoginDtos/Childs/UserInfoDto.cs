namespace ProjectLucy.Shared.Dtos.LoginDtos.Childs
{
    /// <summary>
    /// Basic user info embedded inside login / refresh-token responses.
    /// </summary>
    public class UserInfoDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string RoleCode { get; set; } = null!;
        public string RoleName { get; set; } = null!;
    }
}
