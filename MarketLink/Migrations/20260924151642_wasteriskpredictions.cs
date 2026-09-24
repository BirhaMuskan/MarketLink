using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketLink.Migrations
{
    /// <inheritdoc />
    public partial class wasteriskpredictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wasteriskpredictions",
                columns: table => new
                {
                    WasteRiskPredictionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiModelRunId = table.Column<int>(type: "int", nullable: true),
                    InventoryId = table.Column<int>(type: "int", nullable: false),
                    CurrentStock = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    PredictedDemand = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    PredictedExcess = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wasteriskpredictions", x => x.WasteRiskPredictionId);
                    table.ForeignKey(
                        name: "FK_wasteriskpredictions_aimodelruns_AiModelRunId",
                        column: x => x.AiModelRunId,
                        principalTable: "aimodelruns",
                        principalColumn: "AiModelRunId");
                    table.ForeignKey(
                        name: "FK_wasteriskpredictions_inventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "inventories",
                        principalColumn: "InventoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wasteriskpredictions_AiModelRunId",
                table: "wasteriskpredictions",
                column: "AiModelRunId");

            migrationBuilder.CreateIndex(
                name: "IX_wasteriskpredictions_InventoryId",
                table: "wasteriskpredictions",
                column: "InventoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wasteriskpredictions");
        }
    }
}
