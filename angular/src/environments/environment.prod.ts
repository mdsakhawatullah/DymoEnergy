import { Environment } from '@abp/ng.core';

const baseUrl = 'https://d32u8xq08ndhng.cloudfront.net';

const oAuthConfig = {
  issuer: 'https://dl0ymm8cusxta.cloudfront.net/',
  redirectUri: baseUrl,
  clientId: 'DymoEnergy_App',
  responseType: 'code',
  scope: 'offline_access DymoEnergy',
  requireHttps: false,
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
      url: 'https://dl0ymm8cusxta.cloudfront.net',
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
