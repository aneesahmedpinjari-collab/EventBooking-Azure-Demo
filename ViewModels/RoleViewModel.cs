using System.ComponentModel.DataAnnotations;

namespace EventBookingSecure.ViewModels
{
    public class CreateRoleViewModel
    {
        [Required]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } = string.Empty;
    }

    public class EditRoleViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role Name is required")]
        public string RoleName { get; set; } = string.Empty;

        public List<string> Users { get; set; } = new List<string>();
    }

    public class UserRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
