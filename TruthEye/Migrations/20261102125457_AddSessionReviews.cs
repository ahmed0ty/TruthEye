using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TruthEye.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamSessionId = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    ReviewedBy = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionReviews_ExamSessions_ExamSessionId",
                        column: x => x.ExamSessionId,
                        principalTable: "ExamSessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionReviews_ExamSessionId",
                table: "SessionReviews",
                column: "ExamSessionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionReviews");
        }
    }
}
