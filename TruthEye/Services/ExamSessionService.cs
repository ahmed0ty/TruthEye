// using AutoMapper;
// using TruthEye.DTOs;
// using TruthEye.Entities;
// using TruthEye.UnitOfWorkk;

// namespace TruthEye.Services
// {
//     public class ExamSessionService : IExamSessionService
//     {
//         private readonly IUnitOfWork _unitOfWork;
//         private readonly IMapper _mapper;
//         private readonly IAIEvaluationService _aiEvaluationService;
//         private readonly ILogger<ExamSessionService> _logger;

//         public ExamSessionService(
//             IUnitOfWork unitOfWork,
//             IMapper mapper,
//             IAIEvaluationService aiEvaluationService,
//             ILogger<ExamSessionService> logger)
//         {
//             _unitOfWork = unitOfWork;
//             _mapper = mapper;
//             _aiEvaluationService = aiEvaluationService;
//             _logger = logger;
//         }
//         public async Task<ExamSessionDto> GetExamSessionByIdAsync(int sessionId)
//         {
//             var session = await _unitOfWork.Repository<ExamSession>()
//                 .GetByIdAsync(sessionId, s => s.Student!, s => s.Exam!);

//             if (session == null)
//                 throw new Exception("Session not found");

//             return _mapper.Map<ExamSessionDto>(session);
//         }


//         public async Task<ExamSessionDto> StartExamSessionAsync(StartExamSessionDto startDto)
//         {
//             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(startDto.ExamId);
//             if (exam == null)
//                 throw new Exception("Exam not found");

//             var existingSessions = await _unitOfWork.Repository<ExamSession>()
//                 .FindAsync(es => es.StudentId == startDto.StudentId && es.ExamId == startDto.ExamId);

//             if (existingSessions.Any())
//             {
//                 throw new Exception("You have already attempted this exam. Only one attempt is allowed.");
//             }

          

//             var session = new ExamSession
//             {
//                 StudentId = startDto.StudentId,
//                 ExamId = startDto.ExamId,
//                 StartTime = DateTime.UtcNow,
//                 Status = "InProgress",
//                 AttemptNumber = 1, 
//                 TotalMarks = exam.TotalMarks
//             };

//             await _unitOfWork.Repository<ExamSession>().AddAsync(session);
//             await _unitOfWork.SaveChangesAsync();

//             return _mapper.Map<ExamSessionDto>(session);
//         }

//         public async Task<ExamSessionDto> ContinueExamSessionAsync(int sessionId)
//         {
//             var session = await _unitOfWork.Repository<ExamSession>()
//                 .GetByIdAsync(sessionId, s => s.Exam!, s => s.Answers!);

//             if (session == null)
//                 throw new Exception("Session not found");

//             if (session.Status != "InProgress")
//                 throw new Exception("Session is not in progress");

//             var exam = session.Exam;
//             if (DateTime.UtcNow > exam.EndTime)
//             {
//                 await AutoSubmitSession(session);
//                 throw new Exception("Exam time has expired");
//             }

//             var sessionDto = _mapper.Map<ExamSessionDto>(session);
//             sessionDto.CurrentAnswers = await GetCurrentAnswersAsync(sessionId);

//             return sessionDto;
//         }
//         public async Task<QuestionNavigationDto> MoveToNextQuestionAsync(int sessionId, int currentQuestionId, StudentAnswerDto answerDto)
//         {
//             var session = await _unitOfWork.Repository<ExamSession>()
//                 .GetByIdAsync(sessionId, s => s.Exam!);

//             if (session == null)
//                 throw new Exception("Session not found");

//             await SaveAnswerAsync(sessionId, answerDto);

//             var nextQuestion = await GetNextQuestionAsync(session.ExamId, currentQuestionId);
//             var currentQuestionNumber = await GetCurrentQuestionNumberAsync(session.ExamId, currentQuestionId);
//             var totalQuestions = await GetTotalQuestionsAsync(session.ExamId);

//             return new QuestionNavigationDto
//             {
//                 NextQuestion = nextQuestion,
//                 HasNext = nextQuestion != null,
//                 CurrentQuestionNumber = currentQuestionNumber,
//                 TotalQuestions = totalQuestions
//             };
//         }

//         public async Task<ExamResultDto> SubmitExamSessionAsync(int sessionId)
//         {
//             var session = await _unitOfWork.Repository<ExamSession>()
//        .GetByIdAsync(sessionId, s => s.Exam!);

//             if (session == null)
//                 throw new Exception("Session not found");

//             if (session.Status != "InProgress")
//                 throw new Exception("Session is not in progress");

//             var exam = session.Exam!;
//             var questions = (await _unitOfWork.Repository<Question>()
//                 .FindAsync(q => q.ExamId == exam.Id,
//                     q => (q as MCQQuestion)!.Options!))
//                 .ToList();

//             await _unitOfWork.BeginTransactionAsync();

//             try
//             {
//                 double totalScore = 0;
//                 int correctAnswers = 0;

