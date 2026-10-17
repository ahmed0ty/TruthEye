using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruthEye.Migrations
{
    /// <inheritdoc />
    public partial class init4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "ExamSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Exams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Exams",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Exams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Exams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "Exams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Exams",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Exams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AIFeedback",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AISimilarityScore",
                table: "Answers",
                type: "float",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Answers_SelectedOptionId",
                table: "Answers",
                column: "SelectedOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_MCQOptions_SelectedOptionId",
                table: "Answers",
                column: "SelectedOptionId",
                principalTable: "MCQOptions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_MCQOptions_SelectedOptionId",
                table: "Answers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_SelectedOptionId",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "ExamSessions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "AIFeedback",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "AISimilarityScore",
                table: "Answers");
        }
    }
}
