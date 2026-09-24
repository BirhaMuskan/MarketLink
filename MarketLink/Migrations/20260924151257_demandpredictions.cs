using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketLink.Migrations
{
    /// <inheritdoc />
    public partial class demandpredictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "demandpredictions",
                columns: table => new
                {
                    DemandPredictionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiModelRunId = table.Column<int>(type: "int", nullable: false),
                    FarmerProductId = table.Column<int>(type: "int", nullable: false),
                    FarmerMarketId = table.Column<int>(type: "int", nullable: false),
                    PredictionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PredictedDemand = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    RecommendedStock = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CurrentReservations = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ShortageRisk = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demandpredictions", x => x.DemandPredictionId);
                    table.ForeignKey(
                        name: "FK_demandpredictions_aimodelruns_AiModelRunId",
                        column: x => x.AiModelRunId,
                        principalTable: "aimodelruns",
                        principalColumn: "AiModelRunId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_demandpredictions_farmermarkets_FarmerMarketId",
                        column: x => x.FarmerMarketId,
                        principalTable: "farmermarkets",
                        principalColumn: "FarmerMarketId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_demandpredictions_farmerproducts_FarmerProductId",
                        column: x => x.FarmerProductId,
                        principalTable: "farmerproducts",
                        principalColumn: "FarmerProductId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_demandpredictions_AiModelRunId",
                table: "demandpredictions",
                column: "AiModelRunId");

            migrationBuilder.CreateIndex(
                name: "IX_demandpredictions_FarmerMarketId",
                table: "demandpredictions",
                column: "FarmerMarketId");

            migrationBuilder.CreateIndex(
                name: "IX_demandpredictions_FarmerProductId",
                table: "demandpredictions",
                column: "FarmerProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "demandpredictions");
        }
    }
}
