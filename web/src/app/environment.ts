declare const __API_URL__: string;
declare const __PRODUCTION__: boolean;

export const environment = {
  production: typeof __PRODUCTION__ !== 'undefined' ? __PRODUCTION__ : true,
  apiUrl: typeof __API_URL__ !== 'undefined' ? __API_URL__ : '/api'
} as const;
