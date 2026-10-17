// // using AutoMapper;
// // using TruthEye.DTOs;
// // using TruthEye.Entities;
// // using TruthEye.UnitOfWorkk;

// // namespace TruthEye.Services
// // {
    
// //         public class ExamService : IExamService
// //     {
// //         private readonly IUnitOfWork _unitOfWork;
// //         private readonly IMapper _mapper;
// //         private readonly ICacheService _cacheService;
// //         private readonly ILogger<ExamService> _logger;
// //         private readonly IExamSessionService _sessionService; 

// //         public ExamService(
// //             IUnitOfWork unitOfWork,
// //             IMapper mapper,
// //             ICacheService cacheService,
// //             ILogger<ExamService> logger,
// //             IExamSessionService sessionService) 
// //         {
// //             _unitOfWork = unitOfWork;
// //             _mapper = mapper;
// //             _cacheService = cacheService;
// //             _logger = logger;
// //             _sessionService = sessionService; 
// //         }
// //         public async Task<ExamDto> GetExamByIdAsync(int examId)
// //         {
// //             var cacheKey = $"exam:{examId}";

// //             return await _cacheService.GetOrSetAsync(
// //                 cacheKey,
// //                 async () =>
// //                 {
// //                     _logger.LogInformation("Fetching exam from database: {ExamId}", examId);
// //                     var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);

// //                     if (exam == null)
// //                         throw new Exception("Exam not found");

// //                     return _mapper.Map<ExamDto>(exam);
// //                 },
// //                 TimeSpan.FromMinutes(15)
// //             );
// //         }
// //         public async Task<IEnumerable<QuestionDto>> GetExamQuestionsAsync(int examId)
// //         {
// //             var cacheKey = $"exam-questions:{examId}";

// //             return await _cacheService.GetOrSetAsync(
// //                 cacheKey,
// //                 async () =>
// //                 {
// //                     _logger.LogInformation("Fetching exam questions from database: {ExamId}", examId);

// //                     var questions = await _unitOfWork.Repository<Question>()
// //                         .FindAsync(q => q.ExamId == examId,
// //                             q => (q as MCQQuestion)!.Options!); 

// //                     _logger.LogInformation("Found {Count} questions for exam {ExamId}", questions.Count(), examId);

// //                     var result = _mapper.Map<IEnumerable<QuestionDto>>(questions);

// //                     foreach (var question in result)
// //                     {
// //                         _logger.LogInformation("Question {Id}: Type={Type}, OptionsCount={OptionsCount}",
// //                             question.Id, question.QuestionType, question.Options?.Count ?? 0);
// //                     }

// //                     return result;
// //                 },
// //                 TimeSpan.FromMinutes(15)
// //             );
// //         }
// //         public async Task<IEnumerable<ExamDto>> GetAvailableExamsAsync(int studentId)
// //         {
// //             var cacheKey = $"available-exams:{studentId}";

// //             return await _cacheService.GetOrSetAsync(
// //                 cacheKey,
// //                 async () =>
// //                 {
// //                     _logger.LogInformation("Fetching available exams from database for student: {StudentId}", studentId);
// //                     var student = await _unitOfWork.Repository<Student>().GetByIdAsync(studentId);
// //                     var exams = await _unitOfWork.Repository<Exam>()
// //                         .FindAsync(e => student.RegisteredCourses.Any(c => c.Id == e.CourseId)
// //                             && e.Status == "Active");

// //                     return _mapper.Map<IEnumerable<ExamDto>>(exams);
// //                 },
// //                 TimeSpan.FromMinutes(10)
// //             );
// //         }

// //         public async Task<ExamDto> CreateExamAsync(CreateExamDto createExamDto, int doctorId)
// //         {
// //             var exam = _mapper.Map<Exam>(createExamDto);
// //             exam.DoctorId = doctorId;
// //             exam.CreatedAt = DateTime.UtcNow;
// //             exam.Status = "Draft";
// //             if (exam.TotalMarks == 0)
// //             {
// //                 exam.TotalMarks = createExamDto.TotalMarks > 0 ? createExamDto.TotalMarks : 100; 
// //             }

// //             await _unitOfWork.Repository<Exam>().AddAsync(exam);
// //             await _unitOfWork.SaveChangesAsync();

// //             await InvalidateExamCaches(exam.CourseId);

// //             _logger.LogInformation("Exam created: {ExamId}", exam.Id);
// //             return _mapper.Map<ExamDto>(exam);
// //         }

// //         public async Task<ExamDto> UpdateExamAsync(int examId, CreateExamDto updateExamDto)
// //         {
// //             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
// //             if (exam == null)
// //                 throw new Exception("Exam not found");

// //             _mapper.Map(updateExamDto, exam);
// //             await _unitOfWork.Repository<Exam>().UpdateAsync(exam);
// //             await _unitOfWork.SaveChangesAsync();

// //             await InvalidateExamCaches(exam.CourseId);
// //             await _cacheService.RemoveAsync($"exam:{examId}");

// //             return _mapper.Map<ExamDto>(exam);
// //         }

// //         public async Task DeleteExamAsync(int examId)
// //         {
// //             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
// //             if (exam == null)
// //                 throw new Exception("Exam not found");

