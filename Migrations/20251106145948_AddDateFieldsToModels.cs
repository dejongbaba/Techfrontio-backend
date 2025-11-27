using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Course_management.Migrations
{
    public partial class AddDateFieldsToModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStreakDays",
                table: "LearningStreaks");

            migrationBuilder.DropColumn(
                name: "CurrentStreakStartDate",
                table: "LearningStreaks");

            migrationBuilder.DropColumn(
                name: "LongestStreakEndDate",
                table: "LearningStreaks");

            migrationBuilder.DropColumn(
                name: "CertificateFilePath",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "FinalScore",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Certificates");

            migrationBuilder.RenameColumn(
                name: "TotalLearningHours",
                table: "LearningStreaks",
                newName: "LongestStreak");

            migrationBuilder.RenameColumn(
                name: "LongestStreakStartDate",
                table: "LearningStreaks",
                newName: "StreakStartDate");

            migrationBuilder.RenameColumn(
                name: "LongestStreakDays",
                table: "LearningStreaks",
                newName: "CurrentStreak");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Certificates",
                newName: "IssuedDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Reviews",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Reviews",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EnrollmentDate",
                table: "Enrollments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Courses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Courses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Certificates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateName",
                table: "Certificates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Certificates",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "EnrollmentDate",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CertificateName",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Certificates");

            migrationBuilder.RenameColumn(
                name: "StreakStartDate",
                table: "LearningStreaks",
                newName: "LongestStreakStartDate");

            migrationBuilder.RenameColumn(
                name: "LongestStreak",
                table: "LearningStreaks",
                newName: "TotalLearningHours");

            migrationBuilder.RenameColumn(
                name: "CurrentStreak",
                table: "LearningStreaks",
                newName: "LongestStreakDays");

            migrationBuilder.RenameColumn(
                name: "IssuedDate",
                table: "Certificates",
                newName: "UpdatedAt");

            migrationBuilder.AddColumn<int>(
                name: "CurrentStreakDays",
                table: "LearningStreaks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentStreakStartDate",
                table: "LearningStreaks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LongestStreakEndDate",
                table: "LearningStreaks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Certificates",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateFilePath",
                table: "Certificates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalScore",
                table: "Certificates",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "Certificates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "Certificates",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Certificates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
