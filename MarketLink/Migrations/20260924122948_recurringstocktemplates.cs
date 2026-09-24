using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketLink.Migrations
{
    /// <inheritdoc />
    public partial class recurringstocktemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recurringstocktemplates",
                columns: table => new
                {
                    RecurringStockTemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerProductId = table.Column<int>(type: "int", nullable: false),
                    FarmerMarketDayId = table.Column<int>(type: "int", nullable: false),
                    DefaultQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurringstocktemplates", x => x.RecurringStockTemplateId);
                    table.ForeignKey(
                        name: "FK_recurringstocktemplates_farmermarketdays_FarmerMarketDayId",
                        column: x => x.FarmerMarketDayId,
                        principalTable: "farmermarketdays",
                        principalColumn: "FarmerMarketDayId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recurringstocktemplates_farmerproducts_FarmerProductId",
                        column: x => x.FarmerProductId,
                        principalTable: "farmerproducts",
                        principalColumn: "FarmerProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recurringstocktemplates_FarmerMarketDayId",
                table: "recurringstocktemplates",
                column: "FarmerMarketDayId");

            migrationBuilder.CreateIndex(
                name: "IX_recurringstocktemplates_FarmerProductId",
                table: "recurringstocktemplates",
                column: "FarmerProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recurringstocktemplates");
        }
    }
}