// //             await _unitOfWork.Repository<Exam>().DeleteAsync(exam);
// //             await _unitOfWork.SaveChangesAsync();

// //             await InvalidateExamCaches(exam.CourseId);
// //         }

// //         public async Task<QuestionDto> AddQuestionAsync(CreateQuestionDto questionDto)
// //         {
// //             try
// //             {
// //                 _logger.LogInformation("Starting to add question: {@QuestionDto}", questionDto);

// //                 Question question;

// //                 if (questionDto.QuestionType == "MCQ")
// //                 {
// //                     _logger.LogInformation("Creating MCQ question");
// //                     var mcqQuestion = _mapper.Map<MCQQuestion>(questionDto);
// //                     mcqQuestion.Options = new List<MCQOption>();

// //                     if (questionDto.Options != null && questionDto.Options.Any())
// //                     {
// //                         _logger.LogInformation("Adding {OptionCount} options", questionDto.Options.Count);
// //                         foreach (var optionDto in questionDto.Options)
// //                         {
// //                             var option = new MCQOption
// //                             {
// //                                 OptionText = optionDto.OptionText,
// //                                 IsCorrect = optionDto.IsCorrect
// //                             };
// //                             mcqQuestion.Options.Add(option);
// //                             _logger.LogInformation("Added option: {OptionText}, IsCorrect: {IsCorrect}",
// //                                 option.OptionText, option.IsCorrect);
// //                         }
// //                     }

// //                     question = mcqQuestion;
// //                 }
// //                 else if (questionDto.QuestionType == "Written")
// //                 {
// //                     _logger.LogInformation("Creating Written question");
// //                     question = _mapper.Map<WrittenQuestion>(questionDto);
// //                 }
// //                 else
// //                 {
// //                     throw new Exception("Invalid question type");
// //                 }

// //                 await _unitOfWork.Repository<Question>().AddAsync(question);
// //                 await _unitOfWork.SaveChangesAsync();

// //                 _logger.LogInformation("Question saved successfully. ID: {QuestionId}", question.Id);

// //                 await _cacheService.RemoveAsync($"exam-questions:{questionDto.ExamId}");
// //                 await _cacheService.RemoveAsync($"exam:{questionDto.ExamId}");

// //                 var result = _mapper.Map<QuestionDto>(question);
// //                 _logger.LogInformation("Mapped result: {@Result}", result);

// //                 return result;
// //             }
// //             catch (Exception ex)
// //             {
// //                 _logger.LogError(ex, "Failed to add question");
// //                 throw;
// //             }
// //         }
// //         public async Task DeleteQuestionAsync(int questionId)
// //         {
// //             var question = await _unitOfWork.Repository<Question>().GetByIdAsync(questionId);
// //             if (question == null)
// //                 throw new Exception("Question not found");

// //             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(question.ExamId);

// //             await _unitOfWork.Repository<Question>().DeleteAsync(question);
// //             await _unitOfWork.SaveChangesAsync();

// //             if (exam != null)
// //             {
// //                 var remainingQuestions = await _unitOfWork.Repository<Question>()
// //                     .FindAsync(q => q.ExamId == exam.Id);

// //                 exam.TotalMarks = remainingQuestions.Sum(q => q.Marks);
// //                 await _unitOfWork.Repository<Exam>().UpdateAsync(exam);
// //                 await _unitOfWork.SaveChangesAsync();
// //             }

// //             await _cacheService.RemoveAsync($"exam-questions:{question.ExamId}");
// //             await _cacheService.RemoveAsync($"exam:{question.ExamId}");

// //             _logger.LogInformation("Question deleted: {QuestionId}", questionId);
// //         }
      
// //         public async Task<List<StudentExamResultDto>> GetExamResultsAsync(int examId, int doctorId)
// //         {
// //             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
// //             if (exam?.DoctorId != doctorId)
// //                 throw new Exception("Access denied or exam not found");

// //             var sessions = await _unitOfWork.Repository<ExamSession>()
// //                 .FindAsync(es => es.ExamId == examId && es.Status == "Completed",
// //                     es => es.Student!);

// //             return sessions.Select(s => new StudentExamResultDto
// //             {
// //                 StudentId = s.StudentId,
// //                 StudentName = $"{s.Student.FirstName} {s.Student.LastName}",
// //                 TotalScore = s.TotalScore,
// //                 Percentage = s.Percentage,
// //                 IsPassed = s.IsPassed,
// //                 CorrectAnswers = (int)(s.TotalScore / (s.TotalMarks / s.Exam.Questions.Count)),
// //                 TotalQuestions = s.Exam.Questions.Count,
// //                 SubmittedAt = (DateTime)s.SubmittedAt
// //             }).ToList();
// //         }

// //         public async Task<ExamAnalysisDto> GetExamAnalysisAsync(int examId, int doctorId)
// //         {
// //             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
// //             if (exam?.DoctorId != doctorId)
// //                 throw new Exception("Access denied or exam not found");

// //             var sessions = await _unitOfWork.Repository<ExamSession>()
// //                 .FindAsync(es => es.ExamId == examId && es.Status == "Completed");

// //             var totalStudents = sessions.Count();
// //             var averageScore = totalStudents > 0 ? sessions.Average(s => s.TotalScore) : 0;
// //             var passRate = totalStudents > 0 ? (sessions.Count(s => s.IsPassed) / (double)totalStudents * 100) : 0;