//                 var studentAnswers = await _unitOfWork.Repository<Answer>()
//                     .FindAsync(a => a.ExamSessionId == session.Id);

//                 foreach (var answer in studentAnswers)
//                 {
//                     var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
//                     if (question == null) continue;

//                     if (question is MCQQuestion mcqQuestion)
//                     {
//                         var correctOption = mcqQuestion.Options?.FirstOrDefault(o => o.IsCorrect);
//                         if (correctOption != null && correctOption.Id == answer.SelectedOptionId)
//                         {
//                             answer.IsCorrect = true;
//                             answer.MarksObtained = question.Marks;
//                             totalScore += question.Marks;
//                             correctAnswers++;
//                         }
//                     }
//                     else if (question is WrittenQuestion writtenQuestion)
//                     {
//                         var aiResult = await _aiEvaluationService.EvaluateWrittenAnswerAsync(
//                             question.QuestionText,
//                             writtenQuestion.ModelAnswer,
//                             answer.AnswerText,
//                             writtenQuestion.AIThreshold
//                         );

//                         answer.IsCorrect = aiResult.IsCorrect;
//                         answer.MarksObtained = aiResult.MarksObtained * question.Marks;
//                         answer.AISimilarityScore = aiResult.SimilarityScore;
//                         answer.AIFeedback = aiResult.Feedback;

//                         totalScore += answer.MarksObtained;
//                         if (aiResult.IsCorrect) correctAnswers++;
//                     }

//                     await _unitOfWork.Repository<Answer>().UpdateAsync(answer);
//                 }

//                 session.EndTime = DateTime.UtcNow;
//                 session.SubmittedAt = DateTime.UtcNow;
//                 session.Status = "Completed";
//                 session.TotalScore = totalScore;
//                 session.TotalMarks = exam.TotalMarks;
//                 session.Percentage = session.TotalMarks > 0 ? (totalScore / session.TotalMarks * 100) : 0;
//                 session.IsPassed = session.Percentage >= exam.PassingScore;

//                 await _unitOfWork.Repository<ExamSession>().UpdateAsync(session);
//                 await _unitOfWork.CommitTransactionAsync();

//                 var aiLogs = await _unitOfWork.Repository<AILog>()
//                     .FindAsync(log => log.ExamSessionId == session.Id);

//                 return new ExamResultDto
//                 {
//                     SessionId = session.Id,
//                     ExamTitle = exam.Title,
//                     TotalScore = session.TotalScore,
//                     TotalMarks = session.TotalMarks,
//                     Percentage = session.Percentage,
//                     IsPassed = session.IsPassed,
//                     CorrectAnswers = correctAnswers,
//                     TotalQuestions = questions.Count,
//                     Duration = session.EndTime.Value - session.StartTime,
//                     AIAlertsCount = aiLogs.Count(),
//                     SubmittedAt = session.EndTime.Value
//                 };
//             }
//             catch
//             {
//                 await _unitOfWork.RollbackTransactionAsync();
//                 throw;
//             }
//         }

//         public async Task<ExamResultDto> GetExamResultAsync(int sessionId)
//         {
//             var session = await _unitOfWork.Repository<ExamSession>().GetByIdAsync(sessionId, s => s.Exam);
//             if (session == null)
//                 throw new Exception("Session not found");

//             if (session.Status == "InProgress")
//                 throw new Exception("Exam is still in progress");

//             var answers = (await _unitOfWork.Repository<Answer>()
//                 .FindAsync(a => a.ExamSessionId == sessionId)).ToList();
//             var aiLogs = await _unitOfWork.Repository<AILog>()
//                 .FindAsync(log => log.ExamSessionId == sessionId);

//             return new ExamResultDto
//             {
//                 SessionId = session.Id,
//                 ExamTitle = session.Exam.Title,
//                 TotalScore = session.TotalScore,
//                 TotalMarks = session.TotalMarks,
//                 Percentage = session.Percentage,
//                 IsPassed = session.IsPassed,
//                 CorrectAnswers = answers.Count(a => a.IsCorrect),
//                 TotalQuestions = answers.Count,
//                 Duration = session.EndTime.HasValue ? session.EndTime.Value - session.StartTime : TimeSpan.Zero,
//                 AIAlertsCount = aiLogs.Count(),
//                 SubmittedAt = session.EndTime ?? DateTime.UtcNow
//             };
//         }
//         private async Task SaveAnswerAsync(int sessionId, StudentAnswerDto answerDto)
//         {
//             var existingAnswer = await _unitOfWork.Repository<Answer>()
//                 .FindAsync(a => a.ExamSessionId == sessionId && a.QuestionId == answerDto.QuestionId);

//             if (existingAnswer.Any())
//                 throw new Exception("Cannot modify answer after submission");

