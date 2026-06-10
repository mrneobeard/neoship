// Aspire TypeScript AppHost
// For more information, see: https://aspire.dev

import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();
const provider = (process.env.DB_PROVIDER ?? 'sqlite').toLowerCase();

const migrator = builder
  .addProject('db-migrator', '../api/DbMigrator/NeoShip.DbMigrator.csproj')
  .withEnvironment('Database__Provider', provider)
  .withEnvironment('ASPNETCORE_ENVIRONMENT', process.env.ASPNETCORE_ENVIRONMENT ?? 'Development');

const api = builder
  .addProject('api', '../api/ApiSvc/NeoShip.ApiSvc.csproj')
  .withEnvironment('Database__Provider', provider)
  .withEnvironment('ASPNETCORE_ENVIRONMENT', process.env.ASPNETCORE_ENVIRONMENT ?? 'Development')
  .waitFor(migrator);

if (provider === 'sqlite') {
  const connectionString = process.env.SQLITE_CONNECTION_STRING ?? 'Data Source=neoship-apphost.db';
  migrator.withEnvironment('Database__ConnectionString', connectionString);
  api.withEnvironment('Database__ConnectionString', connectionString);
} else if (provider === 'pgsql') {
  const password = process.env.POSTGRES_PASSWORD ?? 'postgres';
  const pgsql = builder
    .addContainer('pgsql', 'postgres:17')
    .withEnvironment('POSTGRES_USER', 'postgres')
    .withEnvironment('POSTGRES_PASSWORD', password)
    .withEnvironment('POSTGRES_DB', 'neoship')
    .withEndpoint('tcp', 5432);

  migrator
    .withEnvironment('Database__ConnectionString', `Host=pgsql;Port=5432;Database=neoship;Username=postgres;Password=${password}`)
    .waitFor(pgsql);
  api
    .withEnvironment('Database__ConnectionString', `Host=pgsql;Port=5432;Database=neoship;Username=postgres;Password=${password}`)
    .waitFor(pgsql);
} else if (provider === 'mssql') {
  const password = process.env.MSSQL_SA_PASSWORD ?? 'Password123!';
  const mssql = builder
    .addContainer('mssql', 'mcr.microsoft.com/mssql/server:2022-latest')
    .withEnvironment('ACCEPT_EULA', 'Y')
    .withEnvironment('MSSQL_PID', 'Developer')
    .withEnvironment('MSSQL_SA_PASSWORD', password)
    .withEndpoint('tcp', 1433);

  migrator
    .withEnvironment('Database__ConnectionString', `Server=mssql,1433;Database=neoship;User Id=sa;Password=${password};TrustServerCertificate=true`)
    .waitFor(mssql);
  api
    .withEnvironment('Database__ConnectionString', `Server=mssql,1433;Database=neoship;User Id=sa;Password=${password};TrustServerCertificate=true`)
    .waitFor(mssql);
} else {
  throw new Error(`Unsupported DB_PROVIDER '${provider}'. Use sqlite, pgsql, or mssql.`);
}

builder.addExecutable(
  'ui',
  'pnpm',
  '../ui',
  ['run', 'dev']
)
  .withEnvironment('API_BASE_URL', api.getEndpoint('http'))
  .waitFor(api);

await builder.build().run();
