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
    public const string Login = "Login";
    public const string SessionReview = "SessionReview";
    public const string Grade = "Grade";
public const string Appeal = "Appeal";
public const string Resume = "Resume";

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
        public const string LoginSuccess = "LoginSuccess";
public const string LoginFailed = "LoginFailed";
public const string LoginDisabled = "LoginDisabled";
public const string ResetPassword = "ResetPassword";
        public const string EnableTwoFactor = "EnableTwoFactor";
        public const string DisableTwoFactor = "DisableTwoFactor";
        public const string ResetTwoFactor = "ResetTwoFactor";
public const string ReviewCleared = "ReviewCleared";
public const string ReviewConfirmed = "ReviewConfirmed";
public const string GradeAdjusted = "GradeAdjusted";
public const string AppealCreated = "AppealCreated";
public const string AppealAccepted = "AppealAccepted";
public const string AppealRejected = "AppealRejected";
public const string LoginBlocked = "LoginBlocked";
public const string ResumeRequested = "ResumeRequested";
public const string ResumeApproved = "ResumeApproved";
public const string ResumeRejected = "ResumeRejected";

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