import {SeoService} from './seo.service';
import {ContextService} from './context.service';
import {AuthenticationService} from './authentication.service';
import {NewsService} from './news.service';
import {SessionStorageService} from './sessionStorage.service';

export const ALL_SERVICES = [
	SeoService,
	ContextService,
	AuthenticationService,
	SessionStorageService,
	NewsService
];
