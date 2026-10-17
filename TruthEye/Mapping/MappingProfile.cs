// using AutoMapper;
// using TruthEye.DTOs;
// using TruthEye.Entities;
// using TruthEye.Entities.Data;

// namespace TruthEye.Mapping
// {
//     public class MappingProfile : Profile
//     {
//         private static string GetDefaultRole(User user)
//         {
//             return user switch
//             {
//                 Student => "Student",
//                 Doctor => "Doctor",
//                 SuperAdmin => "SuperAdmin",
//                 Admin => "Admin",
//                 Proctor => "Proctor",
//                 Support => "Support",
//                 _ => "User"
//             };
//         }

//         public MappingProfile()
//         {
//             #region User Mappings

//             // RegisterAdminDto -> Admin
//             CreateMap<RegisterAdminDto, Admin>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
//                 .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
//                 .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
//                 .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
//                 .ForMember(dest => dest.Role, opt => opt.Ignore())
//                 .ForMember(dest => dest.University, opt => opt.Ignore())
//                 .ForMember(dest => dest.Faculty, opt => opt.Ignore())
//                 .ForMember(dest => dest.Notifications, opt => opt.Ignore())
//                 .ForMember(dest => dest.SentMessages, opt => opt.Ignore())
//                 .ForMember(dest => dest.ReceivedMessages, opt => opt.Ignore());

//             // Admin -> AdminDto
//             CreateMap<Admin, AdminDto>()
//                 .IncludeBase<User, UserDto>()
//                 .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "Admin"))
//                 .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => "Admin"))
//                 .ForMember(dest => dest.UniversityName, opt => opt.MapFrom(src =>
//                     src.University != null ? src.University.Name : string.Empty))
//                 .ForMember(dest => dest.FacultyName, opt => opt.MapFrom(src =>
//                     src.Faculty != null ? src.Faculty.Name : string.Empty));

//             // SuperAdmin -> AdminDto  
//             CreateMap<SuperAdmin, AdminDto>()
//                 .IncludeBase<User, UserDto>()
//                 .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "SuperAdmin"))
//                 .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => "SuperAdmin"))
//                 .ForMember(dest => dest.UniversityId, opt => opt.Ignore())
//                 .ForMember(dest => dest.FacultyId, opt => opt.Ignore());

//             // RegisterUserDto -> SuperAdmin
//             CreateMap<RegisterUserDto, SuperAdmin>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
//                 .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
//                 .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
//                 .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
//                 .ForMember(dest => dest.Role, opt => opt.Ignore())
//                 .ForMember(dest => dest.ConfiguredAISettings, opt => opt.Ignore())
//                 .ForMember(dest => dest.SystemLogs, opt => opt.Ignore());

//             CreateMap<User, UserDto>()
//                 .ForMember(dest => dest.UserType, opt => opt.MapFrom(src =>
//                     src is Student ? "Student" :
//                     src is Doctor ? "Doctor" :
//                     src is SuperAdmin ? "SuperAdmin" :
//                     src is Admin ? "Admin" :
//                     src is Proctor ? "Proctor" :
//                     src is Support ? "Support" : "User"))
//                 .ForMember(dest => dest.Role, opt => opt.MapFrom(src =>
//                     src.Role != null ? src.Role.Name : GetDefaultRole(src)));

//             #endregion

//             #region Student Mappings

//             CreateMap<RegisterStudentDto, Student>()
//                 .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
//                 .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
//                 .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
//                 .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(_ => "default.jpg"))
//                 .ForMember(dest => dest.RoleId, opt => opt.Ignore())
//                 .ForMember(dest => dest.UniversityId, opt => opt.Ignore()) 
//                 .ForMember(dest => dest.FacultyId, opt => opt.Ignore());   

