using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruthEye.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueSessionAndAnswerIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamSessions_StudentId_ExamId",
                table: "ExamSessions");

            migrationBuilder.DropIndex(
                name: "IX_Answers_ExamSessionId",
                table: "Answers");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_StudentId_ExamId",
                table: "ExamSessions",
                columns: new[] { "StudentId", "ExamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Answers_ExamSessionId_QuestionId",
                table: "Answers",
                columns: new[] { "ExamSessionId", "QuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamSessions_StudentId_ExamId",
                table: "ExamSessions");

            migrationBuilder.DropIndex(
                name: "IX_Answers_ExamSessionId_QuestionId",
                table: "Answers");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_StudentId_ExamId",
                table: "ExamSessions",
                columns: new[] { "StudentId", "ExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_ExamSessionId",
                table: "Answers",
                column: "ExamSessionId");
        }
    }
}
