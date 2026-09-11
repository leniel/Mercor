import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import TodoImport from './TodoImport';

test('selecting a CSV file sends it to the import handler', () => {
    const onImport = jest.fn();
    const file = new File(['User,Name,Completed,DueDate,Priority'], 'todos.csv', {
        type: 'text/csv'
    });

    render(<TodoImport onImport={onImport} />);

    fireEvent.change(screen.getByLabelText(/import csv file/i), {
        target: { files: [file] }
    });

    expect(onImport).toHaveBeenCalledWith(file);
});