// //             return new ExamAnalysisDto
// //             {
// //                 ExamId = examId,
// //                 ExamTitle = exam.Title,
// //                 TotalStudents = totalStudents,
// //                 AverageScore = averageScore,
// //                 PassRate = passRate,
// //                 CommonMistakes = new List<CommonMistakeDto>() 
// //             };
// //         }

// //         public async Task<StudentExamDetailsDto> GetStudentExamDetailsAsync(int sessionId, int doctorId)
// //         {
// //             var session = await _unitOfWork.Repository<ExamSession>()
// //                 .GetByIdAsync(sessionId, s => s.Student!, s => s.Exam!);

// //             if (session?.Exam?.DoctorId != doctorId)
// //                 throw new Exception("Access denied or session not found");

// //             var answers = await _sessionService.GetExamAnswersAsync(sessionId);

// //             return new StudentExamDetailsDto
// //             {
// //                 SessionId = sessionId,
// //                 StudentName = $"{session.Student.FirstName} {session.Student.LastName}",
// //                 TotalScore = session.TotalScore,
// //                 Percentage = session.Percentage,
// //                 IsPassed = session.IsPassed,
// //                 Answers = answers.Questions ?? new List<QuestionWithAnswerDto>(),
// //                 SubmittedAt = (DateTime)session.SubmittedAt
// //             };
// //         }
// //         private async Task InvalidateExamCaches(int courseId)
// //         {
            
// //             await _cacheService.RemoveAsync($"course-exams:{courseId}");
// //         }
// //     }
// //     }

// using AutoMapper;
// using TruthEye.DTOs;
// using TruthEye.Entities;
// using TruthEye.UnitOfWorkk;

// namespace TruthEye.Services
// {
//     public class ExamService : IExamService
//     {
//         private readonly IUnitOfWork _unitOfWork;
//         private readonly IMapper _mapper;
//         private readonly ICacheService _cacheService;
//         private readonly ILogger<ExamService> _logger;
//         private readonly IExamSessionService _sessionService;
//         private readonly INotificationService _notifications;

// public ExamService(
//     IUnitOfWork unitOfWork,
//     IMapper mapper,
//     ICacheService cacheService,
//     ILogger<ExamService> logger,
//     IExamSessionService sessionService,
//     INotificationService notifications)
// {
//     _unitOfWork = unitOfWork;
//     _mapper = mapper;
//     _cacheService = cacheService;
//     _logger = logger;
//     _sessionService = sessionService;
//     _notifications = notifications;
// }

//         public async Task<ExamDto> GetExamByIdAsync(int examId)
//         {
//             var cacheKey = $"exam:{examId}";

//             return await _cacheService.GetOrSetAsync(
//                 cacheKey,
//                 async () =>
//                 {
//                     _logger.LogInformation("Fetching exam from database: {ExamId}", examId);
//                     var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);

//                     if (exam == null)
//                         throw new Exception("Exam not found");

//                     return _mapper.Map<ExamDto>(exam);
//                 },
//                 TimeSpan.FromMinutes(15)
//             );
//         }

//         public async Task<IEnumerable<QuestionDto>> GetExamQuestionsAsync(int examId)
//         {
//             var cacheKey = $"exam-questions:{examId}";

//             return await _cacheService.GetOrSetAsync(
//                 cacheKey,
//                 async () =>
//                 {
//                     _logger.LogInformation("Fetching exam questions from database: {ExamId}", examId);

//                     var questions = await _unitOfWork.Repository<Question>()
//                         .FindAsync(q => q.ExamId == examId,
//                             q => (q as MCQQuestion)!.Options!);

//                     _logger.LogInformation("Found {Count} questions for exam {ExamId}", questions.Count(), examId);

//                     var result = _mapper.Map<IEnumerable<QuestionDto>>(questions);

//                     foreach (var question in result)
//                     {
//                         _logger.LogInformation("Question {Id}: Type={Type}, OptionsCount={OptionsCount}",
//                             question.Id, question.QuestionType, question.Options?.Count ?? 0);
//                     }

//                     return result;
//                 },
//                 TimeSpan.FromMinutes(15)
//             );
//         }

//         public async Task<IEnumerable<ExamDto>> GetAvailableExamsAsync(int studentId)
//         {
//             var cacheKey = $"available-exams:{studentId}";

//             return await _cacheService.GetOrSetAsync(
//                 cacheKey,
//                 async () =>
//                 {
//                     _logger.LogInformation("Fetching available exams from database for student: {StudentId}", studentId);

//                     // Load the student WITH their registered courses
//                     var student = await _unitOfWork.Repository<Student>()
//                         .GetByIdAsync(studentId, s => s.RegisteredCourses);

//                     if (student == null)
//                         throw new Exception("Student not found");

//                     // Turn the course ids into a plain list so EF can translate it to SQL (IN clause)
//                     var courseIds = student.RegisteredCourses.Select(c => c.Id).ToList();

//                     var exams = await _unitOfWork.Repository<Exam>()
//                         .FindAsync(e => courseIds.Contains(e.CourseId) && e.Status == "Active");

