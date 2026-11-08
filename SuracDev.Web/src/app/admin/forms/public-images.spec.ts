import { AdminDocument } from '../../api/generated/admin.client';
import { imageOptionLabel, isPublicImage } from './public-images';

const image: AdminDocument = {
  id: 1,
  fileName: 'portrait.png',
  contentType: 'image/png',
  visibility: 'Public',
  labelDe: 'Porträt',
};

describe('public images', () => {
  it('accepts public images only', () => {
    expect(isPublicImage(image)).toBe(true);
    expect(isPublicImage({ ...image, visibility: 'LinkOnly' })).toBe(false);
    expect(isPublicImage({ ...image, contentType: 'application/pdf' })).toBe(false);
  });

  it('names an image by its label and file name', () => {
    expect(imageOptionLabel(image)).toBe('Porträt (portrait.png)');
    expect(imageOptionLabel({ ...image, labelDe: undefined })).toBe('portrait.png');
  });
});
