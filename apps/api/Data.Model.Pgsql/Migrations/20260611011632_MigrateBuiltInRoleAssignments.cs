using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeoShip.Data.Pgsql.Migrations
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
                    (substr(md5(ru.roles_id::text || ':' || ru.users_id::text || ':' || r.name_upcase), 1, 8) || '-' || substr(md5(ru.roles_id::text || ':' || ru.users_id::text || ':' || r.name_upcase), 9, 4) || '-' || substr(md5(ru.roles_id::text || ':' || ru.users_id::text || ':' || r.name_upcase), 13, 4) || '-' || substr(md5(ru.roles_id::text || ':' || ru.users_id::text || ':' || r.name_upcase), 17, 4) || '-' || substr(md5(ru.roles_id::text || ':' || ru.users_id::text || ':' || r.name_upcase), 21, 12))::uuid,
                    r.org_id,
                    ru.users_id,
                    CASE r.name_upcase
                        WHEN 'OWNER' THEN 'owner'
                        WHEN 'ADMIN' THEN 'admin'
                        ELSE 'reader'
                    END,
                    2,
                    o.slug,
                    now(),
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
                DELETE FROM role_assignments AS ra
                WHERE ra.role_key IN ('owner', 'admin', 'reader')
                  AND ra.scope_kind = 2
                  AND EXISTS (
                      SELECT 1
                      FROM role_user AS ru
                      INNER JOIN roles AS r ON r.id = ru.roles_id
                      INNER JOIN orgs AS o ON o.id = r.org_id
                      WHERE r.name_upcase IN ('OWNER', 'ADMIN', 'MEMBER')
                        AND ra.org_id = r.org_id
                        AND ra.user_id = ru.users_id
                        AND ra.scope_id = o.slug
                  );
                """);
        }
    }
}