//                     return _mapper.Map<IEnumerable<ExamDto>>(exams);
//                 },
//                 // DEV ONLY: short cache so newly published exams show up quickly.
//                 // Raise this (e.g. 10 minutes) before production.
//                 TimeSpan.FromSeconds(10)
//             );
//         }

//         public async Task<ExamDto> CreateExamAsync(CreateExamDto createExamDto, int doctorId)
//         {
//             var exam = _mapper.Map<Exam>(createExamDto);
//             exam.DoctorId = doctorId;
//             exam.CreatedAt = DateTime.UtcNow;
//             exam.Status = "Draft";
//             if (exam.TotalMarks == 0)
//             {
//                 exam.TotalMarks = createExamDto.TotalMarks > 0 ? createExamDto.TotalMarks : 100;
//             }

//             await _unitOfWork.Repository<Exam>().AddAsync(exam);
//             await _unitOfWork.SaveChangesAsync();

//             await InvalidateExamCaches(exam.CourseId);

//             _logger.LogInformation("Exam created: {ExamId}", exam.Id);
//             return _mapper.Map<ExamDto>(exam);
//         }

//         public async Task<ExamDto> UpdateExamAsync(int examId, CreateExamDto updateExamDto)
//         {
//             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
//             if (exam == null)
//                 throw new Exception("Exam not found");

//             // The mapping can reset Status to "Draft", so keep the current value
//             var currentStatus = exam.Status;

//             _mapper.Map(updateExamDto, exam);
//             exam.Status = currentStatus;

//             await _unitOfWork.Repository<Exam>().UpdateAsync(exam);
//             await _unitOfWork.SaveChangesAsync();

//             await InvalidateExamCaches(exam.CourseId);
//             await _cacheService.RemoveAsync($"exam:{examId}");

//             return _mapper.Map<ExamDto>(exam);
//         }

//         public async Task<ExamDto> SetExamStatusAsync(int examId, int doctorId, string status)
//         {
//             if (status != "Active" && status != "Draft")
//                 throw new Exception("Invalid status");

//             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
//             if (exam == null || exam.DoctorId != doctorId)
//                 throw new Exception("Access denied or exam not found");

//             if (status == "Active")
//             {
//                 var questions = await _unitOfWork.Repository<Question>()
//                     .FindAsync(q => q.ExamId == examId);
//                 if (!questions.Any())
//                     throw new Exception("Add at least one question before publishing");
//             }

//             exam.Status = status;
//             await _unitOfWork.Repository<Exam>().UpdateAsync(exam);
//             await _unitOfWork.SaveChangesAsync();

//             await InvalidateExamCaches(exam.CourseId);
//             await _cacheService.RemoveAsync($"exam:{examId}");
//             if (status == "Active")
// {
//     var course = await _unitOfWork.Repository<Course>()
//         .GetByIdAsync(exam.CourseId, c => c.RegisteredStudents);

//     var students = course?.RegisteredStudents?.ToList() ?? new List<Student>();
//     _logger.LogInformation("Publishing exam {ExamId}, notifying {Count} students", examId, students.Count);

//     foreach (var s in students)
//         await _notifications.SendNotificationAsync(s.Id, $"New exam published: {exam.Title}", "Exam");
// }

//             _logger.LogInformation("Exam {ExamId} status changed to {Status}", examId, status);
//             return _mapper.Map<ExamDto>(exam);
//         }

//         public async Task DeleteExamAsync(int examId)
//         {
//             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
//             if (exam == null)
//                 throw new Exception("Exam not found");

//             await _unitOfWork.Repository<Exam>().DeleteAsync(exam);
//             await _unitOfWork.SaveChangesAsync();

//             await InvalidateExamCaches(exam.CourseId);
//         }

//         public async Task<QuestionDto> AddQuestionAsync(CreateQuestionDto questionDto)
//         {
//             try
//             {
//                 _logger.LogInformation("Starting to add question: {@QuestionDto}", questionDto);

//                 Question question;

//                 if (questionDto.QuestionType == "MCQ")
//                 {
//                     // MCQ: options are mandatory, ModelAnswer is NOT needed
//                     if (questionDto.Options == null || !questionDto.Options.Any())
//                         throw new Exception("MCQ question must have at least one option");

//                     if (!questionDto.Options.Any(o => o.IsCorrect))
//                         throw new Exception("MCQ question must have at least one correct option");

//                     _logger.LogInformation("Creating MCQ question");
//                     var mcqQuestion = _mapper.Map<MCQQuestion>(questionDto);
//                     mcqQuestion.Options = new List<MCQOption>();

//                     _logger.LogInformation("Adding {OptionCount} options", questionDto.Options.Count);
//                     foreach (var optionDto in questionDto.Options)
//                     {
//                         var option = new MCQOption
//                         {
//                             OptionText = optionDto.OptionText,
//                             IsCorrect = optionDto.IsCorrect
//                         };
//                         mcqQuestion.Options.Add(option);
//                         _logger.LogInformation("Added option: {OptionText}, IsCorrect: {IsCorrect}",
//                             option.OptionText, option.IsCorrect);
//                     }

