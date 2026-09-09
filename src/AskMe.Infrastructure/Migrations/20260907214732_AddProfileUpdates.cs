using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AboutMe",
                table: "Users",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GithubUrl",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedinUrl",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationIp",
                table: "Users",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TiktokUrl",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitterUrl",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YoutubeUrl",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "QuestionId",
                table: "SecurityAuditLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ReferencedAnswerId",
                table: "Answers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YoutubeUrl",
                table: "Answers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BlockedIps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    BlockedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedIps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionFollowUps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionFollowUps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionFollowUps_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_ReferencedAnswerId",
                table: "Answers",
                column: "ReferencedAnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedIps_IpAddress",
                table: "BlockedIps",
                column: "IpAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionFollowUps_QuestionId",
                table: "QuestionFollowUps",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_Answers_ReferencedAnswerId",
                table: "Answers",
                column: "ReferencedAnswerId",
                principalTable: "Answers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_Answers_ReferencedAnswerId",
                table: "Answers");

            migrationBuilder.DropTable(
                name: "BlockedIps");

            migrationBuilder.DropTable(
                name: "QuestionFollowUps");

            migrationBuilder.DropIndex(
                name: "IX_Answers_ReferencedAnswerId",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "AboutMe",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GithubUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LinkedinUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RegistrationIp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TiktokUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwitterUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "YoutubeUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReferencedAnswerId",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "YoutubeUrl",
                table: "Answers");

            migrationBuilder.AlterColumn<Guid>(
                name: "QuestionId",
                table: "SecurityAuditLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
