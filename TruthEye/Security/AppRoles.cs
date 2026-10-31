namespace TruthEye.Security
{
    public static class AppRoles
    {
        public const string SuperAdmin = "SuperAdmin";   // رئيس الجامعة
        public const string Dean = "Admin";              // العميد (الاسم القديم محفوظ عشان التوافق)
        public const string Secretary = "FacultySecretary";
        public const string Technical = "TechnicalStaff";
        public const string Doctor = "Doctor";
        public const string Student = "Student";
        public const string Proctor = "Proctor";
        public const string Support = "Support";

        // مجموعات جاهزة لـ [Authorize(Roles = ...)]
        public const string Managers = Dean + "," + Secretary;                          // حذف + سجل النشاط
        public const string FacultyStaff = Dean + "," + Secretary + "," + Technical;   // إضافة وتعديل وتعطيل
        public const string ManagersAndSuper = SuperAdmin + "," + Managers;
        public const string FacultyStaffAndSuper = SuperAdmin + "," + FacultyStaff;
    }
}