//             CreateMap<Student, StudentDto>()
//                 .IncludeBase<User, UserDto>()
//                 .ForMember(dest => dest.UniversityName, opt => opt.MapFrom(src =>
//                     src.University != null ? src.University.Name : string.Empty))
//                 .ForMember(dest => dest.FacultyName, opt => opt.MapFrom(src =>
//                     src.Faculty != null ? src.Faculty.Name : string.Empty))
//                 .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src =>
//                     src.Department != null ? src.Department.Name : string.Empty));

//             CreateMap<UpdateStudentDto, Student>()
//                 .ForMember(dest => dest.UniversityId, opt => opt.Ignore())
//                 .ForMember(dest => dest.FacultyId, opt => opt.Ignore())   
//                 .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
//                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

//             #endregion

//             #region Doctor Mappings

//             CreateMap<RegisterDoctorDto, Doctor>()
//                 .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
//                 .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
//                 .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
//                 .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(_ => "default.jpg"))
//                 .ForMember(dest => dest.RoleId, opt => opt.Ignore())
//                 .ForMember(dest => dest.UniversityId, opt => opt.Ignore()) 
//                 .ForMember(dest => dest.FacultyId, opt => opt.Ignore()); 

//             CreateMap<Doctor, DoctorDto>()
//                 .IncludeBase<User, UserDto>()
//                 .ForMember(dest => dest.UniversityName, opt => opt.MapFrom(src =>
//                     src.University != null ? src.University.Name : string.Empty))
//                 .ForMember(dest => dest.FacultyName, opt => opt.MapFrom(src =>
//                     src.Faculty != null ? src.Faculty.Name : string.Empty))
//                 .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src =>
//                     src.Department != null ? src.Department.Name : string.Empty));

//             CreateMap<UpdateDoctorDto, Doctor>()
//                 .ForMember(dest => dest.UniversityId, opt => opt.Ignore())
//                 .ForMember(dest => dest.FacultyId, opt => opt.Ignore())    
//                 .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
//                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

//             #endregion

//             #region Proctor Mappings

//             CreateMap<RegisterProctorDto, Proctor>()
//                 .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
//                 .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
//                 .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
//                 .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(_ => "default.jpg"))
//                 .ForMember(dest => dest.RoleId, opt => opt.Ignore())
//                 .ForMember(dest => dest.IsOnDuty, opt => opt.MapFrom(_ => false))
//                 .ForMember(dest => dest.ActiveSessionsCount, opt => opt.MapFrom(_ => 0))
//                 .ForMember(dest => dest.AverageResponseTime, opt => opt.MapFrom(_ => 0.0));

//             CreateMap<UpdateProctorDto, Proctor>()
//                 .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
//                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

//             CreateMap<Proctor, ProctorDto>()
//                 .IncludeBase<User, UserDto>()
//                 .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
//                 .ForMember(dest => dest.IsOnDuty, opt => opt.MapFrom(src => src.IsOnDuty))
//                 .ForMember(dest => dest.ActiveSessionsCount, opt => opt.MapFrom(src => src.ActiveSessionsCount))
//                 .ForMember(dest => dest.LastDutyDate, opt => opt.MapFrom(src => src.LastDutyDate))
//                 .ForMember(dest => dest.AverageResponseTime, opt => opt.MapFrom(src => src.AverageResponseTime));

//             #endregion

//             #region MCQOption Mappings

//             // MCQOptionDto -> MCQOption
//             CreateMap<MCQOptionDto, MCQOption>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.QuestionId, opt => opt.Ignore())
//                 .ForMember(dest => dest.Question, opt => opt.Ignore());

//             // MCQOption -> MCQOptionDto
//             CreateMap<MCQOption, MCQOptionDto>();

//             #endregion

//             #region Question Mappings

//             // CreateQuestionDto -> Question (base)
//             CreateMap<CreateQuestionDto, Question>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.Exam, opt => opt.Ignore())
//                 .IncludeAllDerived();

//             // CreateQuestionDto -> MCQQuestion
//             CreateMap<CreateQuestionDto, MCQQuestion>()
//                 .ForMember(dest => dest.Options, opt => opt.Ignore()); 

