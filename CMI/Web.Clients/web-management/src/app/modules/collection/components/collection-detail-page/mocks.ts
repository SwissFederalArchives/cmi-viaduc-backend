import { ParamMap} from '@angular/router';
/* eslint-disable */

export class MockUserSettingsParamMap implements ParamMap {
	public readonly keys: string[];

	public get(name: string): string | null {
		return 'new';
	}

	public getAll(name: string): string[] {
		return [];
	}

	public has(name: string): boolean {
		return false;
	}
}
