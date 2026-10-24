import { Environment } from '@abp/ng.core';

const baseUrl = 'https://d32u8xq08ndhng.cloudfront.net';

/**
 * The customer-facing shop (DymoEnergyUserSite) — brand pages open at {userSiteUrl}/brands/{slug}.
 * Left empty until the shop's production address is set; the "view on shop" links stay hidden meanwhile.
 */
export const userSiteUrl = '';

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
