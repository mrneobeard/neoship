using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoShip.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class MigrateBuiltInRoleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO role_assignments (id, org_id, user_id, role_key, scope_kind, scope_id, created_at, created_by)
                SELECT
                    lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || substr(lower(hex(randomblob(2))), 2) || '-' || substr('89ab', abs(random()) % 4 + 1, 1) || substr(lower(hex(randomblob(2))), 2) || '-' || lower(hex(randomblob(6))),
                    r.org_id,
                    ru.users_id,
                    CASE r.name_upcase
                        WHEN 'OWNER' THEN 'owner'
                        WHEN 'ADMIN' THEN 'admin'
                        ELSE 'reader'
                    END,
                    2,
                    o.slug,
                    datetime('now'),
                    ru.users_id
                FROM role_user AS ru
                INNER JOIN roles AS r ON r.id = ru.roles_id
                INNER JOIN orgs AS o ON o.id = r.org_id
                WHERE r.name_upcase IN ('OWNER', 'ADMIN', 'MEMBER')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM role_assignments AS ra
                      WHERE ra.org_id = r.org_id
                        AND ra.user_id = ru.users_id
                        AND ra.role_key = CASE r.name_upcase WHEN 'OWNER' THEN 'owner' WHEN 'ADMIN' THEN 'admin' ELSE 'reader' END
                        AND ra.scope_kind = 2
                        AND ra.scope_id = o.slug
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM role_assignments
                WHERE role_key IN ('owner', 'admin', 'reader')
                  AND scope_kind = 2
                  AND EXISTS (
                      SELECT 1
                      FROM role_user AS ru
                      INNER JOIN roles AS r ON r.id = ru.roles_id
                      INNER JOIN orgs AS o ON o.id = r.org_id
                      WHERE r.name_upcase IN ('OWNER', 'ADMIN', 'MEMBER')
                        AND role_assignments.org_id = r.org_id
                        AND role_assignments.user_id = ru.users_id
                        AND role_assignments.scope_id = o.slug
                  );
                """);
        }
    }
}
