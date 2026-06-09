using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoShip.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddPasskeyMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_used_at",
                table: "user_mfa_factors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "web_authn_credential_id_digest",
                table: "user_mfa_factors",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "web_authn_sign_count",
                table: "user_mfa_factors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_factors_web_authn_credential_id_digest",
                table: "user_mfa_factors",
                column: "web_authn_credential_id_digest");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_mfa_factors_web_authn_credential_id_digest",
                table: "user_mfa_factors");

            migrationBuilder.DropColumn(
                name: "last_used_at",
                table: "user_mfa_factors");

            migrationBuilder.DropColumn(
                name: "web_authn_credential_id_digest",
                table: "user_mfa_factors");

            migrationBuilder.DropColumn(
                name: "web_authn_sign_count",
                table: "user_mfa_factors");
        }
    }
}
