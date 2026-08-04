import { CurrencyUsdPipe } from './currency-usd.pipe';

describe('CurrencyUsdPipe', () => {
  const pipe = new CurrencyUsdPipe();

  it('formats a number as USD with no decimals', () => {
    expect(pipe.transform(45000)).toBe('$45,000');
  });

  it('normalizes a string decimal (the API\'s number | string quirk)', () => {
    expect(pipe.transform('45000.00000000')).toBe('$45,000');
  });

  it('returns an em dash for null/undefined/NaN', () => {
    expect(pipe.transform(null)).toBe('—');
    expect(pipe.transform(undefined)).toBe('—');
    expect(pipe.transform('not-a-number')).toBe('—');
  });
});
