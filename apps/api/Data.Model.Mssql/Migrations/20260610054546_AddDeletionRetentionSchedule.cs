using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoShip.Data.Mssql.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletionRetentionSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "hard_delete_at",
                table: "users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "orgs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "hard_delete_at",
                table: "orgs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_hard_delete_at",
                table: "users",
                column: "hard_delete_at");

            migrationBuilder.CreateIndex(
                name: "ix_orgs_hard_delete_at",
                table: "orgs",
                column: "hard_delete_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_hard_delete_at",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_orgs_hard_delete_at",
                table: "orgs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "hard_delete_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "orgs");

            migrationBuilder.DropColumn(
                name: "hard_delete_at",
                table: "orgs");
        }
    }
}