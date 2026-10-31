using System.ComponentModel.DataAnnotations;

namespace TruthEye.DTOs
{
    public class RegisterStaffDto : RegisterUserDto
    {
        /// <summary>"FacultySecretary" أو "TechnicalStaff"</summary>
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateStaffDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class StaffDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>هل المستخدم الحالي يقدر يعدّل أو يعطّل أو يحذف العضو ده (الفرونت بيخفي الأزرار بناءً عليه).</summary>
        public bool CanManage { get; set; }
    }
}