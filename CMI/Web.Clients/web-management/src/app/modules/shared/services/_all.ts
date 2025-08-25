import {AuthorizationService} from './authorization.service';
import {UrlService} from './url.service';
import {AblieferndeStelleService} from './ablieferndeStelle.service';
import {UiServiceMC} from './ui.service-m-c.service';
import {UserService} from './user.service';
import {DetailPagingService} from './detailPaging.service';
import {ErrorService} from './error.service';
import {FileDownloadService} from './fileDownload.service';
import {ConfigService, TranslationService} from '@cmi/viaduc-web-core';

export const ALL_SERVICES = [
	AuthorizationService,
	UrlService,
	AblieferndeStelleService,
	UiServiceMC,
	DetailPagingService,
	UserService,
	ErrorService,
	FileDownloadService,
	TranslationService,
	ConfigService
];
