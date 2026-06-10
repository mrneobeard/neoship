using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoShip.Data.Pgsql.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMfaPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "mfa_policy_id",
                table: "orgs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mfa_policy_id",
                table: "orgs");
        }
    }
}