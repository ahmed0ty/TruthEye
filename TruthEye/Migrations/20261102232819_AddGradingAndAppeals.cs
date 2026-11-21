using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruthEye.Migrations
{
    /// <inheritdoc />
    public partial class AddGradingAndAppeals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Appeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamSessionId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appeals_ExamSessions_ExamSessionId",
                        column: x => x.ExamSessionId,
                        principalTable: "ExamSessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GradeAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnswerId = table.Column<int>(type: "int", nullable: false),
                    ExamSessionId = table.Column<int>(type: "int", nullable: false),
                    OldMarks = table.Column<double>(type: "float", nullable: false),
                    NewMarks = table.Column<double>(type: "float", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedBy = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeAdjustments_Answers_AnswerId",
                        column: x => x.AnswerId,
                        principalTable: "Answers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_ExamSessionId",
                table: "Appeals",
                column: "ExamSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_StudentId",
                table: "Appeals",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeAdjustments_AnswerId",
                table: "GradeAdjustments",
                column: "AnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeAdjustments_ExamSessionId",
                table: "GradeAdjustments",
                column: "ExamSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appeals");

            migrationBuilder.DropTable(
                name: "GradeAdjustments");
        }
    }
}
