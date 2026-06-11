using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoShip.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupRoleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_role_assignments_org_id_user_id_role_key_scope_kind_scope_id",
                table: "role_assignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "role_assignments",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "role_assignments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_group_id_org_id",
                table: "role_assignments",
                columns: new[] { "group_id", "org_id" });

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_org_id_group_id_role_key_scope_kind_scope_id",
                table: "role_assignments",
                columns: new[] { "org_id", "group_id", "role_key", "scope_kind", "scope_id" });

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_org_id_user_id_role_key_scope_kind_scope_id",
                table: "role_assignments",
                columns: new[] { "org_id", "user_id", "role_key", "scope_kind", "scope_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_role_assignments_groups_group_id",
                table: "role_assignments",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_role_assignments_groups_group_id",
                table: "role_assignments");

            migrationBuilder.DropIndex(
                name: "ix_role_assignments_group_id_org_id",
                table: "role_assignments");

            migrationBuilder.DropIndex(
                name: "ix_role_assignments_org_id_group_id_role_key_scope_kind_scope_id",
                table: "role_assignments");

            migrationBuilder.DropIndex(
                name: "ix_role_assignments_org_id_user_id_role_key_scope_kind_scope_id",
                table: "role_assignments");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "role_assignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "role_assignments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_assignments_org_id_user_id_role_key_scope_kind_scope_id",
                table: "role_assignments",
                columns: new[] { "org_id", "user_id", "role_key", "scope_kind", "scope_id" },
                unique: true);
        }
    }
}
