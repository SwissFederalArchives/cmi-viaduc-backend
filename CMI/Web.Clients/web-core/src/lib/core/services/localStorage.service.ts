import {Injectable} from '@angular/core';

@Injectable()
export class LocalStorageService {
	public getItem<T>(key: string): T | null {
		const item = window.localStorage.getItem(key);
		if (!item) {
			return null;
		}

		try {
			return JSON.parse(item) as T;
		} catch {
			return null;
		}
	}

	public setItem(key: string, item: any): void {
		window.localStorage.setItem(key, JSON.stringify(item));
	}

	public clear(): void {
		window.localStorage.clear();
	}
}
