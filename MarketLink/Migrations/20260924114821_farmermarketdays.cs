using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketLink.Migrations
{
    /// <inheritdoc />
    public partial class farmermarketdays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "farmermarketdays",
                columns: table => new
                {
                    FarmerMarketDayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerMarketId = table.Column<int>(type: "int", nullable: false),
                    MarketDayId = table.Column<int>(type: "int", nullable: false),
                    PickupStartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    PickupEndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    OrderCutoffTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_farmermarketdays", x => x.FarmerMarketDayId);
                    table.ForeignKey(
                        name: "FK_farmermarketdays_farmermarkets_FarmerMarketId",
                        column: x => x.FarmerMarketId,
                        principalTable: "farmermarkets",
                        principalColumn: "FarmerMarketId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_farmermarketdays_marketdays_MarketDayId",
                        column: x => x.MarketDayId,
                        principalTable: "marketdays",
                        principalColumn: "MarketDayId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_farmermarketdays_FarmerMarketId",
                table: "farmermarketdays",
                column: "FarmerMarketId");

            migrationBuilder.CreateIndex(
                name: "IX_farmermarketdays_MarketDayId",
                table: "farmermarketdays",
                column: "MarketDayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "farmermarketdays");
        }
    }
}
