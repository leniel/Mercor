import React from 'react';
import { render } from '@testing-library/react';
import EnhancedTable from './components/todo/EnhancedTable';
import { ThemeProvider, createTheme } from '@mui/material/styles';

test('renders Import CSV button when no rows are selected', () => {
  const theme = createTheme();
  
  render(
    <ThemeProvider theme={theme}>
      <EnhancedTable 
        todos={[]} 
        deleteTodo={() => {}} 
        completeTodo={() => {}} 
        loadTodo={() => {}} 
        searchTodo={() => {}} 
        importTodos={() => {}} 
      />
    </ThemeProvider>
  );
  
  const importInput = document.querySelector('input[type="file"]');
  expect(importInput).toBeInTheDocument();
  expect(importInput).toHaveAttribute(
    'accept',
    '.csv');
});