//             // CreateQuestionDto -> WrittenQuestion  
//             CreateMap<CreateQuestionDto, WrittenQuestion>()
//                 .ForMember(dest => dest.ModelAnswer, opt => opt.MapFrom(src => src.ModelAnswer ?? ""))
//                 .ForMember(dest => dest.AIThreshold, opt => opt.MapFrom(src => src.AIThreshold))
//                 .ForMember(dest => dest.EvaluationModel, opt => opt.MapFrom(src => src.EvaluationModel ?? "Default"));

//             // Question -> QuestionDto
//             CreateMap<Question, QuestionDto>()
//                 .IncludeAllDerived();

//             // MCQQuestion -> QuestionDto
//             CreateMap<MCQQuestion, QuestionDto>()
//                 .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options))
//                 .ForMember(dest => dest.ModelAnswer, opt => opt.Ignore())
//                 .ForMember(dest => dest.AIThreshold, opt => opt.Ignore())
//                 .ForMember(dest => dest.EvaluationModel, opt => opt.Ignore());

//             // WrittenQuestion -> QuestionDto
//             CreateMap<WrittenQuestion, QuestionDto>()
//                 .ForMember(dest => dest.Options, opt => opt.Ignore())
//                 .ForMember(dest => dest.ModelAnswer, opt => opt.MapFrom(src => src.ModelAnswer))
//                 .ForMember(dest => dest.AIThreshold, opt => opt.MapFrom(src => src.AIThreshold))
//                 .ForMember(dest => dest.EvaluationModel, opt => opt.MapFrom(src => src.EvaluationModel));

//             #endregion
//             #region Answer & Result Mappings

//             // StudentAnswerDto -> Answer
//             CreateMap<StudentAnswerDto, Answer>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.ExamSessionId, opt => opt.Ignore())
//                 .ForMember(dest => dest.AnsweredAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
//                 .ForMember(dest => dest.IsCorrect, opt => opt.MapFrom(_ => false))
//                 .ForMember(dest => dest.MarksObtained, opt => opt.MapFrom(_ => 0))
//                 .ForMember(dest => dest.AISimilarityScore, opt => opt.Ignore())
//                 .ForMember(dest => dest.AIFeedback, opt => opt.Ignore())
//                 .ForMember(dest => dest.Question, opt => opt.Ignore())
//                 .ForMember(dest => dest.ExamSession, opt => opt.Ignore());

//             // Answer -> StudentAnswerDto
//             CreateMap<Answer, StudentAnswerDto>();

//             // ExamSession -> ExamResultDto
//             CreateMap<ExamSession, ExamResultDto>()
//                 .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => src.Id))
//                 .ForMember(dest => dest.ExamTitle, opt => opt.MapFrom(src => src.Exam.Title))
//                 .ForMember(dest => dest.CorrectAnswers, opt => opt.Ignore()) 
//                 .ForMember(dest => dest.TotalQuestions, opt => opt.Ignore()) 
//                 .ForMember(dest => dest.Duration, opt => opt.MapFrom(src =>
//                     src.EndTime.HasValue ? src.EndTime.Value - src.StartTime : TimeSpan.Zero))
//                 .ForMember(dest => dest.AIAlertsCount, opt => opt.Ignore()); 

//             #endregion
//             #region Support Mappings

//             CreateMap<RegisterSupportDto, Support>()
//                 .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
//                 .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
//                 .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
//                 .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(_ => "default.jpg"))
//                 .ForMember(dest => dest.RoleId, opt => opt.Ignore())
//                 .ForMember(dest => dest.IsOnline, opt => opt.MapFrom(_ => false))
//                 .ForMember(dest => dest.TicketsHandled, opt => opt.MapFrom(_ => 0))
//                 .ForMember(dest => dest.OpenTickets, opt => opt.MapFrom(_ => 0))
//                 .ForMember(dest => dest.AverageResolutionTime, opt => opt.MapFrom(_ => 0.0));

//             CreateMap<UpdateSupportDto, Support>()
//                 .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
//                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

