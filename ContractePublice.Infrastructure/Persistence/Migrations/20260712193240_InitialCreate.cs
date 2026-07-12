using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ContractePublice.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractingAuthorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CUI = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    County = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractingAuthorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataImportLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordsImported = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataImportLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CUI = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    County = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeapId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CpvCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CpvDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContractType = table.Column<int>(type: "integer", nullable: false),
                    AwardProcedure = table.Column<int>(type: "integer", nullable: false),
                    EstimatedValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AwardedValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContractStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContractEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    County = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContractingAuthorityId = table.Column<int>(type: "integer", nullable: false),
                    SupplierId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_ContractingAuthorities_ContractingAuthorityId",
                        column: x => x.ContractingAuthorityId,
                        principalTable: "ContractingAuthorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contracts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AnomalyFlags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlagType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: true),
                    ContractingAuthorityId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnomalyFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnomalyFlags_ContractingAuthorities_ContractingAuthorityId",
                        column: x => x.ContractingAuthorityId,
                        principalTable: "ContractingAuthorities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnomalyFlags_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyFlags_ContractId",
                table: "AnomalyFlags",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyFlags_ContractingAuthorityId",
                table: "AnomalyFlags",
                column: "ContractingAuthorityId");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyFlags_FlagType",
                table: "AnomalyFlags",
                column: "FlagType");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyFlags_Severity",
                table: "AnomalyFlags",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_ContractingAuthorities_County",
                table: "ContractingAuthorities",
                column: "County");

            migrationBuilder.CreateIndex(
                name: "IX_ContractingAuthorities_CUI",
                table: "ContractingAuthorities",
                column: "CUI",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_AwardProcedure",
                table: "Contracts",
                column: "AwardProcedure");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractingAuthorityId",
                table: "Contracts",
                column: "ContractingAuthorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_County",
                table: "Contracts",
                column: "County");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_CpvCode",
                table: "Contracts",
                column: "CpvCode");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_PublishedAt",
                table: "Contracts",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_SeapId",
                table: "Contracts",
                column: "SeapId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_SupplierId",
                table: "Contracts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_DataImportLogs_ImportedAt",
                table: "DataImportLogs",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_County",
                table: "Suppliers",
                column: "County");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CUI",
                table: "Suppliers",
                column: "CUI",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnomalyFlags");

            migrationBuilder.DropTable(
                name: "DataImportLogs");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "ContractingAuthorities");

            migrationBuilder.DropTable(
                name: "Suppliers");
        }
    }
}