//                     question = mcqQuestion;
//                 }
//                else if (questionDto.QuestionType == "Written")
// {
//     // Written: ModelAnswer is mandatory, Options are NOT needed
//     if (string.IsNullOrWhiteSpace(questionDto.ModelAnswer))
//         throw new Exception("Model answer is required for written questions");

//     if (string.IsNullOrWhiteSpace(questionDto.EvaluationModel))
//         questionDto.EvaluationModel = "Default";

//     // Use the configured default threshold when the doctor didn't pick one
//     var cfg = (await _unitOfWork.Repository<AIConfig>()
//         .FindAsync(c => c.ParameterName == "DefaultWrittenThreshold")).FirstOrDefault();

//     if (cfg != null && questionDto.AIThreshold <= 0)
//         questionDto.AIThreshold = cfg.Threshold;
//     else if (questionDto.AIThreshold <= 0)
//         questionDto.AIThreshold = 0.7;

//     _logger.LogInformation("Creating Written question");
//     question = _mapper.Map<WrittenQuestion>(questionDto);
// }
//                 else
//                 {
//                     throw new Exception("Invalid question type");
//                 }

//                 await _unitOfWork.Repository<Question>().AddAsync(question);
//                 await _unitOfWork.SaveChangesAsync();

//                 _logger.LogInformation("Question saved successfully. ID: {QuestionId}", question.Id);

//                 await _cacheService.RemoveAsync($"exam-questions:{questionDto.ExamId}");
//                 await _cacheService.RemoveAsync($"exam:{questionDto.ExamId}");

//                 var result = _mapper.Map<QuestionDto>(question);
//                 _logger.LogInformation("Mapped result: {@Result}", result);

//                 return result;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Failed to add question");
//                 throw;
//             }
//         }

//         public async Task DeleteQuestionAsync(int questionId)
//         {
//             var question = await _unitOfWork.Repository<Question>().GetByIdAsync(questionId);
//             if (question == null)
//                 throw new Exception("Question not found");

//             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(question.ExamId);

//             await _unitOfWork.Repository<Question>().DeleteAsync(question);
//             await _unitOfWork.SaveChangesAsync();

//             if (exam != null)
//             {
//                 var remainingQuestions = await _unitOfWork.Repository<Question>()
//                     .FindAsync(q => q.ExamId == exam.Id);

//                 exam.TotalMarks = remainingQuestions.Sum(q => q.Marks);
//                 await _unitOfWork.Repository<Exam>().UpdateAsync(exam);
//                 await _unitOfWork.SaveChangesAsync();
//             }

//             await _cacheService.RemoveAsync($"exam-questions:{question.ExamId}");
//             await _cacheService.RemoveAsync($"exam:{question.ExamId}");

//             _logger.LogInformation("Question deleted: {QuestionId}", questionId);
//         }

//         public async Task<List<StudentExamResultDto>> GetExamResultsAsync(int examId, int doctorId)
//         {
//             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
//             if (exam == null || exam.DoctorId != doctorId)
//                 throw new Exception("Access denied or exam not found");

//             var sessions = await _unitOfWork.Repository<ExamSession>()
//                 .FindAsync(es => es.ExamId == examId && es.Status == "Completed",
//                     es => es.Student!);

//             // Count once instead of touching s.Exam.Questions for every session
//             var questions = await _unitOfWork.Repository<Question>()
//                 .FindAsync(q => q.ExamId == examId);
//             var totalQuestions = questions.Count();

//             return sessions.Select(s =>
//             {
//                 // Safe: no division by zero if the exam has no questions or no marks
//                 var correctAnswers = 0;
//                 if (totalQuestions > 0 && s.TotalMarks > 0)
//                 {
//                     var marksPerQuestion = (double)s.TotalMarks / totalQuestions;
//                     correctAnswers = (int)Math.Round(s.TotalScore / marksPerQuestion);
//                 }

//                                return new StudentExamResultDto
//                 {
//                     SessionId = s.Id,
//                     StudentId = s.StudentId,
//                     StudentName = s.Student != null
//                         ? $"{s.Student.FirstName} {s.Student.LastName}"
//                         : "Unknown",
//                     TotalScore = s.TotalScore,
//                     Percentage = s.Percentage,
//                     IsPassed = s.IsPassed,
//                     CorrectAnswers = correctAnswers,
//                     TotalQuestions = totalQuestions,
//                     // SubmittedAt can be null, so fall back to EndTime then StartTime
//                     SubmittedAt = s.SubmittedAt ?? s.EndTime ?? s.StartTime
//                 };
//             }).ToList();
//         }

//         public async Task<ExamAnalysisDto> GetExamAnalysisAsync(int examId, int doctorId)
//         {
//             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
//             if (exam == null || exam.DoctorId != doctorId)
//                 throw new Exception("Access denied or exam not found");

//             var sessions = await _unitOfWork.Repository<ExamSession>()
//                 .FindAsync(es => es.ExamId == examId && es.Status == "Completed");

//             var totalStudents = sessions.Count();
//             var averageScore = totalStudents > 0 ? sessions.Average(s => s.TotalScore) : 0;
//             var passRate = totalStudents > 0
//                 ? (sessions.Count(s => s.IsPassed) / (double)totalStudents * 100)
//                 : 0;

