import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** One file of a `ui-download-list`. */
export interface DownloadItem {
  readonly label: string;
  readonly href: string;
}

/**
 * A labelled list of files to download, such as the documents an application link unlocks. The
 * browser takes each file name from the server's Content-Disposition.
 */
@Component({
  selector: 'ui-download-list',
  templateUrl: './download-list.html',
  styleUrl: './download-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiDownloadList {
  readonly label = input.required<string>();
  readonly items = input.required<readonly DownloadItem[]>();
}
