using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoShip.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class ProviderPortabilityFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_members_groups_group_id",
                table: "group_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_members_users_members_id",
                table: "group_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_owners_groups_group1id",
                table: "group_owners");

            migrationBuilder.DropForeignKey(
                name: "fk_group_owners_users_owners_id",
                table: "group_owners");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_members_groups_group_id",
                table: "group_service_account_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_members_service_accounts_service_account_members_id",
                table: "group_service_account_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_owners_groups_group1id",
                table: "group_service_account_owners");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_owners_service_accounts_service_account_owners_id",
                table: "group_service_account_owners");

            migrationBuilder.AddForeignKey(
                name: "fk_group_members_groups_group_id",
                table: "group_members",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_group_members_users_members_id",
                table: "group_members",
                column: "members_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_group_owners_groups_group1id",
                table: "group_owners",
                column: "group1id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_group_owners_users_owners_id",
                table: "group_owners",
                column: "owners_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_group_service_account_members_groups_group_id",
                table: "group_service_account_members",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_group_service_account_members_service_accounts_service_account_members_id",
                table: "group_service_account_members",
                column: "service_account_members_id",
                principalTable: "service_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_group_service_account_owners_groups_group1id",
                table: "group_service_account_owners",
                column: "group1id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_group_service_account_owners_service_accounts_service_account_owners_id",
                table: "group_service_account_owners",
                column: "service_account_owners_id",
                principalTable: "service_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_members_groups_group_id",
                table: "group_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_members_users_members_id",
                table: "group_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_owners_groups_group1id",
                table: "group_owners");

            migrationBuilder.DropForeignKey(
                name: "fk_group_owners_users_owners_id",
                table: "group_owners");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_members_groups_group_id",
                table: "group_service_account_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_members_service_accounts_service_account_members_id",
                table: "group_service_account_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_owners_groups_group1id",
                table: "group_service_account_owners");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_owners_service_accounts_service_account_owners_id",
                table: "group_service_account_owners");

            migrationBuilder.AddForeignKey(
                name: "fk_group_members_groups_group_id",
                table: "group_members",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_group_members_users_members_id",
                table: "group_members",
                column: "members_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_group_owners_groups_group1id",
                table: "group_owners",
                column: "group1id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_group_owners_users_owners_id",
                table: "group_owners",
                column: "owners_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_group_service_account_members_groups_group_id",
                table: "group_service_account_members",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_group_service_account_members_service_accounts_service_account_members_id",
                table: "group_service_account_members",
                column: "service_account_members_id",
                principalTable: "service_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_group_service_account_owners_groups_group1id",
                table: "group_service_account_owners",
                column: "group1id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_group_service_account_owners_service_accounts_service_account_owners_id",
                table: "group_service_account_owners",
                column: "service_account_owners_id",
                principalTable: "service_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}