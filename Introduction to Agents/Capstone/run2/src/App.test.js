import React from 'react';
import { render } from '@testing-library/react';

jest.mock('./auth/Auth', () => ({
  useAuth0: () => ({ loading: true })
}));
jest.mock('./components/AxiosInterceptor', () => ({
  GlobalLoader: () => null
}));
jest.mock('./components/Footer', () => () => <div />);
jest.mock('./components/Header', () => () => <div />);
jest.mock('./components/routes', () => ({ routes: [] }));

const App = require('./App').default;

test('renders the loading indicator while authentication initializes', () => {
  const { getByRole } = render(<App />);

  expect(getByRole('progressbar')).toBeInTheDocument();
});