//             return new ExamAnalysisDto
//             {
//                 ExamId = examId,
//                 ExamTitle = exam.Title,
//                 TotalStudents = totalStudents,
//                 AverageScore = averageScore,
//                 PassRate = passRate,
//                 CommonMistakes = new List<CommonMistakeDto>()
//             };
//         }

//         public async Task<StudentExamDetailsDto> GetStudentExamDetailsAsync(int sessionId, int doctorId)
//         {
//             var session = await _unitOfWork.Repository<ExamSession>()
//                 .GetByIdAsync(sessionId, s => s.Student!, s => s.Exam!);

//             if (session == null || session.Exam == null || session.Exam.DoctorId != doctorId)
//                 throw new Exception("Access denied or session not found");

//             var answers = await _sessionService.GetExamAnswersAsync(sessionId);

//             return new StudentExamDetailsDto
//             {
//                 SessionId = sessionId,
//                 StudentName = session.Student != null
//                     ? $"{session.Student.FirstName} {session.Student.LastName}"
//                     : "Unknown",
//                 TotalScore = session.TotalScore,
//                 Percentage = session.Percentage,
//                 IsPassed = session.IsPassed,
//                 Answers = answers.Questions ?? new List<QuestionWithAnswerDto>(),
//                 SubmittedAt = session.SubmittedAt ?? session.EndTime ?? session.StartTime
//             };
//         }

//         private async Task InvalidateExamCaches(int courseId)
//         {
//             await _cacheService.RemoveAsync($"course-exams:{courseId}");
//         }
//     }
// }
using AutoMapper;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;
 
namespace TruthEye.Services
{
    public class ExamService : IExamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ExamService> _logger;
        private readonly IExamSessionService _sessionService;
        private readonly INotificationService _notifications;
 
