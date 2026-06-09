using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoShip.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationAuthPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_oidc_sso",
                table: "orgs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_passkey_auth",
                table: "orgs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_password_auth",
                table: "orgs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_saml_sso",
                table: "orgs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_self_service_external_identity_unlink",
                table: "orgs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "require_sso",
                table: "orgs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_oidc_sso",
                table: "orgs");

            migrationBuilder.DropColumn(
                name: "allow_passkey_auth",
                table: "orgs");

            migrationBuilder.DropColumn(
                name: "allow_password_auth",
                table: "orgs");

            migrationBuilder.DropColumn(
                name: "allow_saml_sso",
                table: "orgs");

            migrationBuilder.DropColumn(
                name: "allow_self_service_external_identity_unlink",
                table: "orgs");

            migrationBuilder.DropColumn(
                name: "require_sso",
                table: "orgs");
        }
    }
}
