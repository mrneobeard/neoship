// Aspire TypeScript AppHost
// For more information, see: https://aspire.dev

import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

const api = builder.addProject('api', '../api/ApiSvc/NeoShip.ApiSvc.csproj');

builder.addExecutable(
  'ui',
  'pnpm',
  '../ui',
  ['run', 'dev']
)
  .withEnvironment('API_BASE_URL', api.getEndpoint('http'))
  .waitFor(api);

await builder.build().run();

await builder.build().run();
