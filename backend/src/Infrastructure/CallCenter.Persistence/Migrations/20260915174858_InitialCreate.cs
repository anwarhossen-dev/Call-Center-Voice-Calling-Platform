using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CallCenter.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.CampaignId);
                });

            migrationBuilder.CreateTable(
                name: "Dispositions",
                columns: table => new
                {
                    DispositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequiresFollowup = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispositions", x => x.DispositionId);
                });

            migrationBuilder.CreateTable(
                name: "Queues",
                columns: table => new
                {
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Strategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SLAThresholdSeconds = table.Column<int>(type: "int", nullable: false),
                    MaxWaitTimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queues", x => x.QueueId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    StateChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.AgentId);
                    table.ForeignKey(
                        name: "FK_Agents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentQueues",
                columns: table => new
                {
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentQueues", x => new { x.AgentId, x.QueueId });
                    table.ForeignKey(
                        name: "FK_AgentQueues_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "AgentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentQueues_Queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "Queues",
                        principalColumn: "QueueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentStateLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    SubReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentStateLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentStateLogs_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "AgentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Calls",
                columns: table => new
                {
                    CallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallUuid = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    CallerNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DestinationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DispositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InitiatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    TalkDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    HoldDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    WaitDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    CRMContactId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AgentNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calls", x => x.CallId);
                    table.ForeignKey(
                        name: "FK_Calls_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "AgentId");
                    table.ForeignKey(
                        name: "FK_Calls_Dispositions_DispositionId",
                        column: x => x.DispositionId,
                        principalTable: "Dispositions",
                        principalColumn: "DispositionId");
                    table.ForeignKey(
                        name: "FK_Calls_Queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "Queues",
                        principalColumn: "QueueId");
                });

            migrationBuilder.CreateTable(
                name: "CallRecordings",
                columns: table => new
                {
                    RecordingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageBucket = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileHashSHA256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    AudioChannels = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    OffloadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallRecordings", x => x.RecordingId);
                    table.ForeignKey(
                        name: "FK_CallRecordings_Calls_CallId",
                        column: x => x.CallId,
                        principalTable: "Calls",
                        principalColumn: "CallId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CallSessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChannelUuid = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HangupCause = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_CallSessions_Calls_CallId",
                        column: x => x.CallId,
                        principalTable: "Calls",
                        principalColumn: "CallId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Dispositions",
                columns: new[] { "DispositionId", "Category", "Code", "Description", "IsActive", "RequiresFollowup" },
                values: new object[,]
                {
                    { new Guid("d1111111-1111-1111-1111-111111111111"), "Support", "RESOLVED", "Query Resolved on Call", true, false },
                    { new Guid("d2222222-2222-2222-2222-222222222222"), "Support", "ESCALATED", "Issue Escalated to Tier 2", true, true },
                    { new Guid("d3333333-3333-3333-3333-333333333333"), "General", "CALLBACK_REQ", "Customer Requested Callback", true, true },
                    { new Guid("d4444444-4444-4444-4444-444444444444"), "Sales", "INTERESTED", "Prospect Interested in Offer", true, false },
                    { new Guid("d5555555-5555-5555-5555-555555555555"), "Sales", "NOT_INTERESTED", "Prospect Declined Offer", true, false }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "Description", "RoleName" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "System Administrator", "Admin" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Call Center Team Lead / Supervisor", "Supervisor" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Contact Center Agent", "Agent" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentQueues_QueueId",
                table: "AgentQueues",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Extension",
                table: "Agents",
                column: "Extension",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_UserId",
                table: "Agents",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentStateLogs_AgentId",
                table: "AgentStateLogs",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecordings_CallId",
                table: "CallRecordings",
                column: "CallId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Calls_AgentId",
                table: "Calls",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_CallerNumber",
                table: "Calls",
                column: "CallerNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_CallUuid",
                table: "Calls",
                column: "CallUuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Calls_DispositionId",
                table: "Calls",
                column: "DispositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_InitiatedAt_Status",
                table: "Calls",
                columns: new[] { "InitiatedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Calls_QueueId",
                table: "Calls",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_CallSessions_CallId",
                table: "CallSessions",
                column: "CallId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentQueues");

            migrationBuilder.DropTable(
                name: "AgentStateLogs");

            migrationBuilder.DropTable(
                name: "CallRecordings");

            migrationBuilder.DropTable(
                name: "CallSessions");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "Calls");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Dispositions");

            migrationBuilder.DropTable(
                name: "Queues");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
