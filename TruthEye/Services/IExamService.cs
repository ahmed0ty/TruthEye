using TruthEye.DTOs;

namespace TruthEye.Services
{
    public interface IExamService
    {
        Task<ExamDto> GetExamByIdAsync(int examId);
        Task<IEnumerable<ExamDto>> GetAvailableExamsAsync(int studentId);
        Task<ExamDto> CreateExamAsync(CreateExamDto createExamDto, int doctorId);
        Task<ExamDto> UpdateExamAsync(int examId, CreateExamDto updateExamDto);
        Task<ExamDto> SetExamStatusAsync(int examId, int doctorId, string status);
        Task DeleteExamAsync(int examId);
        Task<QuestionDto> AddQuestionAsync(CreateQuestionDto questionDto);
        Task DeleteQuestionAsync(int questionId);

        Task<IEnumerable<QuestionDto>> GetExamQuestionsAsync(int examId);
        Task<List<StudentExamResultDto>> GetExamResultsAsync(int examId, int doctorId);
        Task<ExamAnalysisDto> GetExamAnalysisAsync(int examId, int doctorId);
        Task<StudentExamDetailsDto> GetStudentExamDetailsAsync(int sessionId, int doctorId);

        Task EnsureOwnerAsync(int examId, int doctorId);
Task EnsureQuestionOwnerAsync(int questionId, int doctorId);
    }
}