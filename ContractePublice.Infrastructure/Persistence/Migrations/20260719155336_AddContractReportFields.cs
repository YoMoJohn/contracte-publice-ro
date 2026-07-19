using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractePublice.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractReportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContractNumber",
                table: "Contracts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EuFunded",
                table: "Contracts",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingType",
                table: "Contracts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxValue",
                table: "Contracts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinValue",
                table: "Contracts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportSource",
                table: "Contracts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "AchizitieDirecta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContractNumber",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "EuFunded",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "FundingType",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ReportSource",
                table: "Contracts");
        }
    }
}