//             CreateMap<Support, SupportDto>()
//                 .IncludeBase<User, UserDto>()
//                 .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
//                 .ForMember(dest => dest.Specialization, opt => opt.MapFrom(src => src.Specialization))
//                 .ForMember(dest => dest.IsOnline, opt => opt.MapFrom(src => src.IsOnline))
//                 .ForMember(dest => dest.TicketsHandled, opt => opt.MapFrom(src => src.TicketsHandled))
//                 .ForMember(dest => dest.OpenTickets, opt => opt.MapFrom(src => src.OpenTickets))
//                 .ForMember(dest => dest.LastTicketHandled, opt => opt.MapFrom(src => src.LastTicketHandled))
//                 .ForMember(dest => dest.AverageResolutionTime, opt => opt.MapFrom(src => src.AverageResolutionTime))
//                 .ForMember(dest => dest.ShiftTiming, opt => opt.MapFrom(src => src.ShiftTiming));

//             #endregion

//             #region University/Faculty/Department Mappings

//             CreateMap<CreateUniversityDto, University>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
//                 .ForMember(dest => dest.Faculties, opt => opt.Ignore());

//             CreateMap<UpdateUniversityDto, University>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
//                 .ForMember(dest => dest.Faculties, opt => opt.Ignore());

//             CreateMap<University, UniversityDto>()
//                 .ForMember(dest => dest.FacultiesCount,
//                            opt => opt.MapFrom(src => src.Faculties != null ? src.Faculties.Count : 0));

//             CreateMap<CreateFacultyDto, Faculty>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.University, opt => opt.Ignore())
//                 .ForMember(dest => dest.Departments, opt => opt.Ignore());

//             CreateMap<UpdateFacultyDto, Faculty>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.University, opt => opt.Ignore())
//                 .ForMember(dest => dest.Departments, opt => opt.Ignore());

//             CreateMap<Faculty, FacultyDto>()
//                 .ForMember(dest => dest.UniversityName,
//                            opt => opt.MapFrom(src => src.University != null ? src.University.Name : string.Empty))
//                 .ForMember(dest => dest.DepartmentsCount,
//                            opt => opt.MapFrom(src => src.Departments != null ? src.Departments.Count : 0));

//             CreateMap<CreateDepartmentDto, Department>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.Faculty, opt => opt.Ignore())
//                 .ForMember(dest => dest.Courses, opt => opt.Ignore());

//             CreateMap<UpdateDepartmentDto, Department>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.Faculty, opt => opt.Ignore())
//                 .ForMember(dest => dest.Courses, opt => opt.Ignore());

//             CreateMap<Department, DepartmentDto>()
//                 .ForMember(dest => dest.FacultyName,
//                            opt => opt.MapFrom(src => src.Faculty != null ? src.Faculty.Name : string.Empty))
//                 .ForMember(dest => dest.UniversityName,
//                            opt => opt.MapFrom(src => src.Faculty != null && src.Faculty.University != null
//                                ? src.Faculty.University.Name : string.Empty))
//                 .ForMember(dest => dest.CoursesCount,
//                            opt => opt.MapFrom(src => src.Courses != null ? src.Courses.Count : 0));

//             #endregion

//             #region Exam Mappings

//             // CreateExamDto -> Exam
//             CreateMap<CreateExamDto, Exam>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
//                 .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "Draft"))
//                 .ForMember(dest => dest.Questions, opt => opt.Ignore())
//                 .ForMember(dest => dest.ExamSessions, opt => opt.Ignore())
//                 .ForMember(dest => dest.Proctors, opt => opt.Ignore())
//                 .ForMember(dest => dest.Course, opt => opt.Ignore())
//                 .ForMember(dest => dest.Doctor, opt => opt.Ignore());

//             // Exam -> ExamDto
//             CreateMap<Exam, ExamDto>()
//                 .ForMember(dest => dest.CourseId, opt => opt.MapFrom(src => src.CourseId))
//                 .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId));

