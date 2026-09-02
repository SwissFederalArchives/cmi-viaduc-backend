import {Injectable} from '@angular/core';

@Injectable()
export class SessionStorageService {
	public getItem<T>(key: string): T {
		const result = window.sessionStorage.getItem(key);
		if (result) {
			return <T>JSON.parse(result);
		}
		return <T>null;
	}

	public removeItem(key: string): void {
		window.sessionStorage.removeItem(key);
	}

	public setItem(key: string, item: any): void {
		window.sessionStorage.setItem(key, JSON.stringify(item));
	}

	public clear(): void {
		window.sessionStorage.clear();
	}

	public setUrl(key: string, item: string) {
		window.sessionStorage.setItem(key, item);
	}

	public getUrl(key: string): string {
		return window.sessionStorage.getItem(key) as string;
	}
}
