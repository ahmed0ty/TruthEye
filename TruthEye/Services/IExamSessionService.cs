using TruthEye.DTOs;

namespace TruthEye.Services
{
    public interface IExamSessionService
    {
        Task<ExamSessionDto> StartExamSessionAsync(StartExamSessionDto startDto);
        Task<ExamSessionDto> ContinueExamSessionAsync(int sessionId);
        Task<ExamTakeDto> GetExamTakeAsync(int sessionId, int studentId);
        Task<QuestionNavigationDto> MoveToNextQuestionAsync(int sessionId, int currentQuestionId, StudentAnswerDto answer);
        Task<ExamResultDto> SubmitExamSessionAsync(int sessionId);
        Task<ExamResultDto> GetExamResultAsync(int sessionId);
        Task<ExamAnswersDto> GetExamAnswersAsync(int sessionId);
        Task<ExamSessionDto> GetExamSessionByIdAsync(int sessionId);
        Task<IEnumerable<ExamSessionDto>> GetStudentExamHistoryAsync(int studentId);
        Task<IEnumerable<ExamSessionDto>> GetActiveSessionsAsync();
        Task<int> ExpireOverdueSessionsAsync();
    }
}