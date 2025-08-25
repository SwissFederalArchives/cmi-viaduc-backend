import {InternalStatus} from '@cmi/viaduc-web-core';

export class StatusHistory {
	public id: number;
	public orderItemId: number;
	public statusChangeDate: Date;
	public fromStatus: InternalStatus;
	public toStatus: InternalStatus;
	public changedBy: string;
}
