namespace TruthEye.Services
{
    public static class ActivityEntities
    {
        public const string Student = "Student";
        public const string Doctor = "Doctor";
        public const string Proctor = "Proctor";
        public const string Staff = "Staff";
        public const string Course = "Course";
        public const string Department = "Department";
        public const string CourseStudent = "CourseStudent";
        public const string CourseDoctor = "CourseDoctor";
    }

    public static class ActivityActions
    {
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Disable = "Disable";
        public const string Enable = "Enable";
        public const string Delete = "Delete";
        public const string Restore = "Restore";
        public const string Assign = "Assign";
        public const string Unassign = "Unassign";
        public const string BulkAssign = "BulkAssign";

        /// <summary>الأقسام مش قابلة للتراجع لأن حذفها حذف فعلي.</summary>
        public static bool IsReversible(string entityType, string action) => entityType switch
        {
            ActivityEntities.Student or ActivityEntities.Doctor or ActivityEntities.Proctor or ActivityEntities.Staff
                => action is Create or Update or Disable or Enable or Delete or Restore,
            ActivityEntities.Course
                => action is Create or Delete or Restore,
            ActivityEntities.CourseStudent or ActivityEntities.CourseDoctor
                => action is Assign or Unassign or BulkAssign,
            _ => false
        };
    }
}