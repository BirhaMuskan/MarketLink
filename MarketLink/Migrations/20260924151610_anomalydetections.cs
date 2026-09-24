using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketLink.Migrations
{
    /// <inheritdoc />
    public partial class anomalydetections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "anomalydetections",
                columns: table => new
                {
                    AnomalyDetectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiModelRunId = table.Column<int>(type: "int", nullable: true),
                    FarmerProductId = table.Column<int>(type: "int", nullable: false),
                    FarmerMarketId = table.Column<int>(type: "int", nullable: false),
                    BaselineAverage = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ObservedDemand = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    DeviationRatio = table.Column<decimal>(type: "decimal(8,3)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anomalydetections", x => x.AnomalyDetectionId);
                    table.ForeignKey(
                        name: "FK_anomalydetections_aimodelruns_AiModelRunId",
                        column: x => x.AiModelRunId,
                        principalTable: "aimodelruns",
                        principalColumn: "AiModelRunId");
                    table.ForeignKey(
                        name: "FK_anomalydetections_farmermarkets_FarmerMarketId",
                        column: x => x.FarmerMarketId,
                        principalTable: "farmermarkets",
                        principalColumn: "FarmerMarketId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_anomalydetections_farmerproducts_FarmerProductId",
                        column: x => x.FarmerProductId,
                        principalTable: "farmerproducts",
                        principalColumn: "FarmerProductId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anomalydetections_AiModelRunId",
                table: "anomalydetections",
                column: "AiModelRunId");

            migrationBuilder.CreateIndex(
                name: "IX_anomalydetections_FarmerMarketId",
                table: "anomalydetections",
                column: "FarmerMarketId");

            migrationBuilder.CreateIndex(
                name: "IX_anomalydetections_FarmerProductId",
                table: "anomalydetections",
                column: "FarmerProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anomalydetections");
        }
    }
}