//             var answer = new Answer
//             {
//                 ExamSessionId = sessionId,
//                 QuestionId = answerDto.QuestionId,
//                 AnswerText = answerDto.AnswerText ?? string.Empty,
//                 SelectedOptionId = answerDto.SelectedOptionId,
//                 AnsweredAt = DateTime.UtcNow,
//                 IsCorrect = false,
//                 MarksObtained = 0
//             };

//             await _unitOfWork.Repository<Answer>().AddAsync(answer);
//             await _unitOfWork.SaveChangesAsync();
//         }
//         private async Task<QuestionDto?> GetNextQuestionAsync(int examId, int currentQuestionId)
//         {
//             var questions = (await _unitOfWork.Repository<Question>()
//                 .FindAsync(q => q.ExamId == examId))
//                 .OrderBy(q => q.Id)
//                 .ToList();

//             var currentIndex = questions.FindIndex(q => q.Id == currentQuestionId);
//             return currentIndex + 1 < questions.Count ? _mapper.Map<QuestionDto>(questions[currentIndex + 1]) : null;
//         }

//         private async Task<int> GetCurrentQuestionNumberAsync(int examId, int currentQuestionId)
//         {
//             var questions = (await _unitOfWork.Repository<Question>()
//                 .FindAsync(q => q.ExamId == examId))
//                 .OrderBy(q => q.Id)
//                 .ToList();

//             return questions.FindIndex(q => q.Id == currentQuestionId) + 1;
//         }
//         private async Task<int> GetTotalQuestionsAsync(int examId)
//         {
//             var questions = await _unitOfWork.Repository<Question>()
//                 .FindAsync(q => q.ExamId == examId);
//             return questions.Count();
//         }

//         public async Task<List<StudentAnswerDto>> GetCurrentAnswersAsync(int sessionId)
//         {
//             var answers = await _unitOfWork.Repository<Answer>()
//                 .FindAsync(a => a.ExamSessionId == sessionId);

//             return answers.Select(a => new StudentAnswerDto
//             {
//                 QuestionId = a.QuestionId,
//                 AnswerText = a.AnswerText,
//                 SelectedOptionId = a.SelectedOptionId
//             }).ToList();
//         }
//         public async Task<ExamAnswersDto> GetExamAnswersAsync(int sessionId)
//         {
//             var session = await _unitOfWork.Repository<ExamSession>().GetByIdAsync(sessionId, s => s.Exam!);
//             if (session == null)
//                 throw new Exception("Session not found");

//             var exam = session.Exam;

//             bool canViewAnswers = DateTime.UtcNow > exam.EndTime || session.Status == "Completed";

//             if (!canViewAnswers)
//             {
//                 return new ExamAnswersDto
//                 {
//                     ExamId = exam.Id,
//                     ExamTitle = exam.Title,
//                     CanViewAnswers = false,
//                     Message = GetViewAnswersMessage(session, exam),
//                     Questions = null,
//                     StudentScore = 0,
//                     TotalMarks = 0,
//                     Percentage = 0,
//                     IsPassed = false
//                 };
//             }

//             var questions = (await _unitOfWork.Repository<Question>()
//                 .FindAsync(q => q.ExamId == exam.Id,
//                     q => (q as MCQQuestion)!.Options!))
//                 .ToList();

//             var studentAnswers = (await _unitOfWork.Repository<Answer>()
//                 .FindAsync(a => a.ExamSessionId == sessionId))
//                 .ToList();

//             var questionsWithAnswers = new List<QuestionWithAnswerDto>();

//             foreach (var question in questions)
//             {
//                 var studentAnswer = studentAnswers.FirstOrDefault(a => a.QuestionId == question.Id);

//                 var questionDto = new QuestionWithAnswerDto
//                 {
//                     QuestionId = question.Id,
//                     QuestionText = question.QuestionText,
//                     QuestionType = question.QuestionType,
//                     Marks = question.Marks,
//                     IsCorrect = studentAnswer?.IsCorrect ?? false,
//                     MarksObtained = studentAnswer?.MarksObtained ?? 0,
//                     StudentAnswer = studentAnswer?.AnswerText,
//                     AISimilarityScore = studentAnswer?.AISimilarityScore,
//                     AIFeedback = studentAnswer?.AIFeedback
//                 };

//                 if (question is MCQQuestion mcqQuestion)
//                 {
//                     questionDto.Options = mcqQuestion.Options?.Select(o => new MCQOptionDto
//                     {
//                         Id = o.Id, 
//                         OptionText = o.OptionText,
//                         IsCorrect = o.IsCorrect
//                     }).ToList() ?? new List<MCQOptionDto>();

//                     questionDto.CorrectOptionId = mcqQuestion.Options?.FirstOrDefault(o => o.IsCorrect)?.Id;
//                     questionDto.StudentSelectedOptionId = studentAnswer?.SelectedOptionId;
//                 }
//                 else if (question is WrittenQuestion writtenQuestion)
//                 {
//                     questionDto.ModelAnswer = writtenQuestion.ModelAnswer;
//                 }

//                 questionsWithAnswers.Add(questionDto);
//             }

