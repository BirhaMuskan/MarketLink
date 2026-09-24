using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketLink.Migrations
{
    /// <inheritdoc />
    public partial class airecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "airecommendations",
                columns: table => new
                {
                    AiRecommendationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiModelRunId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FarmerProductId = table.Column<int>(type: "int", nullable: true),
                    RecommendationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(6,4)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airecommendations", x => x.AiRecommendationId);
                    table.ForeignKey(
                        name: "FK_airecommendations_aimodelruns_AiModelRunId",
                        column: x => x.AiModelRunId,
                        principalTable: "aimodelruns",
                        principalColumn: "AiModelRunId");
                    table.ForeignKey(
                        name: "FK_airecommendations_farmerproducts_FarmerProductId",
                        column: x => x.FarmerProductId,
                        principalTable: "farmerproducts",
                        principalColumn: "FarmerProductId");
                    table.ForeignKey(
                        name: "FK_airecommendations_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_airecommendations_AiModelRunId",
                table: "airecommendations",
                column: "AiModelRunId");

            migrationBuilder.CreateIndex(
                name: "IX_airecommendations_FarmerProductId",
                table: "airecommendations",
                column: "FarmerProductId");

            migrationBuilder.CreateIndex(
                name: "IX_airecommendations_UserId",
                table: "airecommendations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "airecommendations");
        }
    }
}
