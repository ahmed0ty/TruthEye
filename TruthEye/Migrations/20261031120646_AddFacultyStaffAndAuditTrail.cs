using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TruthEye.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyStaffAndAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserType",
                table: "Users",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(13)",
                oldMaxLength: 13);

            migrationBuilder.AddColumn<int>(
                name: "Proctor_FacultyId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorRole",
                table: "SystemLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FacultyId",
                table: "SystemLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReverted",
                table: "SystemLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NewValues",
                table: "SystemLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldValues",
                table: "SystemLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevertedAt",
                table: "SystemLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevertedBy",
                table: "SystemLogs",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 7, "FacultySecretary" },
                    { 8, "TechnicalStaff" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Proctor_FacultyId",
                table: "Users",
                column: "Proctor_FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_FacultyId_Timestamp",
                table: "SystemLogs",
                columns: new[] { "FacultyId", "Timestamp" });

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Faculties_Proctor_FacultyId",
                table: "Users",
                column: "Proctor_FacultyId",
                principalTable: "Faculties",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Faculties_Proctor_FacultyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Proctor_FacultyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SystemLogs_FacultyId_Timestamp",
                table: "SystemLogs");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "Proctor_FacultyId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActorRole",
                table: "SystemLogs");

            migrationBuilder.DropColumn(
                name: "FacultyId",
                table: "SystemLogs");

            migrationBuilder.DropColumn(
                name: "IsReverted",
                table: "SystemLogs");

            migrationBuilder.DropColumn(
                name: "NewValues",
                table: "SystemLogs");

            migrationBuilder.DropColumn(
                name: "OldValues",
                table: "SystemLogs");

            migrationBuilder.DropColumn(
                name: "RevertedAt",
                table: "SystemLogs");

            migrationBuilder.DropColumn(
                name: "RevertedBy",
                table: "SystemLogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserType",
                table: "Users",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(21)",
                oldMaxLength: 21);
        }
    }
}