//             // UpdateExamDto -> Exam (إذا عندك UpdateExamDto)
//             CreateMap<CreateExamDto, Exam>()
//                 .ForMember(dest => dest.Id, opt => opt.Ignore())
//                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
//                 .ForMember(dest => dest.Status, opt => opt.Ignore())
//                 .ForMember(dest => dest.Questions, opt => opt.Ignore())
//                 .ForMember(dest => dest.ExamSessions, opt => opt.Ignore())
//                 .ForMember(dest => dest.Proctors, opt => opt.Ignore())
//                 .ForMember(dest => dest.Course, opt => opt.Ignore())
//                 .ForMember(dest => dest.Doctor, opt => opt.Ignore());

//             #endregion

//             #region ExamSession Mappings

//             CreateMap<ExamSession, ExamSessionDto>()
//                 .ForMember(dest => dest.ExamTitle, opt => opt.MapFrom(src => src.Exam.Title))
//                 .ReverseMap();

//             CreateMap<StartExamSessionDto, ExamSession>();

//             #endregion

//             #region SupportTicket Mappings

//             CreateMap<SupportTicket, SupportTicketDto>()
//                 .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
//                     $"{src.User.FirstName} {src.User.LastName}"))
//                 .ForMember(dest => dest.AssignedToName, opt => opt.MapFrom(src =>
//                     src.AssignedToSupport != null
//                         ? $"{src.AssignedToSupport.FirstName} {src.AssignedToSupport.LastName}"
//                         : null))
//                 .ReverseMap();

//             CreateMap<CreateSupportTicketDto, SupportTicket>();

//             #endregion
//         }
//     }
// }



using AutoMapper;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.Entities.Data;

namespace TruthEye.Mapping
{
    public class MappingProfile : Profile
    {
        private static string GetDefaultRole(User user)
        {
            return user switch
            {
                Student => "Student",
                Doctor => "Doctor",
                SuperAdmin => "SuperAdmin",
                Admin => "Admin",
                Proctor => "Proctor",
                Support => "Support",
                _ => "User"
            };
        }

        public MappingProfile()
        {
            #region User Mappings

            // RegisterAdminDto -> Admin
            CreateMap<RegisterAdminDto, Admin>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.University, opt => opt.Ignore())
                .ForMember(dest => dest.Faculty, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore())
                .ForMember(dest => dest.SentMessages, opt => opt.Ignore())
                .ForMember(dest => dest.ReceivedMessages, opt => opt.Ignore());