//             return new ExamAnswersDto
//             {
//                 ExamId = exam.Id,
//                 ExamTitle = exam.Title,
//                 CanViewAnswers = true,
//                 Message = "Exam answers",
//                 Questions = questionsWithAnswers,
//                 StudentScore = session.TotalScore,
//                 TotalMarks = session.TotalMarks,
//                 Percentage = session.Percentage,
//                 IsPassed = session.IsPassed
//             };
//         }

//         private string GetViewAnswersMessage(ExamSession session, Exam exam)
//         {
//             if (DateTime.UtcNow <= exam.EndTime && session.Status == "InProgress")
//             {
//                 var timeLeft = exam.EndTime - DateTime.UtcNow;
//                 return $"The exam is not over yet. Time remaining: {timeLeft:hh\\:mm\\:ss}"; 
//             }
//             else if (session.Status == "InProgress")
//             {
//                 return "Please submit the exam first to view the results.";
//             }
//             else
//             {
//                 return "Answers are not currently available";
//             }
//         }
      

//         private async Task AutoSubmitSession(ExamSession session)
//         {
//             var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(session.ExamId);

//             session.EndTime = DateTime.UtcNow;
//             session.Status = "TimedOut";

//             var answers = await _unitOfWork.Repository<Answer>()
//                 .FindAsync(a => a.ExamSessionId == session.Id);

//             session.TotalScore = answers.Sum(a => a.MarksObtained);
//             session.Percentage = session.TotalScore / session.TotalMarks * 100;
//             session.IsPassed = session.Percentage >= exam.PassingScore;

//             await _unitOfWork.Repository<ExamSession>().UpdateAsync(session);
//             await _unitOfWork.SaveChangesAsync();
//         }

//         public async Task<IEnumerable<ExamSessionDto>> GetStudentExamHistoryAsync(int studentId)
//         {
//             var sessions = await _unitOfWork.Repository<ExamSession>()
//                 .FindAsync(es => es.StudentId == studentId,
//                     es => es.Exam);

//             return _mapper.Map<IEnumerable<ExamSessionDto>>(sessions);
//         }

//         public async Task<IEnumerable<ExamSessionDto>> GetActiveSessionsAsync()
//         {
//             var sessions = await _unitOfWork.Repository<ExamSession>()
//                 .FindAsync(es => es.Status == "InProgress",
//                     es => es.Exam, es => es.Student);

//             return _mapper.Map<IEnumerable<ExamSessionDto>>(sessions);
//         }
//         public async Task<ExamTakeDto> GetExamTakeAsync(int sessionId, int studentId)
// {
//     var session = await _unitOfWork.Repository<ExamSession>()
//         .GetByIdAsync(sessionId, s => s.Exam!);

//     if (session == null || session.StudentId != studentId)
//         throw new Exception("Session not found");

//     if (session.Status != "InProgress")
//         throw new Exception("Session is not in progress");

//     var exam = session.Exam!;
//     var remaining = ComputeRemainingSeconds(session, exam);

//     if (remaining <= 0)
//     {
//         await AutoSubmitSession(session);
//         throw new Exception("Exam time has expired");
//     }

//     var questions = (await _unitOfWork.Repository<Question>()
//         .FindAsync(q => q.ExamId == exam.Id, q => (q as MCQQuestion)!.Options!))
//         .OrderBy(q => q.Id)
//         .ToList();

//     var answers = await GetCurrentAnswersAsync(sessionId);

//     return new ExamTakeDto
//     {
//         SessionId = session.Id,
//         ExamId = exam.Id,
//         ExamTitle = exam.Title,
//         Instructions = exam.Instructions,
//         RemainingSeconds = remaining,
//         ServerTime = DateTime.UtcNow,
//         Questions = questions.Select(q => new StudentQuestionDto
//         {
//             Id = q.Id,
//             QuestionText = q.QuestionText,
//             QuestionType = q.QuestionType,
//             Marks = q.Marks,
//             Options = q is MCQQuestion mcq
//                 ? mcq.Options?.OrderBy(o => o.Id)
//                     .Select(o => new StudentOptionDto { Id = o.Id, OptionText = o.OptionText })
//                     .ToList()
//                 : null
//         }).ToList(),
//         Answers = answers
//     };
// }

// private static int ComputeRemainingSeconds(ExamSession session, Exam exam)
// {
//     var byDuration = session.StartTime.AddMinutes(exam.Duration);
//     var deadline = byDuration < exam.EndTime ? byDuration : exam.EndTime;
//     return (int)Math.Max(0, (deadline - DateTime.UtcNow).TotalSeconds);
// }
//     }
// }


using AutoMapper;
using TruthEye.DTOs;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;
 
namespace TruthEye.Services
{
    public class ExamSessionService : IExamSessionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAIEvaluationService _aiEvaluationService;
        private readonly ILogger<ExamSessionService> _logger;
        private readonly INotificationService _notifications;
 
