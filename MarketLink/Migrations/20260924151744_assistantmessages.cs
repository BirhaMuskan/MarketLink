using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketLink.Migrations
{
    /// <inheritdoc />
    public partial class assistantmessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assistantmessages",
                columns: table => new
                {
                    AssistantMessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssistantConversationId = table.Column<int>(type: "int", nullable: false),
                    MessageRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MessageText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistantmessages", x => x.AssistantMessageId);
                    table.ForeignKey(
                        name: "FK_assistantmessages_assistantconversations_AssistantConversationId",
                        column: x => x.AssistantConversationId,
                        principalTable: "assistantconversations",
                        principalColumn: "AssistantConversationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assistantmessages_AssistantConversationId",
                table: "assistantmessages",
                column: "AssistantConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assistantmessages");
        }
    }
}
