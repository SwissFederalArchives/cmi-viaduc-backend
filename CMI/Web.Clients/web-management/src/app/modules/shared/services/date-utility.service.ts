import { Injectable } from '@angular/core';
import * as moment from 'moment-timezone';

@Injectable({
  providedIn: 'root'
})
export class DateUtilityService {

  constructor() { }

  formatDate(date: any, format: string = 'DD.MM.YYYY', timeZone: string = 'Europe/Berlin'): string {
    if (!date) {
      return '';
    }

    const momentDate = moment(date);

    if (!momentDate.isValid()) {
      return '';
    }

    // Convert the parsed date to the specified time zone
    let timeZoneAdjustedDate;
    if (momentDate.year() < 1980) {
      timeZoneAdjustedDate = this.adjustForHistoricalDST(momentDate, timeZone);
    } else {
      timeZoneAdjustedDate = momentDate.tz(timeZone);
    }

    // Format the date according to the specified format
    const formattedDate = timeZoneAdjustedDate.format(format);

    return formattedDate;
  }

  private adjustForHistoricalDST(date: moment.Moment, timeZone: string): moment.Moment {
    const year = date.year();
    let offset = 1; // Default offset for CET

    // Apply manual DST rules (example rules, may need adjustment based on historical data)
    if (year >= 1940 && year <= 1945) {
      if (date.isBetween(`${year}-04-01`, `${year}-10-01`)) {
        offset = 2;
      }
    } else if (year > 1945) {
      if (date.isBetween(`${year}-03-25`, `${year}-09-30`)) {
        offset = 2;
      }
    }

    // Adjust the date by the calculated offset
    return date.clone().add(offset, 'hours').tz(timeZone, true);
  }
}