            // Admin -> AdminDto
            CreateMap<Admin, AdminDto>()
                .IncludeBase<User, UserDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "Admin"))
                .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => "Admin"))
                .ForMember(dest => dest.UniversityName, opt => opt.MapFrom(src =>
                    src.University != null ? src.University.Name : string.Empty))
                .ForMember(dest => dest.FacultyName, opt => opt.MapFrom(src =>
                    src.Faculty != null ? src.Faculty.Name : string.Empty));

            // SuperAdmin -> AdminDto  
            CreateMap<SuperAdmin, AdminDto>()
                .IncludeBase<User, UserDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "SuperAdmin"))
                .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => "SuperAdmin"))
                .ForMember(dest => dest.UniversityId, opt => opt.Ignore())
                .ForMember(dest => dest.FacultyId, opt => opt.Ignore());

            // RegisterUserDto -> SuperAdmin
            CreateMap<RegisterUserDto, SuperAdmin>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.ConfiguredAISettings, opt => opt.Ignore())
                .ForMember(dest => dest.SystemLogs, opt => opt.Ignore());

            CreateMap<User, UserDto>()
                .ForMember(dest => dest.UserType, opt => opt.MapFrom(src =>
                    src is Student ? "Student" :
                    src is Doctor ? "Doctor" :
                    src is SuperAdmin ? "SuperAdmin" :
                    src is Admin ? "Admin" :
                    src is Proctor ? "Proctor" :
                    src is Support ? "Support" : "User"))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src =>
                    src.Role != null ? src.Role.Name : GetDefaultRole(src)));

            #endregion

            #region Student Mappings

            CreateMap<RegisterStudentDto, Student>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(_ => "default.jpg"))
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.UniversityId, opt => opt.Ignore()) 
                .ForMember(dest => dest.FacultyId, opt => opt.Ignore());   

            CreateMap<Student, StudentDto>()
                .IncludeBase<User, UserDto>()
                .ForMember(dest => dest.UniversityName, opt => opt.MapFrom(src =>
                    src.University != null ? src.University.Name : string.Empty))
                .ForMember(dest => dest.FacultyName, opt => opt.MapFrom(src =>
                    src.Faculty != null ? src.Faculty.Name : string.Empty))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src =>
                    src.Department != null ? src.Department.Name : string.Empty));

            CreateMap<UpdateStudentDto, Student>()
                .ForMember(dest => dest.UniversityId, opt => opt.Ignore())
                .ForMember(dest => dest.FacultyId, opt => opt.Ignore())   
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            #endregion

            #region Doctor Mappings

            CreateMap<RegisterDoctorDto, Doctor>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(_ => "default.jpg"))
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.UniversityId, opt => opt.Ignore()) 
                .ForMember(dest => dest.FacultyId, opt => opt.Ignore()); 

            CreateMap<Doctor, DoctorDto>()
                .IncludeBase<User, UserDto>()
                .ForMember(dest => dest.UniversityName, opt => opt.MapFrom(src =>
                    src.University != null ? src.University.Name : string.Empty))
                .ForMember(dest => dest.FacultyName, opt => opt.MapFrom(src =>
                    src.Faculty != null ? src.Faculty.Name : string.Empty))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src =>
                    src.Department != null ? src.Department.Name : string.Empty));

            CreateMap<UpdateDoctorDto, Doctor>()
                .ForMember(dest => dest.UniversityId, opt => opt.Ignore())
                .ForMember(dest => dest.FacultyId, opt => opt.Ignore())    
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            #endregion

            #region Proctor Mappings

            CreateMap<RegisterProctorDto, Proctor>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(_ => "default.jpg"))
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.IsOnDuty, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.ActiveSessionsCount, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.AverageResponseTime, opt => opt.MapFrom(_ => 0.0));

            CreateMap<UpdateProctorDto, Proctor>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Proctor, ProctorDto>()
                .IncludeBase<User, UserDto>()
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.IsOnDuty, opt => opt.MapFrom(src => src.IsOnDuty))
                .ForMember(dest => dest.ActiveSessionsCount, opt => opt.MapFrom(src => src.ActiveSessionsCount))
                .ForMember(dest => dest.LastDutyDate, opt => opt.MapFrom(src => src.LastDutyDate))
                .ForMember(dest => dest.AverageResponseTime, opt => opt.MapFrom(src => src.AverageResponseTime));

            #endregion

            #region MCQOption Mappings

            // MCQOptionDto -> MCQOption
            CreateMap<MCQOptionDto, MCQOption>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.QuestionId, opt => opt.Ignore())
                .ForMember(dest => dest.Question, opt => opt.Ignore());

            // MCQOption -> MCQOptionDto
            CreateMap<MCQOption, MCQOptionDto>();

            #endregion

            #region Question Mappings

            // CreateQuestionDto -> Question (base)
            CreateMap<CreateQuestionDto, Question>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Exam, opt => opt.Ignore())
                .IncludeAllDerived();

            // CreateQuestionDto -> MCQQuestion
            CreateMap<CreateQuestionDto, MCQQuestion>()
                .ForMember(dest => dest.Options, opt => opt.Ignore()); 

            // CreateQuestionDto -> WrittenQuestion  
            CreateMap<CreateQuestionDto, WrittenQuestion>()
                .ForMember(dest => dest.ModelAnswer, opt => opt.MapFrom(src => src.ModelAnswer ?? ""))
                .ForMember(dest => dest.AIThreshold, opt => opt.MapFrom(src => src.AIThreshold))
                .ForMember(dest => dest.EvaluationModel, opt => opt.MapFrom(src => src.EvaluationModel ?? "Default"));

            // Question -> QuestionDto
            CreateMap<Question, QuestionDto>()
                .IncludeAllDerived();

            // MCQQuestion -> QuestionDto
            CreateMap<MCQQuestion, QuestionDto>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options))
                .ForMember(dest => dest.ModelAnswer, opt => opt.Ignore())
                .ForMember(dest => dest.AIThreshold, opt => opt.Ignore())
                .ForMember(dest => dest.EvaluationModel, opt => opt.Ignore());

            // WrittenQuestion -> QuestionDto
            CreateMap<WrittenQuestion, QuestionDto>()
                .ForMember(dest => dest.Options, opt => opt.Ignore())
                .ForMember(dest => dest.ModelAnswer, opt => opt.MapFrom(src => src.ModelAnswer))
                .ForMember(dest => dest.AIThreshold, opt => opt.MapFrom(src => src.AIThreshold))
                .ForMember(dest => dest.EvaluationModel, opt => opt.MapFrom(src => src.EvaluationModel));

            #endregion
            #region Answer & Result Mappings

            // StudentAnswerDto -> Answer
            CreateMap<StudentAnswerDto, Answer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ExamSessionId, opt => opt.Ignore())
                .ForMember(dest => dest.AnsweredAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsCorrect, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.MarksObtained, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.AISimilarityScore, opt => opt.Ignore())
                .ForMember(dest => dest.AIFeedback, opt => opt.Ignore())
                .ForMember(dest => dest.Question, opt => opt.Ignore())
                .ForMember(dest => dest.ExamSession, opt => opt.Ignore());

            // Answer -> StudentAnswerDto
            CreateMap<Answer, StudentAnswerDto>();

            // ExamSession -> ExamResultDto
            CreateMap<ExamSession, ExamResultDto>()
                .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ExamTitle, opt => opt.MapFrom(src => src.Exam.Title))
                .ForMember(dest => dest.CorrectAnswers, opt => opt.Ignore()) 
                .ForMember(dest => dest.TotalQuestions, opt => opt.Ignore()) 
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src =>
                    src.EndTime.HasValue ? src.EndTime.Value - src.StartTime : TimeSpan.Zero))
                .ForMember(dest => dest.AIAlertsCount, opt => opt.Ignore()); 

            #endregion
            #region Support Mappings

            CreateMap<RegisterSupportDto, Support>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(_ => "default.jpg"))
                .ForMember(dest => dest.RoleId, opt => opt.Ignore())
                .ForMember(dest => dest.IsOnline, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.TicketsHandled, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.OpenTickets, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.AverageResolutionTime, opt => opt.MapFrom(_ => 0.0));

            CreateMap<UpdateSupportDto, Support>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Support, SupportDto>()
                .IncludeBase<User, UserDto>()
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.Specialization, opt => opt.MapFrom(src => src.Specialization))
                .ForMember(dest => dest.IsOnline, opt => opt.MapFrom(src => src.IsOnline))
                .ForMember(dest => dest.TicketsHandled, opt => opt.MapFrom(src => src.TicketsHandled))
                .ForMember(dest => dest.OpenTickets, opt => opt.MapFrom(src => src.OpenTickets))
                .ForMember(dest => dest.LastTicketHandled, opt => opt.MapFrom(src => src.LastTicketHandled))
                .ForMember(dest => dest.AverageResolutionTime, opt => opt.MapFrom(src => src.AverageResolutionTime))
                .ForMember(dest => dest.ShiftTiming, opt => opt.MapFrom(src => src.ShiftTiming));

            #endregion

            #region University/Faculty/Department Mappings

            CreateMap<CreateUniversityDto, University>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Faculties, opt => opt.Ignore());

            CreateMap<UpdateUniversityDto, University>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Faculties, opt => opt.Ignore());

            CreateMap<University, UniversityDto>()
                .ForMember(dest => dest.FacultiesCount,
                           opt => opt.MapFrom(src => src.Faculties != null ? src.Faculties.Count : 0));

            CreateMap<CreateFacultyDto, Faculty>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.University, opt => opt.Ignore())
                .ForMember(dest => dest.Departments, opt => opt.Ignore());

            CreateMap<UpdateFacultyDto, Faculty>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.University, opt => opt.Ignore())
                .ForMember(dest => dest.Departments, opt => opt.Ignore());

            CreateMap<Faculty, FacultyDto>()
                .ForMember(dest => dest.UniversityName,
                           opt => opt.MapFrom(src => src.University != null ? src.University.Name : string.Empty))
                .ForMember(dest => dest.DepartmentsCount,
                           opt => opt.MapFrom(src => src.Departments != null ? src.Departments.Count : 0));

            CreateMap<CreateDepartmentDto, Department>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Faculty, opt => opt.Ignore())
                .ForMember(dest => dest.Courses, opt => opt.Ignore());

            CreateMap<UpdateDepartmentDto, Department>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Faculty, opt => opt.Ignore())
                .ForMember(dest => dest.Courses, opt => opt.Ignore());

            CreateMap<Department, DepartmentDto>()
                .ForMember(dest => dest.FacultyName,
                           opt => opt.MapFrom(src => src.Faculty != null ? src.Faculty.Name : string.Empty))
                .ForMember(dest => dest.UniversityName,
                           opt => opt.MapFrom(src => src.Faculty != null && src.Faculty.University != null
                               ? src.Faculty.University.Name : string.Empty))
                .ForMember(dest => dest.CoursesCount,
                           opt => opt.MapFrom(src => src.Courses != null ? src.Courses.Count : 0));

            #endregion

            #region Course Mappings

            CreateMap<CreateCourseDto, Course>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Department, opt => opt.Ignore());

            CreateMap<UpdateCourseDto, Course>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Department, opt => opt.Ignore());

            CreateMap<Course, CourseDto>()
                .ForMember(dest => dest.DepartmentName,
                           opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null));

            #endregion

            #region Exam Mappings

            // CreateExamDto -> Exam
            CreateMap<CreateExamDto, Exam>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => "Draft"))
                .ForMember(dest => dest.Questions, opt => opt.Ignore())
                .ForMember(dest => dest.ExamSessions, opt => opt.Ignore())
                .ForMember(dest => dest.Proctors, opt => opt.Ignore())
                .ForMember(dest => dest.Course, opt => opt.Ignore())
                .ForMember(dest => dest.Doctor, opt => opt.Ignore());

            // Exam -> ExamDto
            CreateMap<Exam, ExamDto>()
                .ForMember(dest => dest.CourseId, opt => opt.MapFrom(src => src.CourseId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId));

            // Note: the duplicate CreateMap<CreateExamDto, Exam>() was removed.
            // For updates, edit the fields manually in UpdateExamAsync.

            #endregion

            #region ExamSession Mappings
CreateMap<ExamSession, ExamSessionDto>()
    .ForMember(dest => dest.ExamTitle, opt => opt.MapFrom(src => src.Exam.Title))
    .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src =>
        src.Student != null ? src.Student.FirstName + " " + src.Student.LastName : null))
    .ReverseMap();

            CreateMap<StartExamSessionDto, ExamSession>();

            #endregion

            #region SupportTicket Mappings

            CreateMap<SupportTicket, SupportTicketDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
                    $"{src.User.FirstName} {src.User.LastName}"))
                .ForMember(dest => dest.AssignedToName, opt => opt.MapFrom(src =>
                    src.AssignedToSupport != null
                        ? $"{src.AssignedToSupport.FirstName} {src.AssignedToSupport.LastName}"
                        : null))
                .ReverseMap();

            CreateMap<CreateSupportTicketDto, SupportTicket>();

            #endregion
        }
    }
}