        public ExamSessionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAIEvaluationService aiEvaluationService,
            ILogger<ExamSessionService> logger,
            INotificationService notifications)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _aiEvaluationService = aiEvaluationService;
            _logger = logger;
            _notifications = notifications;
        }
 
        public async Task<ExamSessionDto> GetExamSessionByIdAsync(int sessionId)
        {
            var session = await _unitOfWork.Repository<ExamSession>()
                .GetByIdAsync(sessionId, s => s.Student!, s => s.Exam!);
 
            if (session == null)
                throw new Exception("Session not found");
 
            return _mapper.Map<ExamSessionDto>(session);
        }
 
        public async Task<ExamSessionDto> StartExamSessionAsync(StartExamSessionDto startDto)
        {
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(startDto.ExamId);
            if (exam == null)
                throw new Exception("Exam not found");
 
            var existingSessions = await _unitOfWork.Repository<ExamSession>()
                .FindAsync(es => es.StudentId == startDto.StudentId && es.ExamId == startDto.ExamId);
 
            if (existingSessions.Any())
            {
                throw new Exception("You have already attempted this exam. Only one attempt is allowed.");
            }
 
            var session = new ExamSession
            {
                StudentId = startDto.StudentId,
                ExamId = startDto.ExamId,
                StartTime = DateTime.UtcNow,
                Status = "InProgress",
                AttemptNumber = 1,
                TotalMarks = exam.TotalMarks
            };
 
            await _unitOfWork.Repository<ExamSession>().AddAsync(session);
            await _unitOfWork.SaveChangesAsync();
 
            return _mapper.Map<ExamSessionDto>(session);
        }
 
        public async Task<ExamSessionDto> ContinueExamSessionAsync(int sessionId)
        {
            var session = await _unitOfWork.Repository<ExamSession>()
                .GetByIdAsync(sessionId, s => s.Exam!, s => s.Answers!);
 
            if (session == null)
                throw new Exception("Session not found");
 
            if (session.Status != "InProgress")
                throw new Exception("Session is not in progress");
 
            var exam = session.Exam;
            if (DateTime.UtcNow > exam.EndTime)
            {
                await AutoSubmitSession(session);
                throw new Exception("Exam time has expired");
            }
 
            var sessionDto = _mapper.Map<ExamSessionDto>(session);
            sessionDto.CurrentAnswers = await GetCurrentAnswersAsync(sessionId);
 
            return sessionDto;
        }
 
        public async Task<QuestionNavigationDto> MoveToNextQuestionAsync(int sessionId, int currentQuestionId, StudentAnswerDto answerDto)
        {
            var session = await _unitOfWork.Repository<ExamSession>()
                .GetByIdAsync(sessionId, s => s.Exam!);
 
            if (session == null)
                throw new Exception("Session not found");
 
            if (session.Status != "InProgress")
                throw new Exception("Session is not in progress");
 
            if (ComputeRemainingSeconds(session, session.Exam!) <= 0)
                throw new Exception("Exam time has expired");
 
            await SaveAnswerAsync(sessionId, answerDto);
 
            var nextQuestion = await GetNextQuestionAsync(session.ExamId, currentQuestionId);
            var currentQuestionNumber = await GetCurrentQuestionNumberAsync(session.ExamId, currentQuestionId);
            var totalQuestions = await GetTotalQuestionsAsync(session.ExamId);
 
            return new QuestionNavigationDto
            {
                NextQuestion = nextQuestion,
                HasNext = nextQuestion != null,
                CurrentQuestionNumber = currentQuestionNumber,
                TotalQuestions = totalQuestions
            };
        }
 
        public async Task<ExamResultDto> SubmitExamSessionAsync(int sessionId)
        {
            var session = await _unitOfWork.Repository<ExamSession>()
                .GetByIdAsync(sessionId, s => s.Exam!);
 
            if (session == null)
                throw new Exception("Session not found");
 
            if (session.Status != "InProgress")
                throw new Exception("Session is not in progress");
 
            var exam = session.Exam!;
            var questions = (await _unitOfWork.Repository<Question>()
                .FindAsync(q => q.ExamId == exam.Id,
                    q => (q as MCQQuestion)!.Options!))
                .ToList();
 
            await _unitOfWork.BeginTransactionAsync();
 
            ExamResultDto result;
            try
            {
                var (totalScore, correctAnswers) = await GradeAnswersAsync(session, questions);
 
                session.EndTime = DateTime.UtcNow;
                session.SubmittedAt = DateTime.UtcNow;
                session.Status = "Completed";
                session.TotalScore = totalScore;
                session.TotalMarks = exam.TotalMarks;
                session.Percentage = session.TotalMarks > 0 ? (totalScore / session.TotalMarks * 100) : 0;
                session.IsPassed = session.Percentage >= exam.PassingScore;
 
                await _unitOfWork.Repository<ExamSession>().UpdateAsync(session);
                await _unitOfWork.CommitTransactionAsync();
 
                var aiLogs = await _unitOfWork.Repository<AILog>()
                    .FindAsync(log => log.ExamSessionId == session.Id);
 
                result = new ExamResultDto
                {
                    SessionId = session.Id,
                    ExamTitle = exam.Title,
                    TotalScore = session.TotalScore,
                    TotalMarks = session.TotalMarks,
                    Percentage = session.Percentage,
                    IsPassed = session.IsPassed,
                    CorrectAnswers = correctAnswers,
                    TotalQuestions = questions.Count,
                    Duration = session.EndTime.Value - session.StartTime,
                    AIAlertsCount = aiLogs.Count(),
                    SubmittedAt = session.EndTime.Value
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
 
            // Notifications go out only after the grade is saved, and a failure here never fails the submit
            try
            {
                await _notifications.SendNotificationAsync(session.StudentId, $"Your result for {exam.Title} is ready", "Result");
                await _notifications.SendNotificationAsync(exam.DoctorId, $"A student submitted: {exam.Title}", "Submission");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Couldn't send submit notifications for session {SessionId}", session.Id);
            }
 
            return result;
        }
 
        public async Task<ExamResultDto> GetExamResultAsync(int sessionId)
        {
            var session = await _unitOfWork.Repository<ExamSession>().GetByIdAsync(sessionId, s => s.Exam);
            if (session == null)
                throw new Exception("Session not found");
 
            if (session.Status == "InProgress")
                throw new Exception("Exam is still in progress");
 
            var answers = (await _unitOfWork.Repository<Answer>()
                .FindAsync(a => a.ExamSessionId == sessionId)).ToList();
            var aiLogs = await _unitOfWork.Repository<AILog>()
                .FindAsync(log => log.ExamSessionId == sessionId);
 
            return new ExamResultDto
            {
                SessionId = session.Id,
                ExamTitle = session.Exam.Title,
                TotalScore = session.TotalScore,
                TotalMarks = session.TotalMarks,
                Percentage = session.Percentage,
                IsPassed = session.IsPassed,
                CorrectAnswers = answers.Count(a => a.IsCorrect),
                TotalQuestions = answers.Count,
                Duration = session.EndTime.HasValue ? session.EndTime.Value - session.StartTime : TimeSpan.Zero,
                AIAlertsCount = aiLogs.Count(),
                SubmittedAt = session.EndTime ?? DateTime.UtcNow
            };
        }
 
        private async Task SaveAnswerAsync(int sessionId, StudentAnswerDto answerDto)
        {
            var existingAnswer = await _unitOfWork.Repository<Answer>()
                .FindAsync(a => a.ExamSessionId == sessionId && a.QuestionId == answerDto.QuestionId);
 
            if (existingAnswer.Any())
                throw new Exception("Cannot modify answer after submission");
 
            var answer = new Answer
            {
                ExamSessionId = sessionId,
                QuestionId = answerDto.QuestionId,
                AnswerText = answerDto.AnswerText ?? string.Empty,
                SelectedOptionId = answerDto.SelectedOptionId,
                AnsweredAt = DateTime.UtcNow,
                IsCorrect = false,
                MarksObtained = 0
            };
 
            await _unitOfWork.Repository<Answer>().AddAsync(answer);
            await _unitOfWork.SaveChangesAsync();
        }
 
        private async Task<QuestionDto?> GetNextQuestionAsync(int examId, int currentQuestionId)
        {
            var questions = (await _unitOfWork.Repository<Question>()
                .FindAsync(q => q.ExamId == examId))
                .OrderBy(q => q.Id)
                .ToList();
 
            var currentIndex = questions.FindIndex(q => q.Id == currentQuestionId);
            return currentIndex + 1 < questions.Count ? _mapper.Map<QuestionDto>(questions[currentIndex + 1]) : null;
        }
 
        private async Task<int> GetCurrentQuestionNumberAsync(int examId, int currentQuestionId)
        {
            var questions = (await _unitOfWork.Repository<Question>()
                .FindAsync(q => q.ExamId == examId))
                .OrderBy(q => q.Id)
                .ToList();
 
            return questions.FindIndex(q => q.Id == currentQuestionId) + 1;
        }
 
        private async Task<int> GetTotalQuestionsAsync(int examId)
        {
            var questions = await _unitOfWork.Repository<Question>()
                .FindAsync(q => q.ExamId == examId);
            return questions.Count();
        }
 
        public async Task<List<StudentAnswerDto>> GetCurrentAnswersAsync(int sessionId)
        {
            var answers = await _unitOfWork.Repository<Answer>()
                .FindAsync(a => a.ExamSessionId == sessionId);
 
            return answers.Select(a => new StudentAnswerDto
            {
                QuestionId = a.QuestionId,
                AnswerText = a.AnswerText,
                SelectedOptionId = a.SelectedOptionId
            }).ToList();
        }
 
        public async Task<ExamAnswersDto> GetExamAnswersAsync(int sessionId)
        {
            var session = await _unitOfWork.Repository<ExamSession>().GetByIdAsync(sessionId, s => s.Exam!);
            if (session == null)
                throw new Exception("Session not found");
 
            var exam = session.Exam;
 
            // Any finished session (Completed or TimedOut) can be reviewed
            bool canViewAnswers = DateTime.UtcNow > exam.EndTime || session.Status != "InProgress";
 
            if (!canViewAnswers)
            {
                return new ExamAnswersDto
                {
                    ExamId = exam.Id,
                    ExamTitle = exam.Title,
                    CanViewAnswers = false,
                    Message = GetViewAnswersMessage(session, exam),
                    Questions = null,
                    StudentScore = 0,
                    TotalMarks = 0,
                    Percentage = 0,
                    IsPassed = false
                };
            }
 
            var questions = (await _unitOfWork.Repository<Question>()
                .FindAsync(q => q.ExamId == exam.Id,
                    q => (q as MCQQuestion)!.Options!))
                .ToList();
 
            var studentAnswers = (await _unitOfWork.Repository<Answer>()
                .FindAsync(a => a.ExamSessionId == sessionId))
                .ToList();
 
            var questionsWithAnswers = new List<QuestionWithAnswerDto>();
 
            foreach (var question in questions)
            {
                var studentAnswer = studentAnswers.FirstOrDefault(a => a.QuestionId == question.Id);
 
                var questionDto = new QuestionWithAnswerDto
                {
                    QuestionId = question.Id,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    Marks = question.Marks,
                    IsCorrect = studentAnswer?.IsCorrect ?? false,
                    MarksObtained = studentAnswer?.MarksObtained ?? 0,
                    StudentAnswer = studentAnswer?.AnswerText,
                    AISimilarityScore = studentAnswer?.AISimilarityScore,
                    AIFeedback = studentAnswer?.AIFeedback
                };
 
                if (question is MCQQuestion mcqQuestion)
                {
                    questionDto.Options = mcqQuestion.Options?.Select(o => new MCQOptionDto
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect
                    }).ToList() ?? new List<MCQOptionDto>();
 
                    questionDto.CorrectOptionId = mcqQuestion.Options?.FirstOrDefault(o => o.IsCorrect)?.Id;
                    questionDto.StudentSelectedOptionId = studentAnswer?.SelectedOptionId;
                }
                else if (question is WrittenQuestion writtenQuestion)
                {
                    questionDto.ModelAnswer = writtenQuestion.ModelAnswer;
                }
 
                questionsWithAnswers.Add(questionDto);
            }
 
            return new ExamAnswersDto
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                CanViewAnswers = true,
                Message = "Exam answers",
                Questions = questionsWithAnswers,
                StudentScore = session.TotalScore,
                TotalMarks = session.TotalMarks,
                Percentage = session.Percentage,
                IsPassed = session.IsPassed
            };
        }
 
        private string GetViewAnswersMessage(ExamSession session, Exam exam)
        {
            if (DateTime.UtcNow <= exam.EndTime && session.Status == "InProgress")
            {
                var timeLeft = exam.EndTime - DateTime.UtcNow;
                return $"The exam is not over yet. Time remaining: {timeLeft:hh\\:mm\\:ss}";
            }
            else if (session.Status == "InProgress")
            {
                return "Please submit the exam first to view the results.";
            }
            else
            {
                return "Answers are not currently available";
            }
        }
 
        // ------------------------------------------------------------------
        // Grading and time-out handling
        // ------------------------------------------------------------------
 
        private static DateTime DeadlineOf(ExamSession s, Exam e)
        {
            var byDuration = s.StartTime.AddMinutes(e.Duration);
            return byDuration < e.EndTime ? byDuration : e.EndTime;
        }
 
        // One grading routine shared by manual submit, auto submit and the background sweep
        private async Task<(double TotalScore, int Correct)> GradeAnswersAsync(ExamSession session, List<Question> questions)
        {
            double totalScore = 0;
            int correct = 0;
 
            var answers = await _unitOfWork.Repository<Answer>()
                .FindAsync(a => a.ExamSessionId == session.Id);
 
            foreach (var answer in answers)
            {
                var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                if (question == null) continue;
 
                if (question is MCQQuestion mcq)
                {
                    var right = mcq.Options?.FirstOrDefault(o => o.IsCorrect);
                    if (right != null && right.Id == answer.SelectedOptionId)
                    {
                        answer.IsCorrect = true;
                        answer.MarksObtained = question.Marks;
                        totalScore += question.Marks;
                        correct++;
                    }
                }
                else if (question is WrittenQuestion written)
                {
                    if (string.IsNullOrWhiteSpace(answer.AnswerText))
                    {
                        answer.IsCorrect = false;
                        answer.MarksObtained = 0;
                        answer.AISimilarityScore = 0;
                    }
                    else
                    {
                        var ai = await _aiEvaluationService.EvaluateWrittenAnswerAsync(
                            question.QuestionText, written.ModelAnswer, answer.AnswerText, written.AIThreshold);
 
                        answer.IsCorrect = ai.IsCorrect;
                        answer.MarksObtained = ai.MarksObtained * question.Marks;
                        answer.AISimilarityScore = ai.SimilarityScore;
                        answer.AIFeedback = ai.Feedback;
 
                        totalScore += answer.MarksObtained;
                        if (ai.IsCorrect) correct++;
                    }
                }
 
                await _unitOfWork.Repository<Answer>().UpdateAsync(answer);
            }
 
            return (totalScore, correct);
        }
 
        private async Task AutoSubmitSession(ExamSession session)
        {
            if (session.Status != "InProgress") return;
 
            var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(session.ExamId);
            if (exam == null) return;
 
            var questions = (await _unitOfWork.Repository<Question>()
                .FindAsync(q => q.ExamId == exam.Id, q => (q as MCQQuestion)!.Options!))
                .ToList();
 
            var (totalScore, _) = await GradeAnswersAsync(session, questions);
 
            var deadline = DeadlineOf(session, exam);
            var end = deadline < DateTime.UtcNow ? deadline : DateTime.UtcNow;
 
            session.EndTime = end;
            session.SubmittedAt = end;
            session.Status = "TimedOut";
            session.TotalScore = totalScore;
            session.TotalMarks = exam.TotalMarks;
            session.Percentage = session.TotalMarks > 0 ? totalScore / session.TotalMarks * 100 : 0;
            session.IsPassed = session.Percentage >= exam.PassingScore;
 
            await _unitOfWork.Repository<ExamSession>().UpdateAsync(session);
            await _unitOfWork.SaveChangesAsync();
        }
 
        // Called every minute by SessionExpiryWorker
        public async Task<int> ExpireOverdueSessionsAsync()
        {
            var open = await _unitOfWork.Repository<ExamSession>()
                .FindAsync(s => s.Status == "InProgress", s => s.Exam!);
 
            var now = DateTime.UtcNow;
            var grace = TimeSpan.FromMinutes(2); // the browser's own auto-submit gets the first chance
            var count = 0;
 
            foreach (var s in open.Where(x => x.Exam != null && DeadlineOf(x, x.Exam!) + grace < now).ToList())
            {
                try
                {
                    await AutoSubmitSession(s);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Couldn't auto-submit session {SessionId}", s.Id);
                }
            }
 
            return count;
        }
 
        public async Task<IEnumerable<ExamSessionDto>> GetStudentExamHistoryAsync(int studentId)
        {
            var sessions = await _unitOfWork.Repository<ExamSession>()
                .FindAsync(es => es.StudentId == studentId,
                    es => es.Exam);
 
            return _mapper.Map<IEnumerable<ExamSessionDto>>(sessions);
        }
 
        public async Task<IEnumerable<ExamSessionDto>> GetActiveSessionsAsync()
        {
            var sessions = await _unitOfWork.Repository<ExamSession>()
                .FindAsync(es => es.Status == "InProgress",
                    es => es.Exam, es => es.Student);
 
            return _mapper.Map<IEnumerable<ExamSessionDto>>(sessions);
        }
 
        public async Task<ExamTakeDto> GetExamTakeAsync(int sessionId, int studentId)
        {
            var session = await _unitOfWork.Repository<ExamSession>()
                .GetByIdAsync(sessionId, s => s.Exam!);
 
            if (session == null || session.StudentId != studentId)
                throw new Exception("Session not found");
 
            if (session.Status != "InProgress")
                throw new Exception("Session is not in progress");
 
            var exam = session.Exam!;
            var remaining = ComputeRemainingSeconds(session, exam);
 
            if (remaining <= 0)
            {
                await AutoSubmitSession(session);
                throw new Exception("Exam time has expired");
            }
 
            var questions = (await _unitOfWork.Repository<Question>()
                .FindAsync(q => q.ExamId == exam.Id, q => (q as MCQQuestion)!.Options!))
                .OrderBy(q => q.Id)
                .ToList();
 
            var answers = await GetCurrentAnswersAsync(sessionId);
 
            return new ExamTakeDto
            {
                SessionId = session.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                Instructions = exam.Instructions,
                RemainingSeconds = remaining,
                ServerTime = DateTime.UtcNow,
                Questions = questions.Select(q => new StudentQuestionDto
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Marks = q.Marks,
                    Options = q is MCQQuestion mcq
                        ? mcq.Options?.OrderBy(o => o.Id)
                            .Select(o => new StudentOptionDto { Id = o.Id, OptionText = o.OptionText })
                            .ToList()
                        : null
                }).ToList(),
                Answers = answers
            };
        }
 
        private static int ComputeRemainingSeconds(ExamSession session, Exam exam)
        {
            var byDuration = session.StartTime.AddMinutes(exam.Duration);
            var deadline = byDuration < exam.EndTime ? byDuration : exam.EndTime;
            return (int)Math.Max(0, (deadline - DateTime.UtcNow).TotalSeconds);
        }
    }
}
 