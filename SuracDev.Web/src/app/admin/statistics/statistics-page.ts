import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  AdminApplicationStatisticsClient,
  ApplicationActivity,
  ApplicationStatus,
} from '../../api/generated/admin.client';
import { Fact } from '../../ui/fact-bar/fact';
import { UiFactBar } from '../../ui/fact-bar/fact-bar';
import { UiErrorSummary } from '../../ui/error-summary/error-summary';
import { adminStrings } from '../admin-strings';
import { toProblem } from '../forms/problem';

type KnownStatus = Exclude<ApplicationStatus, 'Unknown'>;

const MINUTES_PER_HOUR = 60;
const MINUTES_PER_DAY = 24 * MINUTES_PER_HOUR;
/** Up to two days the hours still say more than "1 day"; beyond that days are precise enough. */
const HOURS_UNTIL = 2 * MINUTES_PER_DAY;
const DATE_TIME = new Intl.DateTimeFormat('de-DE', { dateStyle: 'short', timeStyle: 'short' });

/** One row of the activity table, already formatted. */
interface ActivityRow {
  readonly id: number;
  readonly company: string;
  readonly position: string;
  readonly status: string;
  readonly links: number;
  readonly opens: number;
  readonly pdfDownloads: number;
  readonly documentDownloads: number;
  readonly firstOpened: string;
  readonly timeToFirstOpen: string;
}

/**
 * The statistics page of the mini ATS: response rate, status pipeline and link activity per
 * application. Built from link events, which carry no visitor data (docs/security-and-privacy.md).
 */
@Component({
  selector: 'app-statistics-page',
  imports: [UiErrorSummary, UiFactBar],
  templateUrl: './statistics-page.html',
  styleUrls: ['../forms/editor.scss', './statistics-page.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsPage {
  private readonly client = inject(AdminApplicationStatisticsClient);

  protected readonly strings = adminStrings;
  protected readonly text = adminStrings.statistics;

  private readonly statisticsResource = rxResource({ stream: () => this.client.get() });
  private readonly statistics = computed(() =>
    this.statisticsResource.hasValue() ? this.statisticsResource.value() : undefined,
  );

  protected readonly loadErrors = computed(() =>
    this.statisticsResource.error()
      ? toProblem(this.statisticsResource.error(), adminStrings.form.loadFailed).messages
      : [],
  );
  protected readonly facts = computed<Fact[]>(() => {
    const statistics = this.statistics();
    const rate = statistics?.responseRate;
    return [
      {
        label: this.text.responseRate,
        value: rate === undefined || rate === null ? this.text.none : formatPercent(rate),
      },
      { label: this.text.sent, value: String(statistics?.sentCount ?? 0) },
      { label: this.text.responded, value: String(statistics?.respondedCount ?? 0) },
    ];
  });
  protected readonly pipeline = computed(() =>
    (this.statistics()?.pipeline ?? []).map((entry) => ({
      status: statusLabel(entry.status),
      count: entry.count ?? 0,
    })),
  );
  protected readonly activity = computed<ActivityRow[]>(() =>
    (this.statistics()?.applications ?? []).map(toRow),
  );
}

/** "42 Min.", "25 Std.", "4 Tage": whole units, rounded down; "–" when there was no opening yet. */
export function formatDuration(minutes: number | null | undefined): string {
  const text = adminStrings.statistics;
  if (minutes === null || minutes === undefined) {
    return text.none;
  }
  if (minutes < MINUTES_PER_HOUR) {
    return `${minutes} ${text.minutes}`;
  }
  if (minutes < HOURS_UNTIL) {
    return `${Math.floor(minutes / MINUTES_PER_HOUR)} ${text.hours}`;
  }
  return `${Math.floor(minutes / MINUTES_PER_DAY)} ${text.days}`;
}

function formatPercent(rate: number): string {
  return `${Math.round(rate * 100)} %`;
}

function statusLabel(status: ApplicationStatus | undefined): string {
  return adminStrings.applications.statuses[status as KnownStatus] ?? '';
}

function toRow(item: ApplicationActivity): ActivityRow {
  return {
    id: item.applicationId ?? 0,
    company: item.company ?? '',
    position: item.position ?? '',
    status: statusLabel(item.status),
    links: item.linkCount ?? 0,
    opens: item.opens ?? 0,
    pdfDownloads: item.pdfDownloads ?? 0,
    documentDownloads: item.documentDownloads ?? 0,
    firstOpened: item.firstOpenedAt
      ? DATE_TIME.format(new Date(item.firstOpenedAt))
      : adminStrings.statistics.none,
    timeToFirstOpen: formatDuration(item.minutesToFirstOpen),
  };
}
