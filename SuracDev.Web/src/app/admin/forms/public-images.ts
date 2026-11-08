import { AdminDocument } from '../../api/generated/admin.client';

/** Whether a document may be shown on the open site as an image: public and of an image type. */
export function isPublicImage(document: AdminDocument): boolean {
  return document.visibility === 'Public' && (document.contentType ?? '').startsWith('image/');
}

/** "Label (file.png)", or the file name alone, for the image choices of the editors. */
export function imageOptionLabel(document: AdminDocument): string {
  return document.labelDe ? `${document.labelDe} (${document.fileName})` : (document.fileName ?? '');
}
