using TruthEye.Entities;
using TruthEye.Entities.Data;

namespace TruthEye.Security
{
    public static class StaffRules
    {
        /// <summary>
        /// الترتيب مهم: FacultySecretary وTechnicalStaff بيرثوا من Admin،
        /// فلازم يتفحصوا قبله وإلا هيتحسبوا "Admin".
        /// </summary>
        public static string RoleOf(User user) => user switch
        {
            SuperAdmin => AppRoles.SuperAdmin,
            FacultySecretary => AppRoles.Secretary,
            TechnicalStaff => AppRoles.Technical,
            Admin => AppRoles.Dean,
            Student => AppRoles.Student,
            Doctor => AppRoles.Doctor,
            Proctor => AppRoles.Proctor,
            Support => AppRoles.Support,
            _ => "User"
        };

        /// <summary>مين يقدر يعيّن ويعطّل ويحذف مين. ممنوع التصعيد: محدش بيعيّن مستوى أعلى منه أو زيه.</summary>
        public static bool CanManageStaff(string actorRole, string targetRole) => actorRole switch
        {
            AppRoles.SuperAdmin => targetRole == AppRoles.Dean,
            AppRoles.Dean => targetRole is AppRoles.Secretary or AppRoles.Technical,
            AppRoles.Secretary => targetRole == AppRoles.Technical,
            _ => false
        };

        /// <summary>الحذف (IsDeleted) للعميد والأمين بس. التيم الفني تعطيل فقط.</summary>
        public static bool CanDeleteMembers(string role) =>
            role is AppRoles.Dean or AppRoles.Secretary;

        public static bool CanViewActivityLog(string role) =>
            role is AppRoles.SuperAdmin or AppRoles.Dean or AppRoles.Secretary;

        public static bool IsFacultyStaff(string role) =>
            role is AppRoles.Dean or AppRoles.Secretary or AppRoles.Technical;
    }
}