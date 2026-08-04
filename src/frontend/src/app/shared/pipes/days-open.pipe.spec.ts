import { DaysOpenPipe } from './days-open.pipe';

describe('DaysOpenPipe', () => {
  const pipe = new DaysOpenPipe();

  it('pluralizes days', () => {
    expect(pipe.transform(34)).toBe('34 days');
    expect(pipe.transform(0)).toBe('0 days');
  });

  it('uses singular for exactly 1', () => {
    expect(pipe.transform(1)).toBe('1 day');
  });

  it('normalizes a string decimal and rounds', () => {
    expect(pipe.transform('14.6')).toBe('15 days');
  });

  it('returns an em dash for null/undefined/NaN', () => {
    expect(pipe.transform(null)).toBe('—');
    expect(pipe.transform(undefined)).toBe('—');
    expect(pipe.transform('n/a')).toBe('—');
  });
});