        public ExamService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            ILogger<ExamService> logger,
            IExamSessionService sessionService,
            INotificationService notifications)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
            _sessionService = sessionService;
            _notifications = notifications;
        }
 
        public async Task<ExamDto> GetExamByIdAsync(int examId)
        {
            var cacheKey = $"exam:{examId}";
 
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation("Fetching exam from database: {ExamId}", examId);
                    var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
 
                    if (exam == null)
                        throw new Exception("Exam not found");
 
                    return _mapper.Map<ExamDto>(exam);
                },
                TimeSpan.FromMinutes(15)
            );
        }
 
        public async Task<IEnumerable<QuestionDto>> GetExamQuestionsAsync(int examId)
        {
            var cacheKey = $"exam-questions:{examId}";
 
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation("Fetching exam questions from database: {ExamId}", examId);
 
                    var questions = await _unitOfWork.Repository<Question>()
                        .FindAsync(q => q.ExamId == examId,
                            q => (q as MCQQuestion)!.Options!);
 
                    _logger.LogInformation("Found {Count} questions for exam {ExamId}", questions.Count(), examId);
 
                    var result = _mapper.Map<IEnumerable<QuestionDto>>(questions);
 
                    foreach (var question in result)
                    {
                        _logger.LogInformation("Question {Id}: Type={Type}, OptionsCount={OptionsCount}",
                            question.Id, question.QuestionType, question.Options?.Count ?? 0);
                    }
 
                    return result;
                },
                TimeSpan.FromMinutes(15)
            );
        }
 
        public async Task<IEnumerable<ExamDto>> GetAvailableExamsAsync(int studentId)
        {
            var cacheKey = $"available-exams:{studentId}";
 
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation("Fetching available exams from database for student: {StudentId}", studentId);
 
                    var student = await _unitOfWork.Repository<Student>()
                        .GetByIdAsync(studentId, s => s.RegisteredCourses);
 
                    if (student == null)
                        throw new Exception("Student not found");
 
                    var courseIds = student.RegisteredCourses.Select(c => c.Id).ToList();
 
                    var exams = await _unitOfWork.Repository<Exam>()
                        .FindAsync(e => courseIds.Contains(e.CourseId) && e.Status == "Active");
 
                    return _mapper.Map<IEnumerable<ExamDto>>(exams);
                },
                // DEV ONLY: short cache so newly published exams show up quickly.
                // Raise this (e.g. 10 minutes) before production.
                TimeSpan.FromSeconds(10)
            );
        }
 
        public async Task<ExamDto> CreateExamAsync(CreateExamDto createExamDto, int doctorId)
        {
            var exam = _mapper.Map<Exam>(createExamDto);
            exam.DoctorId = doctorId;
            exam.CreatedAt = DateTime.UtcNow;
            exam.Status = "Draft";
            if (exam.TotalMarks == 0)
            {
                exam.TotalMarks = createExamDto.TotalMarks > 0 ? createExamDto.TotalMarks : 100;
            }
 
            await _unitOfWork.Repository<Exam>().AddAsync(exam);
            await _unitOfWork.SaveChangesAsync();
 
            await InvalidateExamCaches(exam.CourseId);
 
            _logger.LogInformation("Exam created: {ExamId}", exam.Id);
            return _mapper.Map<ExamDto>(exam);
        }
 
        public async Task<ExamDto> UpdateExamAsync(int examId, CreateExamDto updateExamDto)
        {
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
            if (exam == null)
                throw new Exception("Exam not found");
 
            // The mapping can reset Status to "Draft", so keep the current value
            var currentStatus = exam.Status;
 
            _mapper.Map(updateExamDto, exam);
            exam.Status = currentStatus;
 
            await _unitOfWork.Repository<Exam>().UpdateAsync(exam);
            await _unitOfWork.SaveChangesAsync();
 
            await InvalidateExamCaches(exam.CourseId);
            await _cacheService.RemoveAsync($"exam:{examId}");
 
            return _mapper.Map<ExamDto>(exam);
        }
 
        public async Task<ExamDto> SetExamStatusAsync(int examId, int doctorId, string status)
        {
            if (status != "Active" && status != "Draft")
                throw new Exception("Invalid status");
 
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
            if (exam == null || exam.DoctorId != doctorId)
                throw new Exception("Access denied or exam not found");
 
            if (status == "Active")
            {
                var questions = await _unitOfWork.Repository<Question>()
                    .FindAsync(q => q.ExamId == examId);
                if (!questions.Any())
                    throw new Exception("Add at least one question before publishing");
            }
 
            exam.Status = status;
            await _unitOfWork.Repository<Exam>().UpdateAsync(exam);
            await _unitOfWork.SaveChangesAsync();
 
            await InvalidateExamCaches(exam.CourseId);
            await _cacheService.RemoveAsync($"exam:{examId}");
 
            if (status == "Active")
            {
                var course = await _unitOfWork.Repository<Course>()
                    .GetByIdAsync(exam.CourseId, c => c.RegisteredStudents);
 
                var students = course?.RegisteredStudents?.ToList() ?? new List<Student>();
                _logger.LogInformation("Publishing exam {ExamId}, notifying {Count} students", examId, students.Count);
 
                foreach (var s in students)
                    await _notifications.SendNotificationAsync(s.Id, $"New exam published: {exam.Title}", "Exam");
            }
 
            _logger.LogInformation("Exam {ExamId} status changed to {Status}", examId, status);
            return _mapper.Map<ExamDto>(exam);
        }
 
        public async Task DeleteExamAsync(int examId)
        {
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
            if (exam == null)
                throw new Exception("Exam not found");
 
            await _unitOfWork.Repository<Exam>().DeleteAsync(exam);
            await _unitOfWork.SaveChangesAsync();
 
            await InvalidateExamCaches(exam.CourseId);
        }
 
        public async Task<QuestionDto> AddQuestionAsync(CreateQuestionDto questionDto)
        {
            try
            {
                _logger.LogInformation("Starting to add question: {@QuestionDto}", questionDto);
 
                Question question;
 
                if (questionDto.QuestionType == "MCQ")
                {
                    // MCQ: options are mandatory, ModelAnswer is NOT needed
                    if (questionDto.Options == null || !questionDto.Options.Any())
                        throw new Exception("MCQ question must have at least one option");
 
                    if (!questionDto.Options.Any(o => o.IsCorrect))
                        throw new Exception("MCQ question must have at least one correct option");
 
                    _logger.LogInformation("Creating MCQ question");
                    var mcqQuestion = _mapper.Map<MCQQuestion>(questionDto);
                    mcqQuestion.Options = new List<MCQOption>();
 
                    _logger.LogInformation("Adding {OptionCount} options", questionDto.Options.Count);
                    foreach (var optionDto in questionDto.Options)
                    {
                        var option = new MCQOption
                        {
                            OptionText = optionDto.OptionText,
                            IsCorrect = optionDto.IsCorrect
                        };
                        mcqQuestion.Options.Add(option);
                        _logger.LogInformation("Added option: {OptionText}, IsCorrect: {IsCorrect}",
                            option.OptionText, option.IsCorrect);
                    }
 
                    question = mcqQuestion;
                }
                else if (questionDto.QuestionType == "Written")
                {
                    // Written: ModelAnswer is mandatory, Options are NOT needed
                    if (string.IsNullOrWhiteSpace(questionDto.ModelAnswer))
                        throw new Exception("Model answer is required for written questions");
 
                    if (string.IsNullOrWhiteSpace(questionDto.EvaluationModel))
                        questionDto.EvaluationModel = "Default";
 
                    // Use the configured default threshold when the doctor didn't pick one
                    var cfg = (await _unitOfWork.Repository<AIConfig>()
                        .FindAsync(c => c.ParameterName == "DefaultWrittenThreshold")).FirstOrDefault();
 
                    if (cfg != null && questionDto.AIThreshold <= 0)
                        questionDto.AIThreshold = cfg.Threshold;
                    else if (questionDto.AIThreshold <= 0)
                        questionDto.AIThreshold = 0.7;
 
                    _logger.LogInformation("Creating Written question");
                    question = _mapper.Map<WrittenQuestion>(questionDto);
                }
                else
                {
                    throw new Exception("Invalid question type");
                }
 
                await _unitOfWork.Repository<Question>().AddAsync(question);
                await _unitOfWork.SaveChangesAsync();
 
                _logger.LogInformation("Question saved successfully. ID: {QuestionId}", question.Id);
 
                await _cacheService.RemoveAsync($"exam-questions:{questionDto.ExamId}");
                await _cacheService.RemoveAsync($"exam:{questionDto.ExamId}");
 
                var result = _mapper.Map<QuestionDto>(question);
                _logger.LogInformation("Mapped result: {@Result}", result);
 
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add question");
                throw;
            }
        }
 
        public async Task DeleteQuestionAsync(int questionId)
        {
            var question = await _unitOfWork.Repository<Question>().GetByIdAsync(questionId);
            if (question == null)
                throw new Exception("Question not found");
 
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(question.ExamId);
 
            await _unitOfWork.Repository<Question>().DeleteAsync(question);
            await _unitOfWork.SaveChangesAsync();
 
            if (exam != null)
            {
                var remainingQuestions = await _unitOfWork.Repository<Question>()
                    .FindAsync(q => q.ExamId == exam.Id);
 
                exam.TotalMarks = remainingQuestions.Sum(q => q.Marks);
                await _unitOfWork.Repository<Exam>().UpdateAsync(exam);
                await _unitOfWork.SaveChangesAsync();
            }
 
            await _cacheService.RemoveAsync($"exam-questions:{question.ExamId}");
            await _cacheService.RemoveAsync($"exam:{question.ExamId}");
 
            _logger.LogInformation("Question deleted: {QuestionId}", questionId);
        }
 
        public async Task<List<StudentExamResultDto>> GetExamResultsAsync(int examId, int doctorId)
        {
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
            if (exam == null || exam.DoctorId != doctorId)
                throw new Exception("Access denied or exam not found");
 
            // Completed and TimedOut attempts both count; only unfinished ones are hidden
            var sessions = await _unitOfWork.Repository<ExamSession>()
                .FindAsync(es => es.ExamId == examId && es.Status != "InProgress",
                    es => es.Student!);
 
            var questions = await _unitOfWork.Repository<Question>()
                .FindAsync(q => q.ExamId == examId);
            var totalQuestions = questions.Count();
 
            return sessions.Select(s =>
            {
                var correctAnswers = 0;
                if (totalQuestions > 0 && s.TotalMarks > 0)
                {
                    var marksPerQuestion = (double)s.TotalMarks / totalQuestions;
                    correctAnswers = (int)Math.Round(s.TotalScore / marksPerQuestion);
                }
 
                return new StudentExamResultDto
                {
                    SessionId = s.Id,
                    StudentId = s.StudentId,
                    StudentName = s.Student != null
                        ? $"{s.Student.FirstName} {s.Student.LastName}"
                        : "Unknown",
                    TotalScore = s.TotalScore,
                    Percentage = s.Percentage,
                    IsPassed = s.IsPassed,
                    CorrectAnswers = correctAnswers,
                    TotalQuestions = totalQuestions,
                    Status = s.Status,
                    SubmittedAt = s.SubmittedAt ?? s.EndTime ?? s.StartTime
                };
            }).ToList();
        }
 
        public async Task<ExamAnalysisDto> GetExamAnalysisAsync(int examId, int doctorId)
        {
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
            if (exam == null || exam.DoctorId != doctorId)
                throw new Exception("Access denied or exam not found");
 
            var sessions = await _unitOfWork.Repository<ExamSession>()
                .FindAsync(es => es.ExamId == examId && es.Status != "InProgress");
 
            var totalStudents = sessions.Count();
            var averageScore = totalStudents > 0 ? sessions.Average(s => s.TotalScore) : 0;
            var passRate = totalStudents > 0
                ? (sessions.Count(s => s.IsPassed) / (double)totalStudents * 100)
                : 0;
 
            return new ExamAnalysisDto
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                TotalStudents = totalStudents,
                AverageScore = averageScore,
                PassRate = passRate,
                CommonMistakes = new List<CommonMistakeDto>()
            };
        }
 
        public async Task<StudentExamDetailsDto> GetStudentExamDetailsAsync(int sessionId, int doctorId)
        {
            var session = await _unitOfWork.Repository<ExamSession>()
                .GetByIdAsync(sessionId, s => s.Student!, s => s.Exam!);
 
            if (session == null || session.Exam == null || session.Exam.DoctorId != doctorId)
                throw new Exception("Access denied or session not found");
 
            var answers = await _sessionService.GetExamAnswersAsync(sessionId);
 
            return new StudentExamDetailsDto
            {
                SessionId = sessionId,
                StudentName = session.Student != null
                    ? $"{session.Student.FirstName} {session.Student.LastName}"
                    : "Unknown",
                TotalScore = session.TotalScore,
                Percentage = session.Percentage,
                IsPassed = session.IsPassed,
                Answers = answers.Questions ?? new List<QuestionWithAnswerDto>(),
                SubmittedAt = session.SubmittedAt ?? session.EndTime ?? session.StartTime
            };
        }
 
        public async Task EnsureOwnerAsync(int examId, int doctorId)
        {
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
            if (exam == null || exam.DoctorId != doctorId)
                throw new UnauthorizedAccessException("Access denied or exam not found");
        }
 
        public async Task EnsureQuestionOwnerAsync(int questionId, int doctorId)
        {
            var q = await _unitOfWork.Repository<Question>().GetByIdAsync(questionId);
            if (q == null) throw new Exception("Question not found");
            await EnsureOwnerAsync(q.ExamId, doctorId);
        }
 
        private async Task InvalidateExamCaches(int courseId)
        {
            await _cacheService.RemoveAsync($"course-exams:{courseId}");
        }
    }
}
 