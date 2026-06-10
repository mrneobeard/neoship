using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NeoShip.Data.Pgsql.Migrations
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
                name: "fk_group_service_account_members_service_accounts_service_acco",
                table: "group_service_account_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_owners_groups_group1id",
                table: "group_service_account_owners");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_owners_service_accounts_service_accou",
                table: "group_service_account_owners");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "user_claims",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "user_api_key_claims",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "service_account_api_key_claims",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "role_claims",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "audit_events",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

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
                name: "fk_group_service_account_members_service_accounts_service_acco",
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
                name: "fk_group_service_account_owners_service_accounts_service_accou",
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
                name: "fk_group_service_account_members_service_accounts_service_acco",
                table: "group_service_account_members");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_owners_groups_group1id",
                table: "group_service_account_owners");

            migrationBuilder.DropForeignKey(
                name: "fk_group_service_account_owners_service_accounts_service_accou",
                table: "group_service_account_owners");

            migrationBuilder.AlterColumn<decimal>(
                name: "id",
                table: "user_claims",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "user_api_key_claims",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<decimal>(
                name: "id",
                table: "service_account_api_key_claims",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<decimal>(
                name: "id",
                table: "role_claims",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<decimal>(
                name: "id",
                table: "audit_events",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

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
                name: "fk_group_service_account_members_service_accounts_service_acco",
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
                name: "fk_group_service_account_owners_service_accounts_service_accou",
                table: "group_service_account_owners",
                column: "service_account_owners_id",
                principalTable: "service_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}