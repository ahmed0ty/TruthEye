using TruthEye.Entities;
using TruthEye.Entities.Data;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Security
{
    public static class ChatRules
    {
        public static int FacultyOf(User u) => u switch
        {
            Student s => s.FacultyId,
            Doctor d => d.FacultyId,
            Proctor p => p.FacultyId ?? 0,
            Admin a => a.FacultyId,
            _ => 0
        };

        private static bool IsStaff(User u) => u is Admin; // عميد/أمين/تيم فني (SuperAdmin ملوش علاقة بـ Admin)

        private static async Task<bool> OneWay(IUnitOfWork uow, User x, User y)
        {
            switch (x)
            {
                case Student s when y is Doctor d:
                    return await uow.Repository<Course>().CountAsync(c =>
                        c.RegisteredStudents.Any(z => z.Id == s.Id) && c.Instructors.Any(z => z.Id == d.Id)) > 0;

                case Admin a when y is Student or Doctor:
                    return a.FacultyId == FacultyOf(y);

                case Proctor p when IsStaff(y):
                    return p.FacultyId.HasValue && p.FacultyId == FacultyOf(y);

                case Support when IsStaff(y):
                case SuperAdmin when IsStaff(y):
                    return true;

                default:
                    return false;
            }
        }

        public static async Task<bool> CanChat(IUnitOfWork uow, User a, User b) =>
            a.Id != b.Id && (await OneWay(uow, a, b) || await OneWay(uow, b, a));
    }
}