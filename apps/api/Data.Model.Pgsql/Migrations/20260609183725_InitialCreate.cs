using System;

using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NeoShip.Data.Pgsql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    target_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    request_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    session_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    machine_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    trace_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    span_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    parent_span_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(46)", maxLength: 46, nullable: true),
                    ip_digest = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    region = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    asn = table.Column<long>(type: "bigint", nullable: true),
                    risk_level = table.Column<int>(type: "integer", nullable: false),
                    risk_flags_json = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    data_json = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orgs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name_upcase = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    organization_plan_id = table.Column<int>(type: "integer", nullable: false),
                    tenant_mode_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orgs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claims", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name_upcase = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_upcase = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_groups_orgs_org_id",
                        column: x => x.org_id,
                        principalTable: "orgs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    email_upcase = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    name_upcase = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    last_login_ip = table.Column<string>(type: "character varying(39)", maxLength: 39, nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_orgs_org_id",
                        column: x => x.org_id,
                        principalTable: "orgs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    members_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_members", x => new { x.group_id, x.members_id });
                    table.ForeignKey(
                        name: "fk_group_members_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_members_users_members_id",
                        column: x => x.members_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_owners",
                columns: table => new
                {
                    group1id = table.Column<Guid>(type: "uuid", nullable: false),
                    owners_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_owners", x => new { x.group1id, x.owners_id });
                    table.ForeignKey(
                        name: "fk_group_owners_groups_group1id",
                        column: x => x.group1id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_owners_users_owners_id",
                        column: x => x.owners_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name_upcase = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_roles_orgs_org_id",
                        column: x => x.org_id,
                        principalTable: "orgs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_roles_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name_upcase = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_accounts_orgs_org_id",
                        column: x => x.org_id,
                        principalTable: "orgs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_service_accounts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    key_digest = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    scopes_json = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_api_keys_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_emails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_digest = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    email_upcase = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verification_token_digest = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    verification_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    erased_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_emails", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_emails_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_identity_providers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_type_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    issuer_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    client_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    client_secret_enc = table.Column<byte[]>(type: "bytea", nullable: false),
                    metadata_json = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_identity_providers", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_identity_providers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_known_networks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(46)", maxLength: 46, nullable: true),
                    ip_digest = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    region = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    asn = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    count = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_known_networks", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_known_networks_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_mfa_factors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value_enc = table.Column<byte[]>(type: "bytea", nullable: false),
                    web_authn_public_key_credential_data = table.Column<byte[]>(type: "bytea", nullable: false),
                    web_authn_credential_id = table.Column<byte[]>(type: "bytea", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    transports_json = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_mfa_factors", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_mfa_factors_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_password_auths",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_salt = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    hash_algorithm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    iterations = table.Column<int>(type: "integer", nullable: false),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reset_token_digest = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    reset_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_password_auths", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_password_auths_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_digest = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(46)", maxLength: 46, nullable: true),
                    ip_digest = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    risk_level = table.Column<int>(type: "integer", nullable: false),
                    risk_flags_json = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    mfa_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    claims_json = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_sessions_orgs_org_id",
                        column: x => x.org_id,
                        principalTable: "orgs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_role",
                columns: table => new
                {
                    groups_id = table.Column<Guid>(type: "uuid", nullable: false),
                    roles_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_role", x => new { x.groups_id, x.roles_id });
                    table.ForeignKey(
                        name: "fk_group_role_groups_groups_id",
                        column: x => x.groups_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_role_roles_roles_id",
                        column: x => x.roles_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_claims_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_claims_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_user",
                columns: table => new
                {
                    roles_id = table.Column<Guid>(type: "uuid", nullable: false),
                    users_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_user", x => new { x.roles_id, x.users_id });
                    table.ForeignKey(
                        name: "fk_role_user_roles_roles_id",
                        column: x => x.roles_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_user_users_users_id",
                        column: x => x.users_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_service_account_members",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_account_members_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_service_account_members", x => new { x.group_id, x.service_account_members_id });
                    table.ForeignKey(
                        name: "fk_group_service_account_members_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_service_account_members_service_accounts_service_acco",
                        column: x => x.service_account_members_id,
                        principalTable: "service_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_service_account_owners",
                columns: table => new
                {
                    group1id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_account_owners_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_service_account_owners", x => new { x.group1id, x.service_account_owners_id });
                    table.ForeignKey(
                        name: "fk_group_service_account_owners_groups_group1id",
                        column: x => x.group1id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_service_account_owners_service_accounts_service_accou",
                        column: x => x.service_account_owners_id,
                        principalTable: "service_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_account_api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    key_digest = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    scopes_json = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_account_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_account_api_keys_service_accounts_service_account_id",
                        column: x => x.service_account_id,
                        principalTable: "service_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_account_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_account_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_account_claims_service_accounts_service_account_id",
                        column: x => x.service_account_id,
                        principalTable: "service_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_api_key_claims",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_api_key_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_api_key_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_api_key_claims_user_api_keys_user_api_key_id",
                        column: x => x.user_api_key_id,
                        principalTable: "user_api_keys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_account_api_key_claims",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    service_account_api_key_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_account_api_key_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_account_api_key_claims_service_account_api_keys_ser",
                        column: x => x.service_account_api_key_id,
                        principalTable: "service_account_api_keys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_account_api_key_roles",
                columns: table => new
                {
                    roles_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_account_api_key_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_account_api_key_roles", x => new { x.roles_id, x.service_account_api_key_id });
                    table.ForeignKey(
                        name: "fk_service_account_api_key_roles_roles_roles_id",
                        column: x => x.roles_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_service_account_api_key_roles_service_account_api_keys_serv",
                        column: x => x.service_account_api_key_id,
                        principalTable: "service_account_api_keys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_timestamp",
                table: "audit_events",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_type",
                table: "audit_events",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_group_members_members_id",
                table: "group_members",
                column: "members_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_owners_owners_id",
                table: "group_owners",
                column: "owners_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_role_roles_id",
                table: "group_role",
                column: "roles_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_service_account_members_service_account_members_id",
                table: "group_service_account_members",
                column: "service_account_members_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_service_account_owners_service_account_owners_id",
                table: "group_service_account_owners",
                column: "service_account_owners_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_org_id",
                table: "groups",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_created_by",
                table: "role_claims",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_role_id",
                table: "role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_user_users_id",
                table: "role_user",
                column: "users_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_created_by",
                table: "roles",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_roles_org_id_name_upcase",
                table: "roles",
                columns: new[] { "org_id", "name_upcase" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_account_api_key_claims_service_account_api_key_id",
                table: "service_account_api_key_claims",
                column: "service_account_api_key_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_account_api_key_roles_service_account_api_key_id",
                table: "service_account_api_key_roles",
                column: "service_account_api_key_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_account_api_keys_key_digest",
                table: "service_account_api_keys",
                column: "key_digest");

            migrationBuilder.CreateIndex(
                name: "ix_service_account_api_keys_service_account_id",
                table: "service_account_api_keys",
                column: "service_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_account_claims_service_account_id",
                table: "service_account_claims",
                column: "service_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_accounts_created_by",
                table: "service_accounts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_service_accounts_org_id",
                table: "service_accounts",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_api_key_claims_user_api_key_id",
                table: "user_api_key_claims",
                column: "user_api_key_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_api_keys_key_digest",
                table: "user_api_keys",
                column: "key_digest");

            migrationBuilder.CreateIndex(
                name: "ix_user_api_keys_user_id",
                table: "user_api_keys",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_emails_email_digest",
                table: "user_emails",
                column: "email_digest");

            migrationBuilder.CreateIndex(
                name: "ix_user_emails_user_id",
                table: "user_emails",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_emails_verification_token_digest",
                table: "user_emails",
                column: "verification_token_digest");

            migrationBuilder.CreateIndex(
                name: "ix_user_identity_providers_provider_type_id_org_id",
                table: "user_identity_providers",
                columns: new[] { "provider_type_id", "org_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_identity_providers_provider_type_id_user_id",
                table: "user_identity_providers",
                columns: new[] { "provider_type_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_identity_providers_user_id",
                table: "user_identity_providers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_known_networks_user_id",
                table: "user_known_networks",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_factors_user_id",
                table: "user_mfa_factors",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_password_auths_reset_token_digest",
                table: "user_password_auths",
                column: "reset_token_digest");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_org_id",
                table: "user_sessions",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_token_digest",
                table: "user_sessions",
                column: "token_digest");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_id",
                table: "user_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email_upcase",
                table: "users",
                column: "email_upcase");

            migrationBuilder.CreateIndex(
                name: "ix_users_org_id",
                table: "users",
                column: "org_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "group_members");

            migrationBuilder.DropTable(
                name: "group_owners");

            migrationBuilder.DropTable(
                name: "group_role");

            migrationBuilder.DropTable(
                name: "group_service_account_members");

            migrationBuilder.DropTable(
                name: "group_service_account_owners");

            migrationBuilder.DropTable(
                name: "role_claims");

            migrationBuilder.DropTable(
                name: "role_user");

            migrationBuilder.DropTable(
                name: "service_account_api_key_claims");

            migrationBuilder.DropTable(
                name: "service_account_api_key_roles");

            migrationBuilder.DropTable(
                name: "service_account_claims");

            migrationBuilder.DropTable(
                name: "user_api_key_claims");

            migrationBuilder.DropTable(
                name: "user_claims");

            migrationBuilder.DropTable(
                name: "user_emails");

            migrationBuilder.DropTable(
                name: "user_identity_providers");

            migrationBuilder.DropTable(
                name: "user_known_networks");

            migrationBuilder.DropTable(
                name: "user_mfa_factors");

            migrationBuilder.DropTable(
                name: "user_password_auths");

            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "service_account_api_keys");

            migrationBuilder.DropTable(
                name: "user_api_keys");

            migrationBuilder.DropTable(
                name: "service_accounts");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "orgs");
        }
    }
}