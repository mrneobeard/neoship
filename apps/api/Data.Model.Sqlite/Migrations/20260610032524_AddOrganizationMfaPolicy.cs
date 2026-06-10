using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoShip.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMfaPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ushort>(
                name: "mfa_policy_id",
                table: "orgs",
                type: "INTEGER",
                nullable: false,
                defaultValue: (ushort)0);
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
