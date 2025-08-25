// Guards
import {DefaultContextGuard} from './defaultContextGuard';
import {DefaultRedirectGuard} from './defaultRedirectGuard';
import {DefaultAuthenticatedGuard} from './defaultAuthenticatedGuard';
// Resolvers
import {NotFoundResolver} from './notFoundResolver';
import {PreloadedResolver} from './preloadResolvers';
import { TranslationsLoadedResolver } from './translationsLoadedResolver';
import { UserSettingsResolver } from './userSettingsResolver';
import {AuthenticatedResolver} from './authenticatedResolver';
import {ApplicationFeatureGuard} from './applicationFeatureGuard';
import {AuthGuard} from './authGuard';

export const ALL_GUARDS = [
	DefaultContextGuard,
	DefaultRedirectGuard,
	DefaultAuthenticatedGuard,
	AuthGuard
];

export const ALL_RESOLVERS = [
	NotFoundResolver,
	PreloadedResolver,
	UserSettingsResolver,
	TranslationsLoadedResolver,
	AuthenticatedResolver,
	ApplicationFeatureGuard
];
