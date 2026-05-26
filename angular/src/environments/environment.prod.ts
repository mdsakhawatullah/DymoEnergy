import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44371/',
  redirectUri: baseUrl,
  clientId: 'DymoEnergy_App',
  responseType: 'code',
  scope: 'offline_access DymoEnergy',
  requireHttps: true,
};

export const environment = {
  production: true,
  application: {
    baseUrl,
    name: 'DymoEnergy',
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44371',
      rootNamespace: 'DymoEnergy',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
  remoteEnv: {
    url: '/getEnvConfig',
    mergeStrategy: 'deepmerge'
  }
} as Environment;
