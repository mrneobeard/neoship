using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoShip.Data.Mssql.Migrations
{
    /// <inheritdoc />
    public partial class AddUserExternalIdentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_external_identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    org_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_id = table.Column<long>(type: "bigint", nullable: false),
                    subject = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    subject_digest = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_external_identities", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_external_identities_orgs_org_id",
                        column: x => x.org_id,
                        principalTable: "orgs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_external_identities_user_identity_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "user_identity_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_external_identities_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_external_identities_org_id_provider_id_subject_digest",
                table: "user_external_identities",
                columns: new[] { "org_id", "provider_id", "subject_digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_external_identities_provider_id",
                table: "user_external_identities",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_external_identities_user_id_provider_id",
                table: "user_external_identities",
                columns: new[] { "user_id", "provider_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_external_identities");
        }
    }
}
