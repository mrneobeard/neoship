using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoShip.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    org_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    email_upcase = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    token_digest = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    pending_role_ids_json = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                    pending_group_ids_json = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    accepted_by_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_invites", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_invites_orgs_org_id",
                        column: x => x.org_id,
                        principalTable: "orgs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_invites_users_accepted_by_user_id",
                        column: x => x.accepted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_invites_users_invited_by_user_id",
                        column: x => x.invited_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_organization_invites_accepted_by_user_id",
                table: "organization_invites",
                column: "accepted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_invites_invited_by_user_id",
                table: "organization_invites",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_invites_org_id_email_upcase",
                table: "organization_invites",
                columns: new[] { "org_id", "email_upcase" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_invites_token_digest",
                table: "organization_invites",
                column: "token_digest",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_invites");
        }
    }
